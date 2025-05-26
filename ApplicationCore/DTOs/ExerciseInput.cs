using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    public class ExerciseInput
    {
        // 運動種類列表
        public int ExerciseId { get; set; }

        // 運動持續時間
        public float DurationMin { get; set; }

        // 運動組數
        public float? SetsCount { get; set; }

        // 當下運動時間
        public DateTimeOffset EventTime { get; set; }
    }
}
