using ApplicationCore.DTOs;
using ApplicationCore.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.ML;

namespace Infrastructure.ML
{
    public class MlNetGlucosePredictionService : IGlucosePredictionService
    {
        private readonly PredictionEngine<FeatureVector, GlucosePrediction> _engine;

        public MlNetGlucosePredictionService(IHostEnvironment env)
        {
            var ml = new MLContext();
            var modelPath = Path.Combine(env.ContentRootPath, "MLModels", "glucoseModel.zip");
            var model = ml.Model.Load(modelPath, out _);
            _engine = ml.Model.CreatePredictionEngine<FeatureVector, GlucosePrediction>(model);
        }

        public GlucosePrediction Predict(FeatureVector features) => _engine.Predict(features);
    }
}
