using System;

namespace FitnessTrackerBack.Models
{
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
}
