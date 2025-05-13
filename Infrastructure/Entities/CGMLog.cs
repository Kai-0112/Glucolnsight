using System;
using System.Collections.Generic;

namespace Infrastructure.Entities;

public partial class CGMLog
{
    public int cgm_log_id { get; set; }

    public int? user_id { get; set; }

    public DateTime? reading_time { get; set; }

    public int? glucose_mgdl { get; set; }

    public int? meal_id { get; set; }

    public int? exercise_log_id { get; set; }
}
