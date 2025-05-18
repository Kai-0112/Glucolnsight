using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    public class PredictionMultiResultDto
    {
        public FeatureVector Input { get; set; }
        public float[] PredictedBgs { get; set; }
    }
}
