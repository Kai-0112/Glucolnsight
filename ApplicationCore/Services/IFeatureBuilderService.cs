using ApplicationCore.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Services
{
    public interface IFeatureBuilderService
    {
        Task<FeatureVector> BuildAsync(int userId, DateTime predictTime);
    }
}
