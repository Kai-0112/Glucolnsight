// Infrastructure/DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Entities;
// using Infrastructure.Repositories;  // 若有自訂 repo 介面

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var conn = configuration.GetConnectionString("GlucoInsightContext");
        services.AddDbContext<GlucoInsightContext>(opt =>
            opt.UseSqlServer(conn));

        // 例：註冊共用 Repository
        // services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        // 例：註冊 ML 服務
        // services.AddScoped<IGlucosePredictor, MlNetGlucosePredictor>();

        return services;
    }
}

