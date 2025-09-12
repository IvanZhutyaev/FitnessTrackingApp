using System;

namespace FitnessTrackerBack.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string BirthDate { get; set; } = string.Empty;
        public double Weight { get; set; }
        public double Height { get; set; }
        public string Goal { get; set; } = "Похудение";
        public double TargetWeight { get; set; } = 70;
        public string TargetPeriod { get; set; } = "3 месяца";
        public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        //		WorkoutHistory --soon
    }
}
