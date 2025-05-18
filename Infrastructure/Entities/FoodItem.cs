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

    public double? serving_weight_g { get; set; }

    public double? carb_gram_per_serving { get; set; }

    public double? carb_portion_per_serving { get; set; }
}
