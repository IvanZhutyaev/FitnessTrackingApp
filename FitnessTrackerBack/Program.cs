using Microsoft.EntityFrameworkCore;

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
app.MapPost("/activities", async (Activity activity, AppDbContext db) =>
{
    db.Activities.Add(activity);
    await db.SaveChangesAsync();
    return Results.Created($"/activities/{activity.Id}", activity);
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
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest("Имя пользователя и пароль обязательны для заполнения");

    var exists = await db.Users.AnyAsync(user => user.Username == request.Username);
    if (exists)
        return Results.BadRequest("Пользователь с таким именем уже существует");

    var user = new User { Username = request.Username, Password = request.Password };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { Success = true, Message = "Пользователь успешно зарегистрирован" });
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
}

// Активность пользователя
public class Activity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int Steps { get; set; }
    public double Distance { get; set; }
    public int Calories { get; set; }
}

// Контекст базы данных
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Activity> Activities => Set<Activity>();
}