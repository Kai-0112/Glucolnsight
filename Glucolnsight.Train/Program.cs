using ApplicationCore.DTOs;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Collections.Generic;

var options = new DbContextOptionsBuilder<GlucoInsightContext>()
    .UseSqlServer("Data Source=(localdb)\\mssqllocaldb;Initial Catalog=GlucoInsightDB;Integrated Security=True;")
    .Options;

using var ctx = new GlucoInsightContext(options);

// 讀取血糖與小時
var logs = ctx.CGMLog
              .OrderBy(r => r.reading_time)
              .Select(r => new {
                  Glucose = (float)(r.glucose_mgdl ?? 0),
                  Hour    = r.reading_time.HasValue ? r.reading_time.Value.Hour : 0
              })
              .ToList();

Console.WriteLine($"min={logs.Min(l => l.Glucose)}, max={logs.Max(l => l.Glucose)}, rows={logs.Count}");

// 準備訓練資料
var data = new List<Input>();
for (int i = 1; i < logs.Count; i++)
{
    data.Add(new Input
    {
        AvgBgPrev30Min = logs[i - 1].Glucose,  // 這裡暫用上一筆
        CarbPortion    = 0f,
        ExerciseMets   = 0f,
        HourOfDay      = logs[i].Hour,
        Label          = logs[i].Glucose
    });
}

var ml = new MLContext();
var dv = ml.Data.LoadFromEnumerable(data);

var pipeline = ml.Transforms.Concatenate(
                    "Features",
                    nameof(Input.AvgBgPrev30Min),
                    nameof(Input.CarbPortion),
                    nameof(Input.ExerciseMets),
                    nameof(Input.HourOfDay))
               .Append(ml.Regression.Trainers.FastTree());

var model = pipeline.Fit(dv);

// ➜ Local quick test
var testFv = new FeatureVector
{
    AvgBgPrev30Min = 120,
    CarbPortion    = 2,
    ExerciseMets   = 1.5f,
    HourOfDay      = 14
};

var pred = ml.Model.CreatePredictionEngine<FeatureVector, GlucosePrediction>(model)
                   .Predict(testFv);

Console.WriteLine($"[Local test] Pred = {pred.PredictedBg}");

// 儲存模型
System.IO.Directory.CreateDirectory("MLModels");
ml.Model.Save(model, dv.Schema, "MLModels/glucoseModel.zip");
Console.WriteLine("✅ 模型輸出完成 → MLModels/glucoseModel.zip");

// --------- 類別定義 ----------
public class Input : FeatureVector    // 只有 4 特徵 + Label
{
    [ColumnName("Label")]
    public float Label { get; set; }
}




