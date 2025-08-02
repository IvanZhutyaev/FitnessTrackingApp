
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=Username=postgres;Password=12345";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/activities/{username}", async (string username, AppDbContext db) =>
{
    var activities = await db.Activities
        .Where(a => a.Username == username)
        .OrderByDescending(a => a.Date)
        .ToListAsync();
    return Results.Ok(activities);
});

app.MapPost("/exercises", async (Exercise exercise, AppDbContext db) =>
{
    db.Exercises.Add(exercise);
    await db.SaveChangesAsync();
    return Results.Created($"/exercises/{exercise.Id}", exercise);
});

app.MapGet("/activities/stats/{username}", async (string username, AppDbContext db) =>
{
    var today = DateTime.UtcNow.Date;
    var weekAgo = today.AddDays(-7);

    var stats = await db.Activities
        .Where(a => a.Username == username && a.Date >= weekAgo)
        .OrderBy(a => a.Date)
        .Select(a => new
        {
            Date = a.Date,
            Steps = a.Steps,
            Distance = a.Distance,
            Calories = a.Calories
        })
        .ToListAsync();

    return Results.Ok(stats);
});

app.MapPost("/login", async (LoginRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest("Имя пользователя и пароль обязательны для заполнения");

    var exists = await db.Users.AnyAsync(user => user.Username == request.Username && user.Password == request.Password);
    if (!exists)
        return Results.BadRequest("Неверный логин или пароль");

    return Results.Ok(new { Success = true, Message = "Пользователь успешно вошел в систему" });
});

app.MapPost("/register", async (RegisterRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.BirthDate))
        return Results.BadRequest("Имя пользователя, пароль и дата рождения обязательны для заполнения");

    var exists = await db.Users.AnyAsync(user => user.Username == request.Username);
    if (exists)
        return Results.BadRequest("Пользователь с таким именем уже существует");

    var user = new User { Username = request.Username, Password = request.Password, BirthDate = request.BirthDate };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { Success = true, Message = "Пользователь успешно зарегистрирован" });
});

app.MapGet("/user/stats", async (string username, AppDbContext db) =>
{
    var user = await db.Users
        .Include(u => u.Activities)
        .FirstOrDefaultAsync(u => u.Username == username);

    if (user == null)
        return Results.NotFound("Пользователь не найден");

    if (!user.Activities.Any())
        return Results.Ok(new { AvgSteps = 0, AvgDistance = 0.0, AvgCalories = 0 });

    var stats = new
    {
        AvgSteps = user.Activities.Average(a => a.Steps),
        AvgDistance = user.Activities.Average(a => a.Distance),
        AvgCalories = user.Activities.Average(a => a.Calories)
    };

    return Results.Ok(stats);
});


// Получение пользователя по username
app.MapGet("/users/{username}", async (string username, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (user == null)
        return Results.NotFound("Пользователь не найден");
    return Results.Ok(user);
});

app.MapGet("/users/profile/{username}", async (string username, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (user == null)
        return Results.NotFound("Пользователь не найден");

    var profileData = new UserProfileDto
    {
        Username = user.Username,
        BirthDate = user.BirthDate,
        Weight = user.Weight,
        Height = user.Height,
        Goal = user.Goal,
        TargetWeight = user.TargetWeight,
        TargetPeriod = user.TargetPeriod
    };

    return Results.Ok(profileData);
});

app.MapGet("/exercises/{userId}", async (int userId, AppDbContext db) =>
{
    var exercises = await db.Exercises
        .Where(e => e.UserId == userId)
        .OrderByDescending(e => e.Date)
        .ToListAsync();
    return Results.Ok(exercises);
});

app.MapPost("/user/changeinfo", async (ChangeInfoRequest request, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
    if (user == null)
        return Results.NotFound("Пользователь не найден");

    user.Weight = request.Weight;
    user.Height = request.Height;

    await db.SaveChangesAsync();
    return Results.Ok(new { Success = true, Message = "Данные пользователя успешно обновлены" });
});

app.MapPost("/user/updateprofile", async (UserProfileDto request, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user == null)
        return Results.NotFound("Пользователь не найден");

    user.Goal = string.IsNullOrEmpty(request.Goal) ? "Похудение" : request.Goal;
    user.BirthDate = request.BirthDate;
    user.Weight = request.Weight;
    user.Height = request.Height;
    user.TargetWeight = request.TargetWeight;
    user.TargetPeriod = request.TargetPeriod;

    await db.SaveChangesAsync();
    return Results.Ok(new { Success = true, Message = "Профиль успешно обновлен" });
});



