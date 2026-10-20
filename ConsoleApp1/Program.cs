using ConsoleApp1.Models;

var students = new List<Student>
{
    new()
    {
        FirstName = "Иван",
        LastName = "Иванов",
        Age = 20,
        IsActive = true
    },
    new()
    {
        FirstName = "Анна",
        LastName = "Петрова",
        Age = 21,
        IsActive = true
    },
    new()
    {
        FirstName = "Сергей",
        LastName = "Сидоров",
        Age = 17,
        IsActive = false
    }
};

Console.WriteLine("Активные студенты:");
foreach (var s in students.Where(s => s.IsActive))
{
    Console.WriteLine($"{s.FirstName} {s.LastName}, {s.Age}");
}

Console.WriteLine();
Console.WriteLine($"Количество студентов: {students.Count}");
Console.WriteLine($"Средний возраст: {students.Average(s => s.Age):F1}");