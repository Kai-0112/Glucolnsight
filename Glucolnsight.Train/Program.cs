using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using ApplicationCore.DTOs;
using Infrastructure.Entities;
using System.Runtime.CompilerServices;

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

// 2. 讀取 CGMLog 資料表並排序
var logs = ctx.CGMLog
    .Where(r => r.reading_time.HasValue)
    .OrderBy(r => r.reading_time)
    .ToList();
if (logs.Count < 9)
{
    Console.WriteLine("ERROR: 有效 CGMLog 少於 9 筆，無法做 8 步預測。");
    return;
}

// 3. 讀取餐點與運動資料表
var meals = ctx.MealLog
    .OrderBy(m => m.meal_event_time)
    .Select(m => new {
        m.meal_id,
        m.user_id,
        m.meal_event_time,
        m.meal_type,
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

// 4. 產生訓練資料 (List<Input> data)
//  - P = 4  → 取過去 4 筆 BG 作為「短期趨勢」
//  - 每次迴圈 i 代表一個時間點 (logs[i])
//  - 輸出 8 個未來點 (15min × 8 = 2 小時) 當 Label1–Label8

const int P = 4;
var data = new List<Input>();

// i 從第 4 筆 (index = P) 起，確保有足夠歷史與未來值
for (int i = P; i + 8 < logs.Count; i++)
{
    var time = logs[i].reading_time.Value; // 當前時間

    // 4.1 過去 BG 滑動視窗
    var prevBgs = Enumerable.Range(1, P)
        .Select(j => (float)(logs[i - j].glucose_mgdl ?? 0)) 
        .ToArray();

    // 4.2 最近一餐特徵
    var lastMeal = meals.LastOrDefault(m => m.user_id == 1 && m.meal_event_time <= time);

    // 計算碳水總量 (carbSum) 與平均 GI (avgGi)
    float carbSum = 0, giSum = 0, ptSum = 0;
    if (lastMeal != null)
    {
        foreach (var mi in lastMeal.Items)
        {
            carbSum += mi.Portion * mi.CarbPerServing;  // 總碳水化合物
            giSum += mi.Gi * mi.Portion;                // GI 加權總和
            ptSum += mi.Portion;                        // 份數總和
        }
    }
    float avgGi = ptSum > 0 ? giSum / ptSum : 0;        // 份數加權平均 GI

    // 4.3 最近一次運動特徵
    var lastEx = exs.LastOrDefault(e => e.user_id == 1 && e.exercise_event_time <= time);
    float mets = (float)(lastEx?.mets ?? 0m);           // 強度 (METs)
    float dur = (float)(lastEx?.duration_min ?? 0m);    // 持續時間 (min)

    // 4.4 時間差與餐型
    float minutesSinceMeal = lastMeal != null
        ? (float)(time - lastMeal.meal_event_time).TotalMinutes
        : 0f;
    float lastMealItemCount = lastMeal != null ? lastMeal.Items.Count : 0;

    // 用餐時段
    float isBreakfast = lastMeal?.meal_type == "早餐" ? 1f : 0f;
    float isLunch = lastMeal?.meal_type == "午餐" ? 1f : 0f;
    float isDinner = lastMeal?.meal_type == "晚餐" ? 1f : 0f;
    float isAfternoonTea = lastMeal?.meal_type == "下午茶" ? 1f : 0f;
    float isLateNight = lastMeal?.meal_type == "消夜" ? 1f : 0f;

    float minutesSinceExercise = lastEx != null
        ? (float)(time - lastEx.exercise_event_time).TotalMinutes
        : 0f;



    // 4.5 取未來 8 點作為 Label1–Label8
    var lbls = new float[8];
    for (int k = 1; k <= 8; k++)
        lbls[k - 1] = (float)(logs[i + k].glucose_mgdl ?? 0);

    // 4.6 組成 Input 物件並加入 data 列表
    data.Add(new Input
    {
        // 特徵
        PrevBgs = prevBgs,
        AvgBgPrev30Min = prevBgs.Average(),
        CarbPortion = carbSum,
        AvgGlycemicIndex = avgGi,
        ExerciseMets = mets,
        ExerciseDuration = dur,
        HourOfDay = time.Hour,
        MinutesSinceMeal = minutesSinceMeal,
        MinutesSinceExercise = minutesSinceExercise,
        LastMealItemCount = lastMealItemCount,
        IsBreakfast = isBreakfast,
        IsLunch = isLunch,
        IsDinner = isDinner,
        IsAfternoonTea = isAfternoonTea,
        IsLateNight = isLateNight,

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

// 5. 資料管線與時序切分
//    - ML.NET 需先將 List<Input> 轉成 IDataView
//    - Train/Test = 0.8 / 0.2 ；固定 seed = 0 以便重現
var ml = new MLContext(seed: 0);
var fullDv = ml.Data.LoadFromEnumerable(data);
var split = ml.Data.TrainTestSplit(fullDv, testFraction: 0.2);
var trainDv = split.TrainSet; // 80% → 訓練
var testDv = split.TestSet;   // 20% → 評估


// 6.「單步 +15min」Baseline vs Enhanced (兩個進行比較)
//    - Baseline：PrevBgs + HourOfDay，單只看「歷史趨勢 」
//    - Enhanced：再加食物 / GI / 運動強度 … 等 4 個特徵
//    - Trainer：FastTree Regression（梯度提升樹）
//    - 指標：MeanAbsoluteError (MAE)，值越小越好
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


// 7.「 8個點×15min」統一管線 + 逐步訓練 / 儲存
//    - featureCols：納入全部飲食/運動/時間特徵
//    - 迴圈 k = 1..8：各自以 Labelk (+15*k 分) 為目標 (k 為代號)
//    - 每一步存成「MLModels/glucoseModel_step{k}.zip」，方便線上推斷
//    - 另外 Print MAE(平均絕對誤差) / RMSE(均方根誤差) 比較效能 (回歸模型的衡量標準)
string[] featureCols = new[] { 
    nameof(Input.PrevBgs), 
    nameof(Input.CarbPortion), 
    nameof(Input.AvgGlycemicIndex), 
    nameof(Input.ExerciseMets), 
    nameof(Input.ExerciseDuration), 
    nameof(Input.HourOfDay),
    nameof(Input.MinutesSinceMeal),
    nameof(Input.MinutesSinceExercise),
    nameof(Input.LastMealItemCount),
    nameof(Input.IsBreakfast),
    nameof(Input.IsLunch),
    nameof(Input.IsDinner),
    nameof(Input.IsAfternoonTea),
    nameof(Input.IsLateNight),
};
var multiStepPipeline = ml.Transforms
    .Concatenate("Features", featureCols)
    .Append(ml.Transforms.NormalizeMinMax("Features"));

Console.WriteLine("▶ Training, evaluating & saving 8 models:");
Directory.CreateDirectory("MLModels");
for (int k = 1; k <= 8; k++)
{
    // 7-1. 對應 k 步的管線 (Labelk)
    var stepPipeline = multiStepPipeline.Append(
        ml.Regression.Trainers.FastTree(labelColumnName: $"Label{k}", featureColumnName: "Features"));

    // 7-2. 訓練 + 評估
    var modelK = stepPipeline.Fit(trainDv);
    var predDvK = modelK.Transform(testDv);
    var m = ml.Regression.Evaluate(predDvK, labelColumnName: $"Label{k}", scoreColumnName: "Score");
    Console.WriteLine($"  Step {k * 15,3}min → MAE={m.MeanAbsoluteError:F2}, RMSE={m.RootMeanSquaredError:F2}");


    // 7-3. 儲存每一步模型
    var modelPath = Path.Combine("MLModels", $"glucoseModel_step{k}.zip");
    ml.Model.Save(modelK, trainDv.Schema, modelPath);
    Console.WriteLine($"    Saved model: {modelPath}");

    //Debug
    Console.WriteLine("Sample feature vector:");
    var sample = data[0];
    Console.WriteLine($"MinutesSinceMeal={sample.MinutesSinceMeal}, MinutesSinceExercise={sample.MinutesSinceExercise}, LastMealItemCount={sample.LastMealItemCount}, IsDinner={sample.IsDinner}");

}



// Input 類別：訓練用資料結構 (FeatureVector)
//  - PrevBgs[4]            ：t-15,-30,-45,-60 min 血糖
//  - Label1–Label8         ：未來 +15~+120 min 的數據
//  - MinutesSinceMeal/Exercise 時間特徵
//  - IsBreakfast…IsLateNight：用餐時段
public class Input : FeatureVector
{
    [VectorType(4)] public float[] PrevBgs { get; set; }
    [ColumnName("Label1")] public float Label1 { get; set; } // +15 min
    [ColumnName("Label2")] public float Label2 { get; set; } // +30 min
    [ColumnName("Label3")] public float Label3 { get; set; }
    [ColumnName("Label4")] public float Label4 { get; set; }
    [ColumnName("Label5")] public float Label5 { get; set; }
    [ColumnName("Label6")] public float Label6 { get; set; }
    [ColumnName("Label7")] public float Label7 { get; set; }
    [ColumnName("Label8")] public float Label8 { get; set; } // +120 min


    // 時間差 / 用餐時段
    public float MinutesSinceMeal { get; set; }
    public float MinutesSinceExercise { get; set; }
    public float LastMealItemCount { get; set; }
    public float IsBreakfast { get; set; }
    public float IsLunch { get; set; }
    public float IsDinner { get; set; }
    public float IsAfternoonTea { get; set; }
    public float IsLateNight { get; set; }
}
