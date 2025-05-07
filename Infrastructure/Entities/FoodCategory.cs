using System;
using System.Collections.Generic;

namespace Infrastructure.Entities;

public partial class FoodCategory
{
    public int food_category_id { get; set; }

    public string food_type { get; set; } = null!;
}
