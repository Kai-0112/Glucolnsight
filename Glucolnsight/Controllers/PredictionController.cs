using ApplicationCore.DTOs;
using ApplicationCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace Glucolnsight.Controllers;   // ★ 與 Program.cs 同 Assembly

[ApiController]
public class PredictionController : ControllerBase
{
    private readonly IGlucosePredictionService _ml;
    public PredictionController(IGlucosePredictionService ml) => _ml = ml;

    [HttpGet("/api/predict/demo")]   // ★ 完整路徑
    public IActionResult Demo()
    {
        var fv = new FeatureVector(100, 0, 0, 10);
        return Ok(_ml.Predict(fv));
    }
}

