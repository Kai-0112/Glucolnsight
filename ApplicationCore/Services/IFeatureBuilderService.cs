using ApplicationCore.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Services
{
    /// <summary>
    /// 使用者在特定時間點」所需的所有原始資料（最近血糖、餐點、運動…)
    /// </summary>
    public interface IFeatureBuilderService
    {
        Task<FeatureVector> BuildAsync(int userId, DateTime predictTime);
    }
}
