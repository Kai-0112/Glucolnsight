using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.DTOs
{
    /// <summary>
    /// 將前端輸入的「飲食/運動事件」輸入至伺服器，批次處理
    /// </summary>
    public class CustomPredictionRequest
    {
        // 獲取個人化數據(飲食和運動紀錄及28天CGM的血糖數據)
        public int UserId { get; set; }

        // 血糖預測的基準點
        public DateTimeOffset Time { get; set; }

        // 食物輸入資料集合，詳見請移至定義至 FoodInput
        public required List<FoodInput> FoodInputs { get; set; }

        // 運動輸入資料集合，詳見請移至定義至 ExerciseInput
        public required List<ExerciseInput> ExerciseInputs { get; set; }
    }

    // 情境假設 : “使用者輸入完要吃的早餐及運動” ——> 這些點選即封裝成 CustomPredictionRequest 後送進 API。
}
