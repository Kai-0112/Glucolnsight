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

        public float AvgBgPrev30Min { get; set; }
        public float CarbPortion { get; set; }
        public float ExerciseMets { get; set; }
        public float HourOfDay { get; set; }
        public float AvgGlycemicIndex { get; set; }   // 最近一餐加权平均GI
        public float ExerciseDuration { get; set; }   // 最近一次运动时长（分钟）

        public FeatureVector() { }
        public FeatureVector(
        float avgBgPrev30Min,
        float carbPortion,
        float exerciseMets,
        float hourOfDay,
        float avgGlycemicIndex,
        float exerciseDuration)

        {
            AvgBgPrev30Min = avgBgPrev30Min;
            CarbPortion = carbPortion;
            ExerciseMets = exerciseMets;
            HourOfDay = hourOfDay;
            AvgGlycemicIndex = avgGlycemicIndex;
            ExerciseDuration = exerciseDuration;
        }
    }
}
