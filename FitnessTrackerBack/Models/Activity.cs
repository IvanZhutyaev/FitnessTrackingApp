using System;

namespace FitnessTrackerBack.Models
{
    public class Activity
    {

        public int Id { get; set; }
        public string? Username { get; set; }
        public User? User { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public int Steps { get; set; }
        public double Distance { get; set; }
        public int Calories { get; set; }


    }
}
