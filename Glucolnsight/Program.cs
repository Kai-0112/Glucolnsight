using ApplicationCore.Interfaces;
using ApplicationCore.Services;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Glucolnsight
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews(); // for your MVC views

            builder.Services.AddControllers(); // for attribute?routed APIs

            builder.Services.AddDbContext<GlucoInsightContext>(opt =>
            opt.UseSqlServer(builder.Configuration.GetConnectionString("GlucoInsightContext")));

            builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IGlucosePredictionService, MlNetGlucosePredictionService>();

            builder.Services.AddScoped<IFeatureBuilderService, EfFeatureBuilderService>();

            builder.Services.AddLogging();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthorization();

            app.MapControllers();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
