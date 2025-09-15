using System;
using Microsoft.EntityFrameworkCore;
using FitnessTrackerBack.Models;

namespace FitnessTrackerBack.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<Exercise> Exercises => Set<Exercise>();
        public DbSet<WorkoutHistory> WorkoutHistory => Set<WorkoutHistory>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationSettings> NotificationSettings => Set<NotificationSettings>();
        public DbSet<Meal> Meals => Set<Meal>();
        public DbSet<NutritionDay> NutritionDays => Set<NutritionDay>();
    }
}