app.MapDelete("/exercises/{id}", async (int id, AppDbContext db) =>
{
    var exercise = await db.Exercises.FindAsync(id);
    if (exercise == null) return Results.NotFound();

    db.Exercises.Remove(exercise);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapPost("/workouthistory", async (WorkoutHistory history, AppDbContext db) =>
{
    // Добавьте проверку и значения по умолчанию
    history.Description ??= string.Empty;
    history.Notes ??= string.Empty;

    db.WorkoutHistory.Add(history);
    await db.SaveChangesAsync();
    return Results.Created($"/workouthistory/{history.Id}", history);
});

app.MapGet("/workouthistory/{userId}", async (int userId, AppDbContext db) =>
{
    var history = await db.WorkoutHistory
        .Where(h => h.UserId == userId)
        .OrderByDescending(h => h.Date)
        .ToListAsync();
    return Results.Ok(history);
});

app.MapGet("/workouthistory/details/{id}", async (int id, AppDbContext db) =>
{
    var workout = await db.WorkoutHistory.FirstOrDefaultAsync(w => w.Id == id);
    if (workout == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(workout);
});
app.MapGet("/notifications/{userId}", async (int userId, AppDbContext db) =>
{
    var notifications = await db.Notifications
        .Where(n => n.UserId == userId)
        .OrderBy(n => n.Time)
        .ToListAsync();
    return Results.Ok(notifications);
});

app.MapPost("/notifications", async (Notification notification, AppDbContext db) =>
{
    db.Notifications.Add(notification);
    await db.SaveChangesAsync();
    return Results.Created($"/notifications/{notification.Id}", notification);
});

app.MapPut("/notifications/{id}/status", async (int id, bool isActive, AppDbContext db) =>
{
    var notification = await db.Notifications.FindAsync(id);
    if (notification == null) return Results.NotFound();

    notification.IsActive = isActive;
    await db.SaveChangesAsync();
    return Results.Ok(notification);
});

app.MapDelete("/notifications/{id}", async (int id, AppDbContext db) =>
{
    var notification = await db.Notifications.FindAsync(id);
    if (notification == null) return Results.NotFound();

    db.Notifications.Remove(notification);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/notifications/settings/{userId}", async (int userId, AppDbContext db) =>
{
    var settings = await db.NotificationSettings.FirstOrDefaultAsync(n => n.UserId == userId);
    if (settings == null)
    {
        settings = new NotificationSettings { UserId = userId };
        db.NotificationSettings.Add(settings);
        await db.SaveChangesAsync();
    }
    return Results.Ok(settings);
});

app.MapPut("/notifications/settings/{userId}", async (int userId, NotificationSettings updatedSettings, AppDbContext db) =>
{
    var settings = await db.NotificationSettings.FirstOrDefaultAsync(n => n.UserId == userId);
    if (settings == null) return Results.NotFound();

    settings.MotivationalEnabled = updatedSettings.MotivationalEnabled;
    settings.AchievementsEnabled = updatedSettings.AchievementsEnabled;
    settings.ProgressEnabled = updatedSettings.ProgressEnabled;
    settings.GeneralEnabled = updatedSettings.GeneralEnabled;
    settings.SoundEnabled = updatedSettings.SoundEnabled;
    settings.VibrationEnabled = updatedSettings.VibrationEnabled;

    await db.SaveChangesAsync();
    return Results.Ok(settings);
});

app.MapPost("/user/steps", async (Activity activity, AppDbContext db) =>
{
    var today = DateTime.UtcNow.Date;
    var existingActivity = await db.Activities
        .FirstOrDefaultAsync(a => a.Username == activity.Username && a.Date.Date == today);

    if (existingActivity != null)
    {
        existingActivity.Steps = activity.Steps;
        existingActivity.Distance = activity.Distance;
        existingActivity.Calories = activity.Calories;
    }
    else
    {
        activity.Date = today;
        db.Activities.Add(activity);
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { Success = true, Message = "Данные успешно сохранены" });
});
app.MapGet("/nutrition/{userId}", async (int userId, AppDbContext db) =>
{
    var today = DateTime.UtcNow.Date;
    var nutritionDay = await db.NutritionDays
        .FirstOrDefaultAsync(n => n.UserId == userId && n.Date == today);

    if (nutritionDay == null)
    {
        nutritionDay = new NutritionDay
        {
            UserId = userId,
            Date = today,
            WaterGoal = 2.0,
            TotalCalories = 0,
            TotalProtein = 0,
            TotalFat = 0,
            TotalCarbs = 0,
            WaterIntake = 0
        };
        db.NutritionDays.Add(nutritionDay);
        await db.SaveChangesAsync();
    }

    var meals = await db.Meals
        .Where(m => m.UserId == userId && m.Date.Date == today)
        .OrderBy(m => m.Date)
        .ToListAsync();

    return Results.Ok(new { NutritionDay = nutritionDay, Meals = meals });
});

app.MapPost("/meals", async (Meal meal, AppDbContext db) =>
{
    meal.Date = DateTime.UtcNow;
    db.Meals.Add(meal);

    var today = DateTime.UtcNow.Date;
    var nutritionDay = await db.NutritionDays
        .FirstOrDefaultAsync(n => n.UserId == meal.UserId && n.Date == today);

    if (nutritionDay == null)
    {
        nutritionDay = new NutritionDay
        {
            UserId = meal.UserId,
            Date = today,
            WaterGoal = 2.0
        };
        db.NutritionDays.Add(nutritionDay);
    }

    nutritionDay.TotalCalories += meal.Calories;
    nutritionDay.TotalProtein += meal.Protein;
    nutritionDay.TotalFat += meal.Fat;
    nutritionDay.TotalCarbs += meal.Carbs;

    await db.SaveChangesAsync();
    return Results.Created($"/meals/{meal.Id}", meal);
});

app.MapPost("/water", async (WaterUpdateRequest request, AppDbContext db) =>
{
    var today = DateTime.UtcNow.Date;
    var nutritionDay = await db.NutritionDays
        .FirstOrDefaultAsync(n => n.UserId == request.UserId && n.Date == today);

    if (nutritionDay == null)
    {
        nutritionDay = new NutritionDay
        {
            UserId = request.UserId,
            Date = today,
            WaterGoal = 2.0
        };
        db.NutritionDays.Add(nutritionDay);
    }

    nutritionDay.WaterIntake = request.Amount;
    if (request.Goal.HasValue)
    {
        nutritionDay.WaterGoal = request.Goal.Value;
    }

    await db.SaveChangesAsync();
    return Results.Ok(nutritionDay);
});
app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() =>
{
    var timer = new System.Timers.Timer();
    timer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
    timer.Elapsed += async (sender, e) =>
    {
        if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0)
        {
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await ResetDailyNutritionData(db);
            }
        }
    };
    timer.Start();
});

async Task ResetDailyNutritionData(AppDbContext db)
{
    var yesterday = DateTime.UtcNow.Date.AddDays(-1);
    var daysToReset = await db.NutritionDays
        .Where(n => n.Date == yesterday)
        .ToListAsync();

    foreach (var day in daysToReset)
    {
        day.TotalCalories = 0;
        day.TotalProtein = 0;
        day.TotalFat = 0;
        day.TotalCarbs = 0;
        day.WaterIntake = 0;
    }

    await db.SaveChangesAsync();
}

app.Run();

public class WaterUpdateRequest
{
    public int UserId { get; set; }
    public double Amount { get; set; }
    public double? Goal { get; set; }
}
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public double Weight { get; set; }
    public double Height { get; set; }
    public string Goal { get; set; } = "Похудение";
    public double TargetWeight { get; set; } = 70;
    public string TargetPeriod { get; set; } = "3 месяца";
    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();

}

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
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
}

