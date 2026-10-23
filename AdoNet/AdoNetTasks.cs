
using DatabaseCourseExam.Data;
using Npgsql;
using NpgsqlTypes;

namespace DatabaseCourseExam.AdoNet;

public static class AdoNetTasks
{
    public static async Task ShowStudentsAsync()
    {
        //ЗАДАНИЕ 12
        Console.Write("ID группы: ");

        if (!int.TryParse(Console.ReadLine(), out int groupId))
        {
            Console.WriteLine("Некорректный ID группы.");
            return;
        }
        //ЗАДАНИЕ 12

        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        //ЗАДАНИЕ 12
        const string sql = """
            SELECT s.id, s.first_name, s.last_name, s.age, g.name
            FROM students AS s
            JOIN student_groups AS g ON g.id = s.group_id
            WHERE s.group_id = @GroupId
            ORDER BY s.id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        var groupIdParameter = new NpgsqlParameter("@GroupId", NpgsqlDbType.Integer)
        {
            Value = groupId
        };
        command.Parameters.Add(groupIdParameter);
        //ЗАДАНИЕ 12

        await using var reader = await command.ExecuteReaderAsync();

        bool anyRows = false;

        while (await reader.ReadAsync())
        {
            anyRows = true;

            //ЗАДАНИЕ 8
            string firstName = reader.IsDBNull(1) ? "—" : reader.GetString(1);
            string lastName = reader.IsDBNull(2) ? "—" : reader.GetString(2);
            string age = reader.IsDBNull(3) ? "не указан" : reader.GetInt32(3).ToString();
            string groupName = reader.IsDBNull(4) ? "—" : reader.GetString(4);
            //ЗАДАНИЕ 8

            Console.WriteLine(
                $"{reader.GetInt32(0)}. " +
                $"{firstName} {lastName}, " +
                $"возраст: {age}, " +
                $"группа: {groupName}");
        }

        if (!anyRows)
        {
            Console.WriteLine($"В группе с ID {groupId} студентов не найдено.");
        }
    }

    public static async Task UpdateStudentAgeAsync(
        int studentId,
        int newAge)
    {
        //ЗАДАНИЕ 11
        if (newAge is < 14 or > 100)
        {
            Console.WriteLine("Возраст должен быть от 14 до 100.");
            return;
        }
        //ЗАДАНИЕ 11

        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        await using var selectCommand = new NpgsqlCommand(
            "SELECT age FROM students WHERE id = @StudentId;", connection);
        selectCommand.Parameters.Add("@StudentId", NpgsqlDbType.Integer).Value =
            studentId;

        object? oldAgeValue = await selectCommand.ExecuteScalarAsync();

        if (oldAgeValue is null)
        {
            Console.WriteLine($"Студент с ID {studentId} не найден. Ничего не изменено.");
            return;
        }

        string oldAge = oldAgeValue is int oldAgeNumber
            ? oldAgeNumber.ToString()
            : "не указан";

        const string sql = """
            UPDATE students
            SET age = @Age
            WHERE id = @StudentId;
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add("@Age", NpgsqlDbType.Integer).Value = newAge;
        command.Parameters.Add("@StudentId", NpgsqlDbType.Integer).Value =
            studentId;

        int affectedRows = await command.ExecuteNonQueryAsync();

        //ЗАДАНИЕ 11
        Console.WriteLine(
            affectedRows == 0
                ? "Студент не найден."
                : $"Студент ID {studentId}: возраст {oldAge} → {newAge}. " +
                  $"Изменено записей: {affectedRows}");
        //ЗАДАНИЕ 11
    }

    public static async Task EnrollStudentAsync(
        int studentId,
        int courseId)
    {
        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            //ЗАДАНИЕ 9
            const string studentExistsSql = """
                SELECT EXISTS (SELECT 1 FROM students WHERE id = @StudentId);
                """;

            await using var studentExistsCommand =
                new NpgsqlCommand(studentExistsSql, connection, transaction);

            studentExistsCommand.Parameters.Add(
                "@StudentId", NpgsqlDbType.Integer).Value = studentId;

            bool studentExists =
                (bool)(await studentExistsCommand.ExecuteScalarAsync() ?? false);

            if (!studentExists)
            {
                throw new InvalidOperationException(
                    $"Студент с ID {studentId} не найден.");
            }

            const string alreadyEnrolledSql = """
                SELECT EXISTS (
                    SELECT 1
                    FROM student_courses
                    WHERE student_id = @StudentId
                      AND course_id = @CourseId);
                """;

            await using var alreadyEnrolledCommand =
                new NpgsqlCommand(alreadyEnrolledSql, connection, transaction);

            alreadyEnrolledCommand.Parameters.Add(
                "@StudentId", NpgsqlDbType.Integer).Value = studentId;
            alreadyEnrolledCommand.Parameters.Add(
                "@CourseId", NpgsqlDbType.Integer).Value = courseId;

            bool alreadyEnrolled =
                (bool)(await alreadyEnrolledCommand.ExecuteScalarAsync() ?? false);

            if (alreadyEnrolled)
            {
                throw new InvalidOperationException(
                    $"Студент ID {studentId} уже записан на курс ID {courseId}.");
            }
            //ЗАДАНИЕ 9

            const string updateSql = """
                UPDATE courses
                SET available_places = available_places - 1
                WHERE id = @CourseId
                  AND available_places > 0
                RETURNING available_places;
                """;

            await using var updateCommand =
                new NpgsqlCommand(updateSql, connection, transaction);

            updateCommand.Parameters.Add(
                "@CourseId", NpgsqlDbType.Integer).Value = courseId;

            object? result = await updateCommand.ExecuteScalarAsync();

            if (result is null)
            {
                throw new InvalidOperationException(
                    $"Курс ID {courseId} не найден или на нём нет свободных мест.");
            }

            const string insertSql = """
                INSERT INTO student_courses (student_id, course_id)
                VALUES (@StudentId, @CourseId);
                """;

            await using var insertCommand =
                new NpgsqlCommand(insertSql, connection, transaction);

            insertCommand.Parameters.Add(
                "@StudentId", NpgsqlDbType.Integer).Value = studentId;

            insertCommand.Parameters.Add(
                "@CourseId", NpgsqlDbType.Integer).Value = courseId;

            await insertCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            int placesLeft = Convert.ToInt32(result);

            Console.WriteLine("Запись выполнена:");
            Console.WriteLine($"  Студент ID {studentId} → курс ID {courseId}");
            Console.WriteLine($"  Свободных мест: {placesLeft + 1} → {placesLeft}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Запись не выполнена: {ex.Message}");
            Console.WriteLine("В базе ничего не изменилось.");
        }
    }
}