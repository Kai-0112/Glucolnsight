using System;
using System.Collections.Generic;

namespace Infrastructure.Entities;

public partial class ExerciseItem
{
    public int exercise_id { get; set; }

    public int exercise_category_id { get; set; }

    public string exercise_name { get; set; } = null!;
}
