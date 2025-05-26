using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore.DTOs;
using ApplicationCore.Services;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GlucoInsight.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PredictionController : ControllerBase
    {
        private readonly IGlucosePredictionService _ml;
        private readonly IFeatureBuilderService _fb;
        private readonly GlucoInsightContext _ctx;
        private readonly ILogger<PredictionController> _logger;

        public PredictionController(
            IGlucosePredictionService ml,
            IFeatureBuilderService fb,
            GlucoInsightContext ctx,
            ILogger<PredictionController> logger)
        {
            _ml = ml;
            _fb = fb;
            _ctx = ctx;
            _logger = logger;
        }

        [HttpGet("demo")]
        [ProducesResponseType(typeof(PredictionResultDto), 200)]
        public IActionResult Demo()
        {
            var fv = new FeatureVector
            {
                AvgBgPrev30Min = 100,
                CarbPortion = 0,
                ExerciseMets = 0,
                HourOfDay = 10,
                AvgGlycemicIndex = 0f,
                ExerciseDuration = 0f
            };

            float bg = _ml.Predict(fv);
            return Ok(new PredictionResultDto
            {
                Input = fv,
                PredictedBg = bg
            });
        }

        [HttpGet("")]
        [ProducesResponseType(typeof(PredictionResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Predict(
            [FromQuery, Required] int userId,
            [FromQuery, Required] DateTimeOffset time)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation("Predict called: userId={UserId}, time={Time}",
                                    userId, time);

            var fv = await _fb.BuildAsync(userId, time.UtcDateTime);
            float bg = _ml.Predict(fv);

            return Ok(new PredictionResultDto
            {
                Input = fv,
                PredictedBg = bg
            });
        }

        [HttpPost("custom")]
        [ProducesResponseType(typeof(PredictionResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PredictCustom(
            [FromBody] CustomPredictionRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) 先拿 DB 歷史
            var fv = await _fb.BuildAsync(req.UserId, req.Time.UtcDateTime);

            // 2)Override: 用前端傳的食物事件時間 & 份量算 MinutesSinceMeal + 碳水/GI
            if (req.FoodInputs?.Any() == true)
            {
                // 取最近一筆
                var lastFood = req.FoodInputs
                                  .OrderBy(f => f.EventTime)
                                  .Last();
                // 距離用餐分鐘
                fv.MinutesSinceMeal =
                    (float)(req.Time - lastFood.EventTime).TotalMinutes;

                // 碳水 & GI
                var details = await _ctx.FoodItem
                    .Where(f => req.FoodInputs.Select(x => x.FoodId)
                                               .Contains(f.food_id))
                    .Select(f => new {
                        f.food_id,
                        Carb = (float)(f.carb_gram_per_serving ?? 0.0),
                        Gi = (float)f.glycemic_index
                    })
                    .ToListAsync();

                float totalCarb = 0f, giSum = 0f, portSum = 0f;
                foreach (var fi in req.FoodInputs)
                {
                    var d = details.First(x => x.food_id == fi.FoodId);
                    totalCarb += d.Carb * fi.Portion;
                    giSum += d.Gi * fi.Portion;
                    portSum += fi.Portion;
                }

                fv.CarbPortion = totalCarb;
                fv.AvgGlycemicIndex = portSum > 0 ? giSum / portSum : 0f;
            }

            // 3)Override: 用前端運動事件時間 & 時長算 MinutesSinceExercise + Mets
            if (req.ExerciseInputs?.Any() == true)
            {
                var lastEx = req.ExerciseInputs
                                 .OrderBy(e => e.EventTime)
                                 .Last();
                fv.MinutesSinceExercise =
                    (float)(req.Time - lastEx.EventTime).TotalMinutes;
                fv.ExerciseDuration = lastEx.DurationMin;

                // 從 DB 拿對應 mets
                var mets = await _ctx.ExerciseLog
                    .Where(e => e.exercise_id == lastEx.ExerciseId)
                    .OrderByDescending(e => e.exercise_event_time)
                    .Select(e => (float?)(e.mets ?? 0m))
                    .FirstOrDefaultAsync() ?? 0f;
                fv.ExerciseMets = mets;
            }

            // 4) 單點預測
            float pred = _ml.Predict(fv);
            return Ok(new PredictionResultDto
            {
                Input = fv,
                PredictedBg = pred
            });
        }

        [HttpPost("custom/multi")]
        [ProducesResponseType(typeof(PredictionMultiResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PredictCustomMulti(
            [FromBody] CustomPredictionRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) DB 歷史
            var fv = await _fb.BuildAsync(req.UserId, req.Time.UtcDateTime);

            // 2)Override 食物
            if (req.FoodInputs?.Any() == true)
            {
                var lastFood = req.FoodInputs
                                  .OrderBy(f => f.EventTime)
                                  .Last();
                fv.MinutesSinceMeal =
                    (float)(req.Time - lastFood.EventTime).TotalMinutes;

                var details = await _ctx.FoodItem
                    .Where(f => req.FoodInputs.Select(x => x.FoodId)
                                               .Contains(f.food_id))
                    .Select(f => new {
                        f.food_id,
                        Carb = (float)(f.carb_gram_per_serving ?? 0.0),
                        Gi = (float)f.glycemic_index
                    })
                    .ToListAsync();

                float tc = 0f, giS = 0f, pS = 0f;
                foreach (var fi in req.FoodInputs)
                {
                    var d = details.First(x => x.food_id == fi.FoodId);
                    tc += d.Carb * fi.Portion;
                    giS += d.Gi * fi.Portion;
                    pS += fi.Portion;
                }
                fv.CarbPortion = tc;
                fv.AvgGlycemicIndex = pS > 0 ? giS / pS : 0f;
            }

            // 3)Override 運動
            if (req.ExerciseInputs?.Any() == true)
            {
                var lastEx = req.ExerciseInputs
                                 .OrderBy(e => e.EventTime)
                                 .Last();
                fv.MinutesSinceExercise =
                    (float)(req.Time - lastEx.EventTime).TotalMinutes;
                fv.ExerciseDuration = lastEx.DurationMin;

                var mets = await _ctx.ExerciseLog
                    .Where(e => e.exercise_id == lastEx.ExerciseId)
                    .OrderByDescending(e => e.exercise_event_time)
                    .Select(e => (float?)(e.mets ?? 0m))
                    .FirstOrDefaultAsync() ?? 0f;
                fv.ExerciseMets = mets;
            }

            // 4) 多步預測
            var preds = _ml.PredictAhead(fv);

            return Ok(new PredictionMultiResultDto
            {
                Input = fv,
                PredictedBgs = preds
            });
        }
    }
}
