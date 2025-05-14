using Microsoft.ML.Data;

namespace ApplicationCore.DTOs
{
    /// <summary>ML 模型預測結果</summary>
    public class GlucosePrediction
    {
        public GlucosePrediction() { }
        public GlucosePrediction(float predictedBg) => PredictedBg = predictedBg;

        // ML.NET 會把預測值塞到名稱為 "Score" 的欄位
        [ColumnName("Score")]
        public float PredictedBg { get; set; }
    }
}
