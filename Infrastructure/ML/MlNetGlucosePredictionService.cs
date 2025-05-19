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
            for (int i = 0; i < 8; i++)
            {
                // 每支模型都輸出一個 Score
                var engine = _ml.Model.CreatePredictionEngine<FeatureVector, SinglePrediction>(_models[i]);
                results[i] = engine.Predict(fv).PredictedBg;
            }
            return results;
        }

        /// <summary>
        /// 單點預測：回傳 +15 分鐘的預測
        /// </summary>
        public float Predict(FeatureVector fv)
        {
            var all = PredictAhead(fv);
            return all.Length > 0 ? all[0] : 0f;
        }

        private class SinglePrediction
        {
            [ColumnName("Score")]
            public float PredictedBg { get; set; }
        }
    }
}
