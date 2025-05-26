using System;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;
using ApplicationCore.Services;
using ApplicationCore.DTOs;

namespace Infrastructure.Services
{
    /// <summary>
    /// 把離線訓練好的 8 個 ML.NET 模型（+15、+30 … +120 分鐘）讀進記憶體，提供 ApplicationCore
    /// </summary>
    public class MlNetGlucosePredictionService : IGlucosePredictionService
    {
        private readonly ITransformer[] _models;
        private readonly MLContext _ml;

        public MlNetGlucosePredictionService()
        {
            _ml = new MLContext();         
            _models = new ITransformer[8]; // index 0 = +15min, 1 = +30min, … 7 = +120min

            // 執行目錄下的 MLModels 資料夾
            var exeDir = AppContext.BaseDirectory;
            var modelsDir = Path.Combine(exeDir, "MLModels");

            // 依序載入 glucoseModel_step1.zip ~ glucoseModel_step8.zip
            for (int k = 1; k <= 8; k++)
            {
                var path = Path.Combine(modelsDir, $"glucoseModel_step{k}.zip");
                if (!File.Exists(path))
                    throw new FileNotFoundException($"找不到模型檔：{path}");

                using var fs = File.OpenRead(path);
                _models[k - 1] = _ml.Model.Load(fs, out _);
            }
        }

        /// <summary>
        /// 預測未來 8 個單點的血糖值
        /// </summary>
        public float[] PredictAhead(FeatureVector fv)
        {
            var results = new float[8];            // 回傳陣列；索引 0 = +15 min，索引 7 = +120 min
            int baseHour = (int)fv.HourOfDay;      // 記住原本特徵向量裡的小時（0–23），後續每 4 個點才會進位 1 小時
            float baseAvg = fv.AvgBgPrev30Min;     // 30 分鐘平均血糖(保留，尚未用到)

            for (int k = 0; k < 8; k++)
            {
                // 以 15 分鐘為單位計算大概的 HourOfDay，模擬晝夜節律對血糖的影響
                int addedHours = ((k + 1) * 15) / 60;
                fv.HourOfDay = (baseHour + addedHours) % 24;

                // 重新計算「過去 30 分鐘平均值」，讓特徵跟著新預測動態變化
                fv.AvgBgPrev30Min = fv.PrevBgs.Average();

                // 第 k 支模型 = +15×(k+1) min 的 FastTree 回歸
                var engine = _ml.Model
                    .CreatePredictionEngine<FeatureVector, SinglePrediction>(_models[k]);
                float pred = engine.Predict(fv).PredictedBg;
                results[k] = pred;
                var window = fv.PrevBgs.ToList();

                // 提供「最新」趨勢，並去掉舊的一筆
                window.RemoveAt(0);             
                window.Add(pred);

                //依序回傳 +15、+30、…、+120 分鐘 8 個血糖預測值
                fv.PrevBgs = window.ToArray();
            }

            return results;
        }


        /// <summary>
        /// 單點預測：回傳 +15 分鐘的預測
        /// </summary>
        public float Predict(FeatureVector fv)
        {
            // 直接呼叫第 1 支模型（index 0），僅單一個預測點而已
            var engine = _ml.Model.CreatePredictionEngine<FeatureVector, SinglePrediction>(_models[0]);
            return engine.Predict(fv).PredictedBg;
        }

        private class SinglePrediction
        {
            [ColumnName("Score")]
            public float PredictedBg { get; set; }
        }
    }
}
