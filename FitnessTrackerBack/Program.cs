

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=property;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();  -- Я закоментил для локального тестирования :) 

app.MapGet("/activities/{userId}", async (int userId, AppDbContextdb db) =>
{
    var activities = await db.Activities
        .Where((Activity a) => a.UserId == userId)
        .OrderByDescending((Activity a) => a.Date)
        .ToListAsync();
    return Result.Ok(activities);  
});

app.MapPost("/activities", async (Activity activity, AppDbContext db) =>
{

    db.Activities.Add(activity);
    await db.SaveChangesAsync();
    return Results.Created($"/activities/{activity.Id}", activity);

});

app.MapGet("/activities/stats/{userId}", async (int userId, AppDbContext db) =>
{


    var today = DateTime.UtcNow.Date;
    var weekAgo = today.AddDays(-7);

    var stats = await db.Activities
        .Where((Activity a) => a.UserId == userId && a.Date >= weekAgo)
        .GroupBy((Activity a) => a.Date.Date)
        .Select((Activity g) => new
        {

            Date = g.Key,
            Steps = g.Sum((Activity a) => a.Distance),
            Calories = g.Sum((Activity a) => a.Calories)

        })
        .OrderBy((Activity s) => s.Date)
        .ToListAsync();
    return Results.Ok(stats);
});

app.MapPost("/login", async (LoginRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest("Поьзовательское имя и пароль обязательны для заполнения");
    
    var exists = await db.Users.AnyAsync((User user)=> user.Username == request.Username && user.Password == request.Password);
    if (!exists)
        
        return Results.BadRequest("Неверный логин или пароль");

    return Results.Ok("Пользователь успешно вошел в систему");
});

app.MapPost("/register", async (RegisterRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest("Пользовательское имя и пароль обязательны для заполнения");

    var exists = await db.Users.AnyAsync((User user)=> user.Username == request.Username && user.Password == request.Password);
    if (exists)
        return Results.BadRequest("Пользователь с таким именем уже существует");

    var user = new User { Username = request.Username, Password = request.Password };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok("Пользователь успешно зарегистрирован");
});

app.Run();

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
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
}

public class Activity
{


    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int Steps { get; set; }
    public double Distance { get; set; }
    public int Calories { get; set; }


}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Activity> Activities => Set<Activity>();
}


