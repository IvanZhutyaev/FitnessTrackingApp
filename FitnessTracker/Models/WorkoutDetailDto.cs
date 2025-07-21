using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessTrackingApp.Models;

public class WorkoutDetailDto
{
    public int Id { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; } // в минутах
    public int CaloriesBurned { get; set; }
    public DateTime Date { get; set; }
    public string Notes { get; set; } = string.Empty;
}

