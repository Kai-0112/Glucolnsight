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

        /// <summary>
        /// Demo endpoint: 回傳一組範例特徵向量的預測結果
        /// </summary>
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

            // _ml.Predict 現在回傳 float
            float bg = _ml.Predict(fv);
            var result = new PredictionResultDto
            {
                Input = fv,
                PredictedBg = bg
            };
            return Ok(result);
        }

        /// <summary>
        /// 單點預測：根據 userId 與 time，回傳下一筆預測
        /// </summary>
        [HttpGet("")]
        [ProducesResponseType(typeof(PredictionResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Predict(
            [FromQuery, Required] int userId,
            [FromQuery, Required] DateTimeOffset time)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation(
                "Predict called: userId={UserId}, time={Time}",
                userId, time);

            var fv = await _fb.BuildAsync(userId, time.UtcDateTime);

            // _ml.Predict 現在回傳 float
            float bg = _ml.Predict(fv);
            var result = new PredictionResultDto
            {
                Input = fv,
                PredictedBg = bg
            };
            return Ok(result);
        }

        /// <summary>
        /// 自訂單點預測：結合 DB 歷史 + 前端事件
        /// </summary>
        [HttpPost("custom")]
        [ProducesResponseType(typeof(PredictionResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PredictCustom(
            [FromBody] CustomPredictionRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var fv = await _fb.BuildAsync(req.UserId, req.Time.UtcDateTime);

            // (覆蓋食物/運動特徵的程式碼略，與之前相同)

            // 呼叫單點模型，並直接取得 float
            float bg = _ml.Predict(fv);
            var result = new PredictionResultDto
            {
                Input = fv,
                PredictedBg = bg
            };
            return Ok(result);
        }

        /// <summary>
        /// 自訂多步趨勢預測（未來 8 點，每點 15 分鐘）
        /// </summary>
        [HttpPost("custom/multi")]
        [ProducesResponseType(typeof(PredictionMultiResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PredictCustomMulti(
    [FromBody] CustomPredictionRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) 先用 DB 歷史建出基本特徵（PrevBgs, AvgBgPrev30Min, HourOfDay…）
            var fv = await _fb.BuildAsync(req.UserId, req.Time.UtcDateTime);

            // 2) **覆蓋** 前端送來的食物特徵
            if (req.FoodInputs?.Any() == true)
            {
                var details = await _ctx.FoodItem
                    .Where(f => req.FoodInputs.Select(x => x.FoodId).Contains(f.food_id))
                    .Select(f => new {
                        f.food_id,
                        CarbPerServing = f.carb_gram_per_serving ?? 0.0,
                        Gi = f.glycemic_index
                    })
                    .ToListAsync();

                float totalCarb = 0f, giSum = 0f, portionSum = 0f;
                foreach (var fi in req.FoodInputs)
                {
                    var d = details.First(x => x.food_id == fi.FoodId);
                    totalCarb += (float)(d.CarbPerServing * fi.Portion);
                    giSum += d.Gi * fi.Portion;
                    portionSum += fi.Portion;
                }
                fv.CarbPortion = totalCarb;
                fv.AvgGlycemicIndex = portionSum > 0 ? giSum / portionSum : 0f;
            }

            // 3) **覆蓋** 前端送來的運動特徵
            if (req.ExerciseInputs?.Any() == true)
            {
                var details = await _ctx.ExerciseLog
                    .Where(el => req.ExerciseInputs.Select(x => x.ExerciseId)
                                       .Contains(el.exercise_id))
                    .OrderByDescending(el => el.exercise_event_time)
                    .GroupBy(el => el.exercise_id)
                    .Select(g => new {
                        ExerciseId = g.Key,
                        Mets = g.First().mets ?? 0m
                    })
                    .ToListAsync();

                float totalMets = 0f, totalDur = 0f;
                foreach (var ei in req.ExerciseInputs)
                {
                    var d = details.First(x => x.ExerciseId == ei.ExerciseId);
                    totalMets += (float)d.Mets;
                    totalDur += ei.DurationMin;
                }
                fv.ExerciseMets = totalMets;
                fv.ExerciseDuration = totalDur;
            }

            // 4) 呼叫多步預測：這時 fv 裡所有特徵都包含你的手動輸入
            float[] preds = _ml.PredictAhead(fv);

            return Ok(new PredictionMultiResultDto
            {
                Input = fv,
                PredictedBgs = preds
            });
        }

    }
}
