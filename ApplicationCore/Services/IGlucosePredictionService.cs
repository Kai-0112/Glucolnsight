using ApplicationCore.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Services
{
    /// <summary>抽象化的血糖預測服務</summary>
    public interface IGlucosePredictionService
    {
        /// <summary>
        /// 單點預測：傳入一組特徵，回傳 +15 分鐘 的預測血糖值（mg/dL）
        /// </summary>
        float Predict(FeatureVector fv);

        /// <summary>
        /// 多步預測：傳入一組特徵，回傳未來 8 個 15 分鐘步長的預測血糖值陣列
        /// </summary>
        float[] PredictAhead(FeatureVector fv);
    }
}
