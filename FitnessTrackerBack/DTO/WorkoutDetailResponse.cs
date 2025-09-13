using System;

namespace FitnessTrackerBack.DTO
{
    public class WorkoutDetailResponse
    {
        public int Id { get; set; }
        public string WorkoutName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Duration { get; set; } // в минутах
        public int CaloriesBurned { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; } = string.Empty;

    }
}
