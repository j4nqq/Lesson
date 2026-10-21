using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

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
    if (await db.Students.AnyAsync())
    {
        int dbMin = await db.Students.MinAsync(s => s.Age);
        int dbMax = await db.Students.MaxAsync(s => s.Age);
        Console.WriteLine($"В базе возраст студентов: от {dbMin} до {dbMax}");
    }

    Console.Write("Минимальный возраст: ");
    if (!int.TryParse(Console.ReadLine(), out int minAge)) return;

    Console.Write("Максимальный возраст: ");
    if (!int.TryParse(Console.ReadLine(), out int maxAge)) return;

    Console.Write("Номер страницы: ");
    if (!int.TryParse(Console.ReadLine(), out int page) || page < 1) page = 1;

    const int pageSize = 3;

    // Пагинация требует сортировки: без ORDER BY база не гарантирует порядок строк,
    // поэтому OFFSET/LIMIT могут дать повторы и пропуски записей между страницами.
    // Id в конце делает порядок однозначным.
    var students = await db.Students
        .Where(s => s.Age >= minAge && s.Age <= maxAge)
        .OrderBy(s => s.LastName)
        .ThenBy(s => s.FirstName)
        .ThenBy(s => s.Id)
        .Select(s => new { s.FirstName, s.LastName, s.Age })
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    Console.WriteLine("\n=== Результат поиска ===");
    foreach (var s in students)
        Console.WriteLine($"{s.FirstName} {s.LastName}, {s.Age}");

    bool hasMinors = await db.Students.AnyAsync(s => s.Age < 18);
    Console.WriteLine($"\nЕсть несовершеннолетние студенты: {hasMinors}");
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}
