using System;
using System.Collections.Generic;

namespace Infrastructure.Entities;

public partial class MealItem
{
    public int meal_item_id { get; set; }

    public int meal_id { get; set; }

    public int food_id { get; set; }

    public decimal portion { get; set; }
}
