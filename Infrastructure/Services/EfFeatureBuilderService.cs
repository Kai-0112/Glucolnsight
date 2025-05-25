using ApplicationCore.DTOs;
using ApplicationCore.Services;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class EfFeatureBuilderService : IFeatureBuilderService
    {
        private readonly GlucoInsightContext _ctx;
        public EfFeatureBuilderService(GlucoInsightContext ctx) => _ctx = ctx;

        public async Task<FeatureVector> BuildAsync(int userId, DateTime predictTime)
        {
            const int P = 4; // 滑動視窗長度

            // 1. 取過去 P 筆血糖
            var lastBgs = await _ctx.CGMLog
                .Where(l => l.user_id == userId
                         && l.reading_time < predictTime
                         && l.glucose_mgdl.HasValue)
                .OrderByDescending(l => l.reading_time)
                .Select(l => (float)l.glucose_mgdl.Value)
                .Take(P)
                .ToListAsync();

            // 2. 反轉 & 補零到長度 P
            lastBgs.Reverse();
            while (lastBgs.Count < P)
                lastBgs.Insert(0, 0f);

            // 3. 計算前 30 分鐘平均血糖
            var from = predictTime.AddMinutes(-30);
            float avgBg = await _ctx.CGMLog
                .Where(l => l.user_id == userId
                         && l.reading_time >= from
                         && l.reading_time <= predictTime)
                .AverageAsync(l => (float?)l.glucose_mgdl) ?? 0f;

            // 4. 計算最近一餐碳水、GI
            var latestMeal = await _ctx.MealLog
                .Where(m => m.user_id == userId && m.meal_event_time <= predictTime)
                .OrderByDescending(m => m.meal_event_time)
                .FirstOrDefaultAsync();

            float carbPortion = 0f, avgGI = 0f;
            if (latestMeal != null)
            {
                var items = await _ctx.MealItem
                    .Where(mi => mi.meal_id == latestMeal.meal_id)
                    .Join(_ctx.FoodItem,
                          mi => mi.food_id,
                          f => f.food_id,
                          (mi, f) => new {
                              Portion = (float)mi.portion,
                              Carb = (float)(f.carb_portion_per_serving ?? 0.0),
                              Gi = (float)f.glycemic_index
                          })
                    .ToListAsync();

                var totalPortion = items.Sum(x => x.Portion);
                if (totalPortion > 0)
                {
                    carbPortion = items.Sum(x => x.Portion * x.Carb);
                    avgGI = items.Sum(x => x.Portion * x.Gi) / totalPortion;
                }
            }

            // 5. 計算最近一次運動 METs & 時長
            var lastEx = await _ctx.ExerciseLog
                .Where(e => e.user_id == userId && e.exercise_event_time <= predictTime)
                .OrderByDescending(e => e.exercise_event_time)
                .FirstOrDefaultAsync();

            float mets = lastEx != null ? (float)(lastEx.mets ?? 0m) : 0f;
            float duration = lastEx != null ? (float)(lastEx.duration_min) : 0f;

            // 6. 建立基本 FeatureVector
            var fv = new FeatureVector
            {
                PrevBgs = lastBgs.ToArray(),
                AvgBgPrev30Min = avgBg,
                CarbPortion = carbPortion,
                AvgGlycemicIndex = avgGI,
                ExerciseMets = mets,
                ExerciseDuration = duration,
                HourOfDay = predictTime.Hour
            };

            // 7. 增加時間差與餐型 one-hot 特徵
            if (latestMeal != null)
            {
                fv.MinutesSinceMeal = (float)(predictTime - latestMeal.meal_event_time).TotalMinutes;
                fv.LastMealItemCount = await _ctx.MealItem.CountAsync(mi => mi.meal_id == latestMeal.meal_id);
                // one-hot meal_type
                fv.IsBreakfast = latestMeal.meal_type == "早餐" ? 1f : 0f;
                fv.IsLunch = latestMeal.meal_type == "午餐" ? 1f : 0f;
                fv.IsDinner = latestMeal.meal_type == "晚餐" ? 1f : 0f;
                fv.IsAfternoonTea = latestMeal.meal_type == "下午茶" ? 1f : 0f;
                fv.IsLateNight = latestMeal.meal_type == "消夜" ? 1f : 0f;
            }
            else
            {
                fv.MinutesSinceMeal = 0f;
                fv.LastMealItemCount = 0f;
                fv.IsBreakfast = 
                fv.IsLunch =
                fv.IsDinner =
                fv.IsAfternoonTea =
                fv.IsLateNight = 0f;
            }

            // 8. 計算距離上次運動時間差
            fv.MinutesSinceExercise = lastEx != null
                ? (float)(predictTime - lastEx.exercise_event_time).TotalMinutes
                : 0f;

            return fv;
        }
    }
}
