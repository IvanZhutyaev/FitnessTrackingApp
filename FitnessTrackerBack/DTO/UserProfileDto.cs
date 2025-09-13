using System;
using System.Text.Json.Serialization;

namespace FitnessTrackerBack.DTO
{
    public class UserProfileDto
    {

        public string Username { get; set; } = string.Empty;
        [JsonPropertyName("birthDate")]
        public string BirthDate { get; set; } = string.Empty;
        public double Weight { get; set; }
        public double Height { get; set; }
        public string Goal { get; set; } = string.Empty;
        public double TargetWeight { get; set; }
        public string TargetPeriod { get; set; } = string.Empty;

    }
}
