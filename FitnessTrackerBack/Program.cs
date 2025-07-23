
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json; 
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка подключения к базе данных PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=12345";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Настройка Swagger для разработки
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Отключено для локального тестирования
// app.UseHttpsRedirection();

// Маршруты API

// Получение активностей пользователя
app.MapGet("/activities/{userId}", async (int userId, AppDbContext db) =>
{
    var activities = await db.Activities
        .Where(a => a.UserId == userId)
        .OrderByDescending(a => a.Date)
        .ToListAsync();
    return Results.Ok(activities);
});

// Добавление новой активности
app.MapPost("/exercises", async (Exercise exercise, AppDbContext db) =>
{
    db.Exercises.Add(exercise);
    await db.SaveChangesAsync();
    return Results.Created($"/exercises/{exercise.Id}", exercise);
});

// Получение статистики за последнюю неделю
app.MapGet("/activities/stats/{userId}", async (int userId, AppDbContext db) =>
{
    var today = DateTime.UtcNow.Date;
    var weekAgo = today.AddDays(-7);

    var stats = await db.Activities
        .Where(a => a.UserId == userId && a.Date >= weekAgo)
        .GroupBy(a => a.Date.Date)
        .Select(g => new
        {
            Date = g.Key,
            Steps = g.Sum(a => a.Steps),
            Distance = g.Sum(a => a.Distance),
            Calories = g.Sum(a => a.Calories)
        })
        .OrderBy(s => s.Date)
        .ToListAsync();
    return Results.Ok(stats);
});

// Авторизация пользователя
app.MapPost("/login", async (LoginRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest("Имя пользователя и пароль обязательны для заполнения");

    var exists = await db.Users.AnyAsync(user => user.Username == request.Username && user.Password == request.Password);
    if (!exists)
        return Results.BadRequest("Неверный логин или пароль");

    return Results.Ok(new { Success = true, Message = "Пользователь успешно вошел в систему" });
});

// Регистрация нового пользователя
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

// Получение статистики пользователя
app.MapGet("/user/stats", async (string username, AppDbContext db) =>
{
    // Сначала находим пользователя
    var user = await db.Users
        .Include(u => u.Activities)  // Подгружаем активности
        .FirstOrDefaultAsync(u => u.Username == username);

    if (user == null)
        return Results.NotFound("Пользователь не найден");

    // Если у пользователя нет активностей
    if (!user.Activities.Any())
        return Results.Ok(new { AvgSteps = 0, AvgDistance = 0.0, AvgCalories = 0 });

    // Рассчитываем средние значения
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

//Получение списка упражнений пользователя
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
    // Находим пользователя по Id
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
    if (user == null)
        return Results.NotFound("Пользователь не найден");

    // Обновляем данные
    //user.Age = request.Age;
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

    // Гарантируем, что Goal не будет null
    user.Goal = string.IsNullOrEmpty(request.Goal) ? "Похудение" : request.Goal;
    user.BirthDate = request.BirthDate;
    user.Weight = request.Weight;
    user.Height = request.Height;
    user.TargetWeight = request.TargetWeight;
    user.TargetPeriod = request.TargetPeriod;

    await db.SaveChangesAsync();
    return Results.Ok(new { Success = true, Message = "Профиль успешно обновлен" });
});


// Удаление упражнения
app.MapDelete("/exercises/{id}", async (int id, AppDbContext db) =>
{
    var exercise = await db.Exercises.FindAsync(id);
    if (exercise == null) return Results.NotFound();

    db.Exercises.Remove(exercise);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
// Перед app.Run() добавьте:
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
    Console.WriteLine($"Запрос истории для пользователя: {userId}"); // Логирование
    var history = await db.WorkoutHistory
        .Where(h => h.UserId == userId)
        .OrderByDescending(h => h.Date)
        .ToListAsync();
    Console.WriteLine($"Найдено записей: {history.Count}"); // Логирование
    return Results.Ok(history);
});

app.MapGet("/workouthistory/details/{id}", async (int id, AppDbContext db) =>
{
    Console.WriteLine($"Запрос деталей для ID: {id}"); // Логирование

    var workout = await db.WorkoutHistory.FirstOrDefaultAsync(w => w.Id == id);
    if (workout == null)
    {
        Console.WriteLine($"Тренировка с ID {id} не найдена");
        return Results.NotFound();
    }

    Console.WriteLine($"Найдена тренировка: {workout.WorkoutName}");
    return Results.Ok(workout);
});
// Уведомления
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

// Настройки уведомлений
app.MapGet("/notifications/settings/{userId}", async (int userId, AppDbContext db) =>
{
    var settings = await db.NotificationSettings.FirstOrDefaultAsync(n => n.UserId == userId);
    if (settings == null)
    {
        // Создаем настройки по умолчанию
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
// Запуск приложения
app.Run();

// Модели данных

// Пользователь
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public double Weight { get; set; }
    public double Height { get; set; }
    public string Goal { get; set; } = "Похудение"; // Устанавливаем значение по умолчанию
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
// Запрос на авторизацию
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// Запрос на регистрацию
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
    public string Description { get; set; } = string.Empty; // Добавлено
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int Duration { get; set; } // в минутах
    public int CaloriesBurned { get; set; }
    public string Notes { get; set; } = string.Empty; // Добавлено
}

// Активность пользователя
public class Activity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = new User(); // Инициализировано
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
    public int Duration { get; set; } // Продолжительность в секундах
    public int CaloriesBurned { get; set; } // Сожженные калории
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
    public TimeSpan NotificationTime { get; set; } = new TimeSpan(9, 0, 0); // Добавлено новое свойство
}
// Контекст базы данных
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutHistory> WorkoutHistory => Set<WorkoutHistory>(); // Добавьте эту строку
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationSettings> NotificationSettings => Set<NotificationSettings>();
} 
