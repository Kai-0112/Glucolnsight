using Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HistoryController : Controller
    {
        private readonly GlucoInsightContext _ctx;
        public HistoryController(GlucoInsightContext ctx) => _ctx = ctx;
        /// <summary>
        /// 取得所有食物清單
        /// GET /api/v1/history/foods
        /// </summary>
        [HttpGet("foods")]
        public async Task<IActionResult> Foods()
        {
            var list = await _ctx.FoodItem
                .Select(f => new {
                    f.food_id,
                    f.food_name,
                    f.glycemic_index
                })
                .ToListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 取得使用者曾執行過的運動清單（含最新 METs）
        /// GET /api/v1/history/exercises?userId=1
        /// </summary>
        /// <summary>
        /// 取得所有運動清單
        /// GET /api/v1/history/exercises
        /// </summary>
        [HttpGet("exercises")]
        public async Task<IActionResult> Exercises()
        {
            var list = await _ctx.ExerciseItem
                .Select(e => new {
                    e.exercise_id,
                    e.exercise_name
                })
                .ToListAsync();
            return Ok(list);
        }
    }
}
