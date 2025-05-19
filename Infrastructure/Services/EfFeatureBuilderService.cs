using ApplicationCore.DTOs;
using ApplicationCore.Services;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            // 1. 讀取過去 P 筆血糖 (reading_time < predictTime)
            var lastBgs = await _ctx.CGMLog
                .Where(l => l.user_id == userId
                         && l.reading_time < predictTime
                         && l.glucose_mgdl.HasValue)
                .OrderByDescending(l => l.reading_time)
                .Select(l => (float)l.glucose_mgdl.Value)
                .Take(P)
                .ToListAsync();

            // 2. 反轉成由遠到近順序，並補零到長度 P
            lastBgs.Reverse();
            while (lastBgs.Count < P)
                lastBgs.Insert(0, 0f);


            // ❶ 取前 30 分鐘平均 BG
            var from = predictTime.AddMinutes(-30);
            var avgBg = await _ctx.CGMLog
                           .Where(l => l.user_id == userId && l.reading_time >= from && l.reading_time <= predictTime)
                           .AverageAsync(l => (float?)l.glucose_mgdl) ?? 0;

            // ❷ 從 MealItem＋FoodItem 算最近一餐的碳水份數
            var latestMealId = await _ctx.MealLog
                .Where(m => m.user_id == userId && m.meal_event_time <= predictTime)
                .OrderByDescending(m => m.meal_event_time)
                .Select(m => (int?)m.meal_id)
                .FirstOrDefaultAsync();

            // 先取出整餐的 carbPortion（decimal）
            double carbDouble = 0d;
            if (latestMealId.HasValue)
            {
                carbDouble = await _ctx.MealItem
                    .Where(mi => mi.meal_id == latestMealId.Value)
                    .Join(_ctx.FoodItem,
                          mi => mi.food_id,
                          f => f.food_id,
                          (mi, f) => (double)mi.portion * (f.carb_portion_per_serving ?? 0.0))
                    .SumAsync();
            }

            // 最後一次性 cast 成 float
            float carb = (float)carbDouble;


            // ❸ 取最近一次運動 METs
            var mets = await _ctx.ExerciseLog
                       .Where(e => e.user_id == userId && e.exercise_event_time <= predictTime)
                       .OrderByDescending(e => e.exercise_event_time)
                       .Select(e => (float?)(e.mets.HasValue ? e.mets.Value : 0m))
                       .FirstOrDefaultAsync() ?? 0f;

            // —— ❹ 取最近一餐的加权平均 GI ——  
            float avgGI = 0f;
            if (latestMealId.HasValue)
            {
                var q = _ctx.MealItem
                    .Where(mi => mi.meal_id == latestMealId.Value)
                    .Join(_ctx.FoodItem,
                          mi => mi.food_id, f => f.food_id,
                          (mi, f) => new {
                              Gi = (float)f.glycemic_index,
                              Portion = (float)mi.portion
                          });
                var totalPortion = await q.SumAsync(x => x.Portion);
                if (totalPortion > 0)
                {
                    var weightedGi = await q.SumAsync(x => x.Gi * x.Portion);
                    avgGI = weightedGi / totalPortion;
                }
            }

            // —— ❺ 取最近一次运动的时长 ——  
            float duration = await _ctx.ExerciseLog
                .Where(e => e.user_id == userId
                         && e.exercise_event_time <= predictTime)
                .OrderByDescending(e => e.exercise_event_time)
                .Select(e => (float?)e.duration_min)
                .FirstOrDefaultAsync() ?? 0f;


            // 最後建立 FeatureVector（用物件初始器方式）
            var fv = new FeatureVector();
            fv.PrevBgs = lastBgs.ToArray();
            fv.AvgBgPrev30Min = avgBg;
            fv.CarbPortion = carb;
            fv.AvgGlycemicIndex = avgGI;
            fv.ExerciseMets = mets;
            fv.ExerciseDuration = duration;
            fv.HourOfDay = predictTime.Hour;
            return fv;
        }
    }
}