public class WorkoutHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int Duration { get; set; } // в минутах
    public int CaloriesBurned { get; set; }
    public string Notes { get; set; } = string.Empty;
}

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

public class Exercise
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; }
    public int CaloriesBurned { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;

}
public class ChangeInfoRequest
{

    public int UserId { get; set; }
    public string BirthDate { get; set; } = string.Empty;
    public double Weight { get; set; }
    public double Height { get; set; }

}
public class WorkoutDetailResponse
{
    public int Id { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; } // в минутах
    public int CaloriesBurned { get; set; }
    public DateTime Date { get; set; }
    public string Notes { get; set; } = string.Empty;
}
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

public class Meal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string MealType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Calories { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Carbs { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}
public class NutritionDay
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public int TotalCalories { get; set; }
    public double TotalProtein { get; set; }
    public double TotalFat { get; set; }
    public double TotalCarbs { get; set; }
    public double WaterIntake { get; set; }
    public double WaterGoal { get; set; } = 2.0;
}
public class NutritionResponse
{
    public NutritionDay NutritionDay { get; set; }
    public List<Meal> Meals { get; set; }
}
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutHistory> WorkoutHistory => Set<WorkoutHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationSettings> NotificationSettings => Set<NotificationSettings>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<NutritionDay> NutritionDays => Set<NutritionDay>();
}
