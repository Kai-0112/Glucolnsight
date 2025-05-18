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

            var fv = await _fb.BuildAsync(req.UserId, req.Time.UtcDateTime);

            // (覆蓋食物/運動特徵的程式碼略，與之前相同)

            // 呼叫多步模型，取得 float[8]
            float[] preds = _ml.PredictAhead(fv);
            var result = new PredictionMultiResultDto
            {
                Input = fv,
                PredictedBgs = preds
            };
            return Ok(result);
        }
    }
}
