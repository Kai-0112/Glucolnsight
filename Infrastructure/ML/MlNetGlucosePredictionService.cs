using System.IO;
using Microsoft.ML;
using ApplicationCore.Services;
using ApplicationCore.DTOs;
using Microsoft.ML.Data;

namespace Infrastructure.Services
{
    public class MlNetGlucosePredictionService : IGlucosePredictionService
    {
        private readonly ITransformer _multiStepModel;
        private readonly MLContext _ml;

        public MlNetGlucosePredictionService()
        {
            _ml = new MLContext();
            // 載入你剛訓練好的多步模型
            using var fs = File.OpenRead("MLModels/glucoseTrendModel.zip");
            _multiStepModel = _ml.Model.Load(fs, out _);
        }

        public float[] PredictAhead(FeatureVector fv)
        {
            var engine = _ml.Model.CreatePredictionEngine<FeatureVector, MultiPrediction>(_multiStepModel);
            var pred = engine.Predict(fv);
            return pred.PredictedBg;   // length = 8
        }

        public float Predict(FeatureVector fv)
        {
            // 回傳第一步（+15 分鐘）的預測
            var all = PredictAhead(fv);
            return all.Length > 0 ? all[0] : 0f;
        }

        private class MultiPrediction
        {
            [VectorType(8)]
            public float[] PredictedBg { get; set; }
        }
    }
}


