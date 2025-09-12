using System;

namespace FitnessTrackerBack.Models
{
    public class Exercise
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int CaloriesBurned { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
