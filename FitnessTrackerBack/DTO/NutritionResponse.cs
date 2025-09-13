using System;

namespace FitnessTrackerBack.DTO
{
    public class NutritionResponse
    {
        public NutritionDay NutritionDay { get; set; }
        public List<Meal> Meals { get; set; }

    }
}
