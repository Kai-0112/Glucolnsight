using ApplicationCore.Interfaces;
using ApplicationCore.Services;
using Infrastructure.Entities;
using Infrastructure.ML;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;

namespace Glucolnsight
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<GlucoInsightContext>(opt =>
            opt.UseSqlServer(builder.Configuration.GetConnectionString("GlucoInsightContext")));

            builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IGlucosePredictionService, MlNetGlucosePredictionService>();



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

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
