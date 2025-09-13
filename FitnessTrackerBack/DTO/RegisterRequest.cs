using System;

namespace FitnessTrackerBack.DTO
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string BirthDate { get; set; } = string.Empty;

    }
}
