using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    /// <summary>ML 模型預測結果</summary>
    public class GlucosePrediction
    {
        public GlucosePrediction() { }
        public GlucosePrediction(float predictedBg) => PredictedBg = predictedBg;

        // 必須 set; 讓 ML.NET 可以回填
        public float PredictedBg { get; set; }
    }
}
