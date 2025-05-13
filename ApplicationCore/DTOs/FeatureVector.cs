using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    /// <summary>送進 ML 模型的特徵向量</summary>
    public class FeatureVector
    {
        // 無參數 ctor 讓 ML.NET 反射
        public FeatureVector() { }

        // 方便呼叫端仍可用帶參數 ctor
        public FeatureVector(float avgBgPrev30Min,
                             float carbPortion,
                             float exerciseMets,
                             float hourOfDay)
        {
            AvgBgPrev30Min = avgBgPrev30Min;
            CarbPortion = carbPortion;
            ExerciseMets = exerciseMets;
            HourOfDay = hourOfDay;
        }

        // set; 一定要有
        public float AvgBgPrev30Min { get; set; }
        public float CarbPortion { get; set; }
        public float ExerciseMets { get; set; }
        public float HourOfDay { get; set; }
    }
}
