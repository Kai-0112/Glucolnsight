using System;
using System.Collections.Generic;

namespace Infrastructure.Entities;

public partial class ExerciseCategory
{
    public int exercise_category_id { get; set; }

    public string exercise_type { get; set; } = null!;
}
