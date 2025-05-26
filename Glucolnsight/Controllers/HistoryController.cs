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
        /// 取得所有食物類型
        /// GET /api/v1/history/foodcategories
        /// </summary>
        [HttpGet("foodcategories")]
        public async Task<IActionResult> FoodCategories()
        {
            var list = await _ctx.FoodCategory
                .Select(c => new {
                    c.food_category_id,
                    c.food_type
                })
                .ToListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 取得所有食物清單（可依 categoryId 過濾）
        /// GET /api/v1/history/foods
        /// GET /api/v1/history/foods?categoryId=2
        /// </summary>
        [HttpGet("foods")]
        public async Task<IActionResult> Foods([FromQuery] int? categoryId)
        {
            var query = _ctx.FoodItem.AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(f => f.food_category_id == categoryId.Value);

            var list = await query
                .Select(f => new {
                    f.food_id,
                    f.food_name,
                    f.glycemic_index,
                    f.food_category_id
                })
                .ToListAsync();

            return Ok(list);
        }

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
