using Microsoft.ML.Data;
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
        // 新增：過去 4 筆血糖歷史
        [VectorType(4)]
        public float[] PrevBgs { get; set; }

        public float AvgBgPrev30Min { get; set; }
        public float CarbPortion { get; set; }
        public float ExerciseMets { get; set; }
        public float HourOfDay { get; set; }
        public float AvgGlycemicIndex { get; set; }   // 最近一餐加权平均GI
        public float ExerciseDuration { get; set; }   // 最近一次运动时长（分钟）

        public FeatureVector() 
        {
            // 預設給個 4 長度的空陣列
            PrevBgs = new float[4];
        }

        public FeatureVector(
        float[] prevBgs,
        float avgBgPrev30Min,
        float carbPortion,
        float exerciseMets,
        float hourOfDay,
        float avgGlycemicIndex,
        float exerciseDuration)

        {
            PrevBgs = prevBgs;
            AvgBgPrev30Min = avgBgPrev30Min;
            CarbPortion = carbPortion;
            ExerciseMets = exerciseMets;
            HourOfDay = hourOfDay;
            AvgGlycemicIndex = avgGlycemicIndex;
            ExerciseDuration = exerciseDuration;
        }
    }
}
