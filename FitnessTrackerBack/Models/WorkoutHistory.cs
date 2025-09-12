using System;


namespace FitnessTrackerBack.Models
{
    public class WorkoutHistory
    {

        public int Id { get; set; }
        public int UserId { get; set; }
        public string WorkoutName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public int Duration { get; set; } // в минутах
        public int CaloriesBurned { get; set; }
        public string Notes { get; set; } = string.Empty;

    }
}

