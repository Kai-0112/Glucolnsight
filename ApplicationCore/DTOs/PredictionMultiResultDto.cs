using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    /// <summary>
    /// 多個時間點 / 多組 scenario 時使用（例：早餐 + 午餐 + 運動，一次預測 3 筆）
    /// </summary>
    public class PredictionMultiResultDto
    {
        public FeatureVector Input { get; set; }
        public float[] PredictedBgs { get; set; }
        public List<ExerciseInput> ExerciseInputs { get; set; }
    }
}
