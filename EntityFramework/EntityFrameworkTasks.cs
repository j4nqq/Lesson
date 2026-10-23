using DatabaseCourseExam.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatabaseCourseExam.EntityFramework;

public static class EntityFrameworkTasks
{
    // TODO (задание 3): Вывести студентов с названием группы; обеспечить загрузку связанной сущности без обращения к незагруженному свойству.
    // TODO (задание 10): Сгруппировать загруженных студентов по группе и вывести количество студентов в каждой группе.
    public static async Task ShowStudentsAsync()
    {
        await using var context = SchoolDbContextFactory.Create();

        List<Student> students = await context.Students
            .Include(student => student.Group)
            .OrderBy(student => student.Id)
            .ToListAsync();

        foreach (Student student in students)
        {
            Console.WriteLine(
                $"{student.Id}. {student.FirstName} {student.LastName}, " +
                $"возраст: {student.Age}, группа: {student.Group.Name}");
        }
    }

    // TODO (задание 7): Проверить входные данные и отсутствие студента; сохранить изменения через SaveChangesAsync.
    public static async Task UpdateStudentAgeAsync(
        int studentId,
        int newAge)
    {
        await using var context = SchoolDbContextFactory.Create();

        Student? student = await context.Students
            .FirstOrDefaultAsync(x => x.Id == studentId);

        if (student is null)
        {
            Console.WriteLine("Студент не найден.");
            return;
        }

        student.Age = newAge;

        int affectedRows = await context.SaveChangesAsync();

        Console.WriteLine(
            $"Возраст изменён. Сохранено изменений: {affectedRows}");
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