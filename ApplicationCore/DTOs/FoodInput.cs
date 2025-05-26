using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    /// <summary>
    /// 食物輸入資料
    /// </summary>
    public class FoodInput
    {
        // 食物種類列表
        public int FoodId { get; set; }

        // 食物份數
        public float Portion { get; set; }
        
        // 當下用餐時間
        public DateTimeOffset EventTime { get; set; }
    }
}
