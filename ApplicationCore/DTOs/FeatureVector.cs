using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    /// <summary>
    /// 送進 ML 模型的特徵
    /// </summary>
    public class FeatureVector
    {
        // 新增：過去 4 筆血糖歷史
        [VectorType(4)]

        // 過去 1 小時內每 15 分鐘的資料
        public float[] PrevBgs { get; set; }

        // 前 30 分鐘的血糖波動
        public float AvgBgPrev30Min { get; set; }

        // 碳水化合物計算
        public float CarbPortion { get; set; }

        // 運動強度 (METs)
        public float ExerciseMets { get; set; }

        // 一天的時間
        public float HourOfDay { get; set; }

        // 平均血糖指數
        public float AvgGlycemicIndex { get; set; }

        // 運動持續時間
        public float ExerciseDuration { get; set; }

        // 用餐時段
        public float MinutesSinceMeal { get; set; }
        public float MinutesSinceExercise { get; set; }
        public float LastMealItemCount { get; set; }
        public float IsBreakfast { get; set; }
        public float IsLunch { get; set; }
        public float IsDinner { get; set; }
        public float IsAfternoonTea { get; set; }
        public float IsLateNight { get; set; }

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
