using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    public class CustomPredictionRequest
    {
        public int UserId { get; set; }
        public DateTimeOffset Time { get; set; }

        // 新增：前端送進來的未來事件列表
        public required List<FoodInput> FoodInputs { get; set; }
        public required List<ExerciseInput> ExerciseInputs { get; set; }
    }
}
