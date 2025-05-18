using System;
using System.Collections.Generic;

namespace Infrastructure.Entities;

public partial class ExerciseLog
{
    public int exercise_log_id { get; set; }

    public int user_id { get; set; }

    public DateTime exercise_event_time { get; set; }

    public string? meal_relation { get; set; }

    public int exercise_id { get; set; }

    public int duration_min { get; set; }

    public int sets_count { get; set; }

    public decimal? mets { get; set; }
}
