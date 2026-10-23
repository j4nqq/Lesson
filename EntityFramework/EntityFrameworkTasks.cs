using DatabaseCourseExam.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatabaseCourseExam.EntityFramework;

public static class EntityFrameworkTasks
{
    public static async Task ShowStudentsAsync()
    {
        await using var context = SchoolDbContextFactory.Create();

        // >>> ЗАДАНИЕ 3: Include загружает Group, чтобы student.Group.Name был доступен
        List<Student> students = await context.Students
            .AsNoTracking()
            .Include(student => student.Group)
            .OrderBy(student => student.Id)
            .ToListAsync();

        foreach (Student student in students)
        {
            Console.WriteLine(
                $"{student.Id}. {student.FirstName} {student.LastName}, " +
                $"возраст: {student.Age}, группа: {student.Group.Name}");
        }
        // <<< ЗАДАНИЕ 3

        // >>> ЗАДАНИЕ 10: группировка уже загруженных студентов (в памяти)
        if (students.Count == 0)
        {
            Console.WriteLine("Студентов нет.");
            return;
        }

        var studentsByGroup = students
            .GroupBy(student => student.GroupId)
            .Select(g => new
            {
                GroupName = g.First().Group.Name,
                Count = g.Count()
            })
            .OrderBy(item => item.GroupName);

        Console.WriteLine();
        Console.WriteLine("Количество студентов по группам:");

        foreach (var item in studentsByGroup)
        {
            Console.WriteLine($"{item.GroupName}: {item.Count}");
        }
        // <<< ЗАДАНИЕ 10
    }

    public static async Task UpdateStudentAgeAsync(
        int studentId,
        int newAge)
    {
        //ЗАДАНИЕ 7
        if (studentId <= 0)
        {
            Console.WriteLine("ID студента должен быть положительным числом.");
            return;
        }

        if (newAge is < 14 or > 100)
        {
            Console.WriteLine("Возраст должен быть от 14 до 100.");
            return;
        }
        //ЗАДАНИЕ 7

        await using var context = SchoolDbContextFactory.Create();

        Student? student = await context.Students
            .FirstOrDefaultAsync(x => x.Id == studentId);

        if (student is null)
        {
            Console.WriteLine($"Студент с ID {studentId} не найден. Ничего не изменено.");
            return;
        }

        //ЗАДАНИЕ 7
        int oldAge = student.Age;
        student.Age = newAge;

        int affectedRows = await context.SaveChangesAsync();

        Console.WriteLine(
            affectedRows == 0
                ? "Возраст не изменился, сохранять нечего."
                : $"{student.FirstName} {student.LastName} (ID {studentId}): " +
                  $"возраст {oldAge} → {newAge}. Сохранено изменений: {affectedRows}");
        //ЗАДАНИЕ 7
    }

    public static async Task ShowCoursesAsync()
    {
        await using var context = SchoolDbContextFactory.Create();

        var courses = await context.Courses
            .OrderBy(course => course.Name)
            .Select(course => new
            {
                course.Name,
                course.Price,
                course.AvailablePlaces
            })
            .ToListAsync();

        foreach (var course in courses)
        {
            Console.WriteLine(
                $"{course.Name} | Цена: {course.Price:N2} | " +
                $"Свободных мест: {course.AvailablePlaces}");
        }
    }
}