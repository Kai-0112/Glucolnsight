using System;
using System.Collections.Generic;

namespace Infrastructure.Entities;

public partial class FoodItem
{
    public int food_id { get; set; }

    public int food_category_id { get; set; }

    public string food_name { get; set; } = null!;

    public int glycemic_index { get; set; }

    public decimal default_portion { get; set; }
}
