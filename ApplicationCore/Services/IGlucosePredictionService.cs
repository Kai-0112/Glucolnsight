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
        GlucosePrediction Predict(FeatureVector features);
    }
}
