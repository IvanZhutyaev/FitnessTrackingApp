using System;

namespace FitnessTrackerBack.DTO
{
    public class WaterUpdateRequest
    {
        public int UserId { get; set; }
        public double Amount { get; set; }
        public double? Goal { get; set; }

    }
}
