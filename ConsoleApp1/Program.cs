using ConsoleApp1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Нет строки подключения");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

try
{
    bool canConnect = await db.Database.CanConnectAsync();

    if (!canConnect)
    {
        Console.WriteLine("Нет подключения");
        return;
    }

    Console.WriteLine("Подключено");
    Console.WriteLine($"База: {db.Database.GetDbConnection().Database}");

    var students = await db.Students.ToListAsync();

    if (students.Count == 0)
    {
        Console.WriteLine("Студентов нет");
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine("Студенты:");

        foreach (var student in students)
        {
            Console.WriteLine(
                $"{student.Id}: {student.FirstName} {student.LastName}, {student.Age}");
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Всего: {students.Count}");
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}
// Контрольные вопросы
// 1. Управлять взаимодействием приложения с EF Core и базой данных
// 2. Набор сущностей Student, доступный через контекст
// 3. Регистрирует контекст в контейнере зависимостей
// 4. При материализации запроса через ToListAsync