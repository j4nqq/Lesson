using System.Globalization;
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

    Console.WriteLine("\nРезультат поиска");
    foreach (var s in students)
        Console.WriteLine($"{s.FirstName} {s.LastName}, {s.Age}");

    bool hasMinors = await db.Students.AnyAsync(s => s.Age < 18);
    Console.WriteLine($"\nЕсть несовершеннолетние студенты: {hasMinors}");

    Console.WriteLine("\nАналитика учебного центра\n");

    int count = await db.Students.CountAsync();
    Console.WriteLine($"Всего студентов: {count}");

    if (count > 0)
    {
        double avg = await db.Students.AverageAsync(s => s.Age);
        int youngest = await db.Students.MinAsync(s => s.Age);
        int oldest = await db.Students.MaxAsync(s => s.Age);

        Console.WriteLine($"Средний возраст: {avg.ToString("F1", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"Минимальный возраст: {youngest}");
        Console.WriteLine($"Максимальный возраст: {oldest}");

        var statistics = await db.Students
            .GroupBy(s => s.Age)
            .Select(g => new { Age = g.Key, Count = g.Count() })
            .OrderBy(x => x.Age)
            .ToListAsync();

        Console.WriteLine("\nРаспределение по возрасту:");
        foreach (var item in statistics)
            Console.WriteLine($"{item.Age} {YearsWord(item.Age)} — {item.Count}");

        bool allAdults = await db.Students.AllAsync(s => s.Age >= 18);
        Console.WriteLine($"\nВсе студенты совершеннолетние: {allAdults}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}

static string YearsWord(int n) =>
    n % 100 is >= 11 and <= 14 ? "лет" :
    n % 10 == 1 ? "год" :
    n % 10 is >= 2 and <= 4 ? "года" : "лет";
