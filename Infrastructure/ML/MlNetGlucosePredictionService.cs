using System;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;
using ApplicationCore.Services;
using ApplicationCore.DTOs;

namespace Infrastructure.Services
{
    public class MlNetGlucosePredictionService : IGlucosePredictionService
    {
        private readonly ITransformer[] _models;
        private readonly MLContext _ml;

        public MlNetGlucosePredictionService()
        {
            _ml = new MLContext();
            _models = new ITransformer[8];

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
        /// 預測未來 8 個步長的血糖值
        /// </summary>
        public float[] PredictAhead(FeatureVector fv)
        {
            var results = new float[8];
            int baseHour = (int)fv.HourOfDay;                // 原始预测时刻的小时
            float baseAvg = fv.AvgBgPrev30Min;           // 原始平均 BG（可选保留）

            for (int k = 0; k < 8; k++)
            {
                // ─── 1) 更新 HourOfDay ─────────────────────────
                // 每 15 分钟算一次，(k+1)*15 分钟后的大致小时数
                int addedHours = ((k + 1) * 15) / 60;
                fv.HourOfDay = (baseHour + addedHours) % 24;

                // （可选）如果你要把 AvgBgPrev30Min 保持过去 30 分钟的平均，
                // 就不用重置它；否则你也可以让它等于 PrevBgs.Average()：
                fv.AvgBgPrev30Min = fv.PrevBgs.Average();

                // ─── 2) 用第 k 支模型做这一步的预测 ───────────────
                var engine = _ml.Model
                    .CreatePredictionEngine<FeatureVector, SinglePrediction>(_models[k]);
                float pred = engine.Predict(fv).PredictedBg;
                results[k] = pred;

                // ─── 3) 滑动更新 PrevBgs ────────────────────────
                var window = fv.PrevBgs.ToList();
                window.RemoveAt(0);    // 去掉最旧一笔
                window.Add(pred);      // 加上新预测
                fv.PrevBgs = window.ToArray();
            }

            return results;
        }



        /// <summary>
        /// 單點預測：回傳 +15 分鐘的預測
        /// </summary>
        public float Predict(FeatureVector fv)
        {
            // 直接呼叫第 1 支模型（index 0）
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
