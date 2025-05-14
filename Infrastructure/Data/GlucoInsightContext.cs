using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Entities;

public partial class GlucoInsightContext : DbContext
{
    public GlucoInsightContext()
    {
    }

    public GlucoInsightContext(DbContextOptions<GlucoInsightContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CGMLog> CGMLog { get; set; }

    public virtual DbSet<ExerciseCategory> ExerciseCategory { get; set; }

    public virtual DbSet<ExerciseItem> ExerciseItem { get; set; }

    public virtual DbSet<ExerciseLog> ExerciseLog { get; set; }

    public virtual DbSet<FoodCategory> FoodCategory { get; set; }

    public virtual DbSet<FoodItem> FoodItem { get; set; }

    public virtual DbSet<MealItem> MealItem { get; set; }

    public virtual DbSet<MealLog> MealLog { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    => optionsBuilder.UseSqlServer("Name=ConnectionStrings:GlucoInsightContext");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CGMLog>(entity =>
        {
            entity.HasKey(e => e.cgm_log_id);

            entity.Property(e => e.reading_time).HasColumnType("datetime");
        });

        modelBuilder.Entity<ExerciseCategory>(entity =>
        {
            entity.HasKey(e => e.exercise_category_id);

            entity.Property(e => e.exercise_type).HasMaxLength(50);
        });

        modelBuilder.Entity<ExerciseItem>(entity =>
        {
            entity.HasKey(e => e.exercise_id);

            entity.Property(e => e.exercise_name).HasMaxLength(100);
        });

        modelBuilder.Entity<ExerciseLog>(entity =>
        {
            entity.HasKey(e => e.exercise_log_id);

            entity.Property(e => e.exercise_event_time).HasColumnType("datetime");
            entity.Property(e => e.meal_relation).HasMaxLength(20);
        });

        modelBuilder.Entity<FoodCategory>(entity =>
        {
            entity.HasKey(e => e.food_category_id);

            entity.Property(e => e.food_type).HasMaxLength(50);
        });

        modelBuilder.Entity<FoodItem>(entity =>
        {
            entity.HasKey(e => e.food_id);

            entity.Property(e => e.default_portion).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.food_name).HasMaxLength(100);
        });

        modelBuilder.Entity<MealItem>(entity =>
        {
            entity.HasKey(e => e.meal_item_id);

            entity.Property(e => e.portion).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<MealLog>(entity =>
        {
            entity.HasKey(e => e.meal_id);

            entity.Property(e => e.meal_event_time).HasColumnType("datetime");
            entity.Property(e => e.meal_type).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
