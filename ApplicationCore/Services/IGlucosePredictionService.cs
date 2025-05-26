using ApplicationCore.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Services
{
    /// <summary>血糖預測服務</summary>
    public interface IGlucosePredictionService
    {
        // 單點預測：傳入一組特徵，回傳 +15 分鐘 的預測血糖值（mg/dL）
        float Predict(FeatureVector fv);

        // 多步預測：傳入一組特徵，回傳未來 8 個 15 分鐘步長的預測血糖值陣列
        float[] PredictAhead(FeatureVector fv);
    }
}
