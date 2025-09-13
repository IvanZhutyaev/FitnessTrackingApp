using System;

namespace FitnessTrackerBack.DTO
{
    public class ChangeInfoRequest
    {
        public int UserId { get; set; }
        public string BirthDate { get; set; } = string.Empty;
        public double Weight { get; set; }
        public double Height { get; set; }

    }
}
