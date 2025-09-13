using System;

namespace FitnessTrackerBack.Models
{
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
}
