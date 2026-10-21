using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.OutputEncoding = Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

string connectionString =
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Не найдена строка подключения Default.");

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

try
{
    var groups = await db.Groups
        .Include(g => g.Students)
        .OrderBy(g => g.Name)
        .ToListAsync();

    foreach (var group in groups)
    {
        Console.WriteLine($"\nГруппа {group.Name}, студентов: {group.Students.Count}");

        if (group.Students.Count == 0)
        {
            Console.WriteLine("В группе пока нет студентов");
            continue;
        }

        foreach (var s in group.Students.OrderBy(s => s.LastName))
            Console.WriteLine($"  {s.LastName} {s.FirstName}, {s.Age}");
    }

    Console.WriteLine("\nСтатистика по группам\n");

    var statistics = await db.Groups
        .OrderBy(g => g.Name)
        .Select(g => new
        {
            GroupName = g.Name,
            StudentCount = g.Students.Count(),
            MinorCount = g.Students.Count(s => s.Age < 18),
            AverageAge = g.Students.Select(s => (double?)s.Age).Average(),
            HasMinors = g.Students.Any(s => s.Age < 18)
        })
        .ToListAsync();

    foreach (var item in statistics)
    {
        Console.WriteLine($"Группа: {item.GroupName}");
        Console.WriteLine($"Студентов: {item.StudentCount}");
        Console.WriteLine($"Несовершеннолетних: {item.MinorCount}");
        Console.WriteLine(item.AverageAge.HasValue
            ? $"Средний возраст: {item.AverageAge.Value:F1}"
            : "Средний возраст: нет данных");
        Console.WriteLine(item.HasMinors
            ? "В группе есть несовершеннолетние"
            : "Несовершеннолетних нет");
        Console.WriteLine();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}
