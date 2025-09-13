using System;

namespace FitnessTrackerBack.Models
{
    public class NotificationSettings
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool MotivationalEnabled { get; set; } = true;
        public bool AchievementsEnabled { get; set; } = true;
        public bool ProgressEnabled { get; set; } = true;
        public bool GeneralEnabled { get; set; } = true;
        public bool SoundEnabled { get; set; } = true;
        public bool VibrationEnabled { get; set; } = true;
        public TimeSpan NotificationTime { get; set; } = new TimeSpan(9, 0, 0);


    }
}
