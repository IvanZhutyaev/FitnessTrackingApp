using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessTrackingApp.Models
{
    public class Exercise
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public int CaloriesBurned { get; set; }
        public DateTime Date { get; set; }
    }

    public class WorkoutHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string WorkoutName { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public int CaloriesBurned { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }
    }

    public class WorkoutDetailResponse
    {
        public int Id { get; set; }
        public string WorkoutName { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; } 
        public int CaloriesBurned { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }
    }
    public class Activity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public int Steps { get; set; }
        public double Distance { get; set; }
        public int Calories { get; set; }
    }
    public static class UserSession
    {
        public static int UserId { get; set; }
    }
    public class Meal
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string MealType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Calories { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbs { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    public class NutritionDay
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;
        public int TotalCalories { get; set; }
        public double TotalProtein { get; set; }
        public double TotalFat { get; set; }
        public double TotalCarbs { get; set; }
        public double WaterIntake { get; set; }
        public double WaterGoal { get; set; } = 2.0;
    }

    public class WaterUpdateRequest
    {
        public int UserId { get; set; }
        public double Amount { get; set; }
        public double? Goal { get; set; }
    }

    public class NutritionResponse
    {
        public NutritionDay NutritionDay { get; set; }
        public List<Meal> Meals { get; set; }
    }
}