
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы
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

// Активность пользователя
public class Activity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }  // Навигационное свойство
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

// Контекст базы данных
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
}