using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using ApplicationCore.DTOs;
using Infrastructure.Entities;

#nullable disable

// --------------------------------------------------
// GlucoInsight.Train/Program.cs
// 多步趨勢模型訓練與評估：滑動視窗歷史 BG + 食物/運動特徵
// 儲存每一步模型至獨立檔案
// --------------------------------------------------

// 1. 設定 EF Core 連線
var options = new DbContextOptionsBuilder<GlucoInsightContext>()
    .UseSqlServer("Data Source=(localdb)\\mssqllocaldb;Initial Catalog=GlucoInsightDB;Integrated Security=True;")
    .Options;
using var ctx = new GlucoInsightContext(options);

// 2. 讀取有效 CGMLog 並排序
var logs = ctx.CGMLog
    .Where(r => r.reading_time.HasValue)
    .OrderBy(r => r.reading_time)
    .ToList();
if (logs.Count < 9)
{
    Console.WriteLine("ERROR: 有效 CGMLog 少於 9 筆，無法做 8 步預測。");
    return;
}

// 3. 讀取餐點與運動資料
var meals = ctx.MealLog
    .OrderBy(m => m.meal_event_time)
    .Select(m => new {
        m.meal_id,
        m.user_id,
        m.meal_event_time,
        Items = ctx.MealItem
            .Where(mi => mi.meal_id == m.meal_id)
            .Join(ctx.FoodItem,
                  mi => mi.food_id,
                  f => f.food_id,
                  (mi, f) => new {
                      Portion = (float)mi.portion,
                      CarbPerServing = (float)(f.carb_gram_per_serving ?? 0.0),
                      Gi = f.glycemic_index
                  })
            .ToList()
    })
    .ToList();
var exs = ctx.ExerciseLog
    .OrderBy(e => e.exercise_event_time)
    .ToList();

// 4. 準備多步資料，加入滑動視窗歷史 BG (P=4)
const int P = 4;
var data = new List<Input>();
for (int i = P; i + 8 < logs.Count; i++)
{
    var time = logs[i].reading_time.Value;
    // 滑動視窗：過去 P 筆 BG
    var prevBgs = Enumerable.Range(1, P)
        .Select(j => (float)(logs[i - j].glucose_mgdl ?? 0))
        .ToArray();

    // 最近一餐特徵
    var lastMeal = meals.LastOrDefault(m => m.user_id == 1 && m.meal_event_time <= time);
    float carbSum = 0, giSum = 0, ptSum = 0;
    if (lastMeal != null)
    {
        foreach (var mi in lastMeal.Items)
        {
            carbSum += mi.Portion * mi.CarbPerServing;
            giSum += mi.Gi * mi.Portion;
            ptSum += mi.Portion;
        }
    }
    float avgGi = ptSum > 0 ? giSum / ptSum : 0;

    // 最近一次運動
    var lastEx = exs.LastOrDefault(e => e.user_id == 1 && e.exercise_event_time <= time);
    float mets = (float)(lastEx?.mets ?? 0m);
    float dur = (float)(lastEx?.duration_min ?? 0m);

    // 未來 8 點 Label
    var lbls = new float[8];
    for (int k = 1; k <= 8; k++)
        lbls[k - 1] = (float)(logs[i + k].glucose_mgdl ?? 0);

    data.Add(new Input
    {
        PrevBgs = prevBgs,
        AvgBgPrev30Min = prevBgs.Average(),
        CarbPortion = carbSum,
        AvgGlycemicIndex = avgGi,
        ExerciseMets = mets,
        ExerciseDuration = dur,
        HourOfDay = time.Hour,
        Label1 = lbls[0],
        Label2 = lbls[1],
        Label3 = lbls[2],
        Label4 = lbls[3],
        Label5 = lbls[4],
        Label6 = lbls[5],
        Label7 = lbls[6],
        Label8 = lbls[7]
    });
}

