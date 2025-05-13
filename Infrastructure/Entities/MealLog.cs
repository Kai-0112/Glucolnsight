using System;
using System.Collections.Generic;

namespace Infrastructure.Entities;

public partial class MealLog
{
    public int meal_id { get; set; }

    public int user_id { get; set; }

    public DateTime meal_event_time { get; set; }

    public string meal_type { get; set; } = null!;
}
