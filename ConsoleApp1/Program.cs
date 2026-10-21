using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using lesson19.Models;

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

while (true)
{
    Console.WriteLine("\n=== Меню ===");
    Console.WriteLine("1. Показать студентов");
    Console.WriteLine("2. Добавить студента");
    Console.WriteLine("3. Изменить возраст");
    Console.WriteLine("4. Удалить студента");
    Console.WriteLine("5. Перевести студента в другую группу");
    Console.WriteLine("0. Выход");
    Console.Write("Выбор: ");

    string? choice = Console.ReadLine();
    if (choice == "0") break;

    try
    {
        switch (choice)
        {
            case "1": await ShowStudents(); break;
            case "2": await AddStudent(); break;
            case "3": await ChangeAge(); break;
            case "4": await DeleteStudent(); break;
            case "5": await TransferStudent(); break;
            default: Console.WriteLine("Неизвестный пункт меню."); break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

async Task ShowStudents()
{
    var students = await db.Students
        .Include(s => s.Group)
        .OrderBy(s => s.LastName)
        .ToListAsync();

    if (students.Count == 0)
    {
        Console.WriteLine("Студентов нет.");
        return;
    }

    foreach (var s in students)
        Console.WriteLine($"{s.Id}: {s.LastName} {s.FirstName}, {s.Age}, группа {s.Group.Name}");
}

async Task AddStudent()
{
    Console.Write("Имя: ");
    string firstName = (Console.ReadLine() ?? "").Trim();
    Console.Write("Фамилия: ");
    string lastName = (Console.ReadLine() ?? "").Trim();

    if (firstName == "" || lastName == "")
    {
        Console.WriteLine("Имя и фамилия не могут быть пустыми.");
        return;
    }

    int? age = ReadInt("Возраст: ");
    if (age is null || age <= 0)
    {
        Console.WriteLine("Возраст должен быть числом больше нуля.");
        return;
    }

    int? groupId = ReadInt("Id группы: ");
    if (groupId is null || !await db.Groups.AnyAsync(g => g.Id == groupId))
    {
        Console.WriteLine("Группа не найдена.");
        return;
    }

    db.Students.Add(new Student
    {
        FirstName = firstName,
        LastName = lastName,
        Age = age.Value,
        GroupId = groupId.Value
    });

    // SaveChangesAsync отправляет в базу все накопленные изменения (INSERT/UPDATE/DELETE);
    // до его вызова Add, Remove и смена свойств меняют только состояние в памяти.
    await db.SaveChangesAsync();
    Console.WriteLine("Студент добавлен.");
}

async Task ChangeAge()
{
    int? id = ReadInt("Id студента: ");
    var student = id is null ? null : await db.Students.FindAsync(id.Value);
    if (student is null)
    {
        Console.WriteLine("Студент не найден.");
        return;
    }

    int? age = ReadInt("Новый возраст: ");
    if (age is null || age <= 0)
    {
        Console.WriteLine("Возраст должен быть числом больше нуля.");
        return;
    }

    student.Age = age.Value;
    await db.SaveChangesAsync();
    Console.WriteLine("Возраст изменён.");
}

async Task DeleteStudent()
{
    int? id = ReadInt("Id студента: ");
    var student = id is null ? null : await db.Students.FindAsync(id.Value);
    if (student is null)
    {
        Console.WriteLine("Студент не найден.");
        return;
    }

    db.Students.Remove(student);
    await db.SaveChangesAsync();
    Console.WriteLine("Студент удалён.");
}

async Task TransferStudent()
{
    int? id = ReadInt("Id студента: ");
    var student = id is null
        ? null
        : await db.Students.Include(s => s.Group).FirstOrDefaultAsync(s => s.Id == id);
    if (student is null)
    {
        Console.WriteLine("Студент не найден.");
        return;
    }

    Console.WriteLine($"Текущая группа: {student.Group.Name}");

    Console.WriteLine("Доступные группы:");
    var groups = await db.Groups.OrderBy(g => g.Name).ToListAsync();
    foreach (var g in groups)
        Console.WriteLine($"{g.Id}: {g.Name}");

    int? newGroupId = ReadInt("Id новой группы: ");
    var newGroup = groups.FirstOrDefault(g => g.Id == newGroupId);
    if (newGroup is null)
    {
        Console.WriteLine("Группа не найдена.");
        return;
    }

    if (newGroup.Id == student.GroupId)
    {
        Console.WriteLine("Студент уже состоит в этой группе.");
        return;
    }

    student.GroupId = newGroup.Id;
    await db.SaveChangesAsync();
    Console.WriteLine($"Студент {student.LastName} {student.FirstName} переведён в группу {newGroup.Name}.");
}

int? ReadInt(string prompt)
{
    Console.Write(prompt);
    return int.TryParse(Console.ReadLine(), out int value) ? value : null;
}