// 5. 轉 IDataView + 時序切分
var ml = new MLContext(seed: 0);
var fullDv = ml.Data.LoadFromEnumerable(data);
var split = ml.Data.TrainTestSplit(fullDv, testFraction: 0.2);
var trainDv = split.TrainSet;
var testDv = split.TestSet;

// 6. 單步 +15 分鐘：Baseline vs Enhanced
string[] singleCols = new[] { nameof(Input.PrevBgs), nameof(Input.CarbPortion), nameof(Input.AvgGlycemicIndex), nameof(Input.ExerciseMets), nameof(Input.ExerciseDuration), nameof(Input.HourOfDay) };
var baselinePipeline = ml.Transforms
    .Concatenate("Features", nameof(Input.PrevBgs), nameof(Input.HourOfDay))
    .Append(ml.Transforms.NormalizeMinMax("Features"))
    .Append(ml.Regression.Trainers.FastTree(labelColumnName: "Label1"));
var enhancedPipeline = ml.Transforms
    .Concatenate("Features", singleCols)
    .Append(ml.Transforms.NormalizeMinMax("Features"))
    .Append(ml.Regression.Trainers.FastTree(labelColumnName: "Label1"));

var baselineModel = baselinePipeline.Fit(trainDv);
var enhancedModel = enhancedPipeline.Fit(trainDv);
var baselineMetrics = ml.Regression.Evaluate(baselineModel.Transform(testDv), labelColumnName: "Label1", scoreColumnName: "Score");
var enhancedMetrics = ml.Regression.Evaluate(enhancedModel.Transform(testDv), labelColumnName: "Label1", scoreColumnName: "Score");
Console.WriteLine($"Single-step Baseline MAE = {baselineMetrics.MeanAbsoluteError:F2}");
Console.WriteLine($"Single-step Enhanced MAE = {enhancedMetrics.MeanAbsoluteError:F2}");

// 7. 多步訓練 & 評估，並同時儲存每一步模型
string[] featureCols = new[] { nameof(Input.PrevBgs), nameof(Input.CarbPortion), nameof(Input.AvgGlycemicIndex), nameof(Input.ExerciseMets), nameof(Input.ExerciseDuration), nameof(Input.HourOfDay) };
var multiStepPipeline = ml.Transforms
    .Concatenate("Features", featureCols)
    .Append(ml.Transforms.NormalizeMinMax("Features"));

Console.WriteLine("▶ Training, evaluating & saving 8 models:");
Directory.CreateDirectory("MLModels");
for (int k = 1; k <= 8; k++)
{
    var stepPipeline = multiStepPipeline.Append(
        ml.Regression.Trainers.FastTree(labelColumnName: $"Label{k}", featureColumnName: "Features"));
    var modelK = stepPipeline.Fit(trainDv);
    var predDvK = modelK.Transform(testDv);
    var m = ml.Regression.Evaluate(predDvK, labelColumnName: $"Label{k}", scoreColumnName: "Score");
    Console.WriteLine($"  Step {k * 15,3}min → MAE={m.MeanAbsoluteError:F2}, RMSE={m.RootMeanSquaredError:F2}");
    // 儲存每一步模型
    var modelPath = Path.Combine("MLModels", $"glucoseModel_step{k}.zip");
    ml.Model.Save(modelK, trainDv.Schema, modelPath);
    Console.WriteLine($"    Saved model: {modelPath}");
}

// --------- Input 類別 定義 ----------
public class Input : FeatureVector
{
    [VectorType(4)] public float[] PrevBgs { get; set; }
    [ColumnName("Label1")] public float Label1 { get; set; }
    [ColumnName("Label2")] public float Label2 { get; set; }
    [ColumnName("Label3")] public float Label3 { get; set; }
    [ColumnName("Label4")] public float Label4 { get; set; }
    [ColumnName("Label5")] public float Label5 { get; set; }
    [ColumnName("Label6")] public float Label6 { get; set; }
    [ColumnName("Label7")] public float Label7 { get; set; }
    [ColumnName("Label8")] public float Label8 { get; set; }
}
