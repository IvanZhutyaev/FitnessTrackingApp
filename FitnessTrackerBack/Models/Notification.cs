using System;

namespace FitnessTrackerBack.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan Time { get; set; }
        public bool IsActive { get; set; } = true;
        public NotificationType Type { get; set; }
    }

    public enum NotificationType
    {
        Meal,
        Workout,
        Water,
        Custom
    }
}
