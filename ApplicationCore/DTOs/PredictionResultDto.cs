using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    /// <summary>
    /// 預測結果：回傳用戶輸入的特徵向量與模型預測出的血糖值
    /// </summary>
    public class PredictionResultDto
    {
        /// <summary>送入模型的特徵</summary>
        public FeatureVector Input { get; set; }

        /// <summary>模型預測的血糖值 (mg/dL)</summary>
        public float PredictedBg { get; set; }
    }
}

