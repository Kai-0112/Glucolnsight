using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    /// <summary>
    /// 預測結果：回傳使用者輸入的特徵向量與模型預測出的血糖值變化
    /// </summary>
    public class PredictionResultDto
    {
        // 送入模型的特徵，詳細請移至定義至 FeatureVector
        public FeatureVector Input { get; set; }

        // 模型預測的血糖值 (mg/dL)
        public float PredictedBg { get; set; }
    }
}

