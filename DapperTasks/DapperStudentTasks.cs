using DatabaseCourseExam.Data;
using Dapper;
using Npgsql;
using DatabaseCourseExam.DapperTasks.DTO;

namespace DatabaseCourseExam.DapperTasks;

public static class DapperStudentTasks
{
    public static async Task ShowStudentsAsync()
    {
        await using var connection = DatabaseConnection.Create();

        const string sql = """
            SELECT
                s.id AS Id,
                s.first_name AS FirstName,
                s.last_name AS LastName,
                s.age AS Age,
                g.name AS GroupName
            FROM students AS s
            JOIN student_groups AS g ON g.id = s.group_id
            ORDER BY s.id;
            """;

        var students = await connection.QueryAsync<StudentDto>(sql);

        foreach (StudentDto student in students)
        {
            Console.WriteLine(
                $"{student.Id}. {student.FirstName} {student.LastName}, " +
                $"возраст: {student.Age}, группа: {student.GroupName}");
        }
    }

    public static async Task UpdateStudentAgeAsync(
        int studentId,
        int newAge)
    {
        //ЗАДАНИЕ 2
        if (newAge is < 14 or > 100)
        {
            Console.WriteLine("Возраст должен быть от 14 до 100.");
            return;
        }
        //ЗАДАНИЕ 2

        await using var connection = DatabaseConnection.Create();

        StudentDto? before = await connection.QuerySingleOrDefaultAsync<StudentDto>(
            """
            SELECT first_name AS FirstName, last_name AS LastName, age AS Age
            FROM students
            WHERE id = @StudentId;
            """,
            new { StudentId = studentId });

        if (before is null)
        {
            Console.WriteLine($"Студент с ID {studentId} не найден. Ничего не изменено.");
            return;
        }

        const string sql = """
            UPDATE students
            SET age = @NewAge
            WHERE id = @StudentId;
            """;

        int affectedRows = await connection.ExecuteAsync(
            sql,
            new
            {
                NewAge = newAge,
                StudentId = studentId
            });

        //ЗАДАНИЕ 2
        Console.WriteLine(
            affectedRows == 0
                ? "Студент не найден."
                : $"{before.FirstName} {before.LastName} (ID {studentId}): " +
                  $"возраст {before.Age} → {newAge}. Изменено записей: {affectedRows}");
        //ЗАДАНИЕ 2
    }

    public static async Task ShowCoursesAsync()
    {
        await using var connection = DatabaseConnection.Create();

        const string sql = """
            SELECT
                id AS Id,
                name AS Name,
                price AS Price,
                available_places AS AvailablePlaces
            FROM courses
            ORDER BY name;
            """;

        var courses = await connection.QueryAsync<CourseDto>(sql);

        foreach (CourseDto course in courses)
        {
            Console.WriteLine(
                $"{course.Name} | Цена: {course.Price:N2} | " +
                $"Свободных мест: {course.AvailablePlaces}");
        }
    }

    public static async Task EnrollStudentAsync(
        int studentId,
        int courseId)
    {
        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            string? studentName =
                await connection.QuerySingleOrDefaultAsync<string>(
                    """
                    SELECT concat_ws(' ', first_name, last_name)
                    FROM students
                    WHERE id = @StudentId;
                    """,
                    new { StudentId = studentId },
                    transaction);

            if (studentName is null)
            {
                throw new InvalidOperationException(
                    $"Студент с ID {studentId} не найден.");
            }

            string? courseName =
                await connection.QuerySingleOrDefaultAsync<string>(
                    """
                    SELECT name
                    FROM courses
                    WHERE id = @CourseId;
                    """,
                    new { CourseId = courseId },
                    transaction);

            if (courseName is null)
            {
                throw new InvalidOperationException(
                    $"Курс с ID {courseId} не найден.");
            }

            bool alreadyEnrolled =
                await connection.QuerySingleAsync<bool>(
                    """
                    SELECT EXISTS (
                        SELECT 1
                        FROM student_courses
                        WHERE student_id = @StudentId
                          AND course_id = @CourseId);
                    """,
                    new { StudentId = studentId, CourseId = courseId },
                    transaction);

            if (alreadyEnrolled)
            {
                throw new InvalidOperationException(
                    $"{studentName} уже записан на курс «{courseName}».");
            }

            const string updateSql = """
                UPDATE courses
                SET available_places = available_places - 1
                WHERE id = @CourseId
                  AND available_places > 0
                RETURNING available_places;
                """;

            int? remainingPlaces =
                await connection.QuerySingleOrDefaultAsync<int?>(
                    updateSql,
                    new { CourseId = courseId },
                    transaction);

            if (remainingPlaces is null)
            {
                throw new InvalidOperationException(
                    $"На курсе «{courseName}» нет свободных мест.");
            }

            const string insertSql = """
                INSERT INTO student_courses (student_id, course_id)
                VALUES (@StudentId, @CourseId);
                """;

            await connection.ExecuteAsync(
                insertSql,
                new
                {
                    StudentId = studentId,
                    CourseId = courseId
                },
                transaction);

            await transaction.CommitAsync();

            Console.WriteLine("Запись выполнена:");
            Console.WriteLine($"  Студент: {studentName} (ID {studentId})");
            Console.WriteLine($"  Курс: {courseName} (ID {courseId})");
            Console.WriteLine(
                $"  Свободных мест: {remainingPlaces + 1} → {remainingPlaces}");
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Запись не выполнена: {ex.Message}");
            Console.WriteLine("В базе ничего не изменилось.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync();
            Console.WriteLine(
                "Запись не выполнена: такая запись уже есть (сработал уникальный индекс).");
            Console.WriteLine("В базе ничего не изменилось.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Операция отменена: {ex.Message}");
            Console.WriteLine("В базе ничего не изменилось.");
        }
    }
}