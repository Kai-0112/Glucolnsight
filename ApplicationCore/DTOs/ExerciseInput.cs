using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    public class ExerciseInput
    {
        public int ExerciseId { get; set; }   // ExerciseItem.exercise_id
        public float DurationMin { get; set; }   // 使用者輸入的分鐘數
        public float? SetsCount { get; set; }   // 可選：組數
    }
}
