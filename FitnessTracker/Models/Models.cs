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

    public static class UserSession
    {
        public static int UserId { get; set; }
    }
}