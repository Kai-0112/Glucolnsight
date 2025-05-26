using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    public class FoodInput
    {
        public int FoodId { get; set; }   // FoodItem.food_id
        public float Portion { get; set; }   // 使用者輸入的份數
        public DateTimeOffset EventTime { get; set; }   // 新增
    }
}
