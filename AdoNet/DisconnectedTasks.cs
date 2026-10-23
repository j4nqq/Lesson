
using System.Data;
using DatabaseCourseExam.Data;
using Npgsql;

namespace DatabaseCourseExam.AdoNet;

public static class DisconnectedTasks
{
    // TODO (задание 6): Реализовать добавление новой строки в DataTable с проверкой возраста и правильным RowState Added.
    public static async Task<DataTable> LoadStudentsAsync()
    {
        var table = new DataTable("Students");

        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        const string sql = """
            SELECT id, first_name, last_name, age, group_id
            FROM students
            ORDER BY id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        using var adapter = new NpgsqlDataAdapter(command);

        adapter.Fill(table);
        table.PrimaryKey = [table.Columns["id"]!];

        return table;
    }

    public static void PrintTable(DataTable table)
    {
        foreach (DataRow row in table.Rows)
        {
            Console.WriteLine(
                $"{row["id"]}: {row["first_name"]} " +
                $"{row["last_name"]}, возраст {row["age"]}");
        }
    }

    // TODO (задание 1): Найти строку по первичному ключу, обработать отсутствие результата и не менять строку, если значение не изменилось.
    public static void ChangeAgeInMemory(
        DataTable table,
        int studentId,
        int newAge)
    {
        DataRow? row = table.Rows.Find(studentId);

        if (row is null)
        {
            Console.WriteLine("Студент не найден в DataTable.");
            return;
        }

        // Удалённый фрагмент: восстановите присваивание нового возраста DataRow.

        Console.WriteLine(
            $"Возраст изменён в памяти. Состояние строки: {row.RowState}");
    }

    // TODO (задание 5): После Update вывести количество сохранённых строк и проверить, остались ли строки в состоянии Modified.
    public static int SaveChanges(DataTable table)
    {
        using var connection = DatabaseConnection.Create();
        connection.Open();

        const string sql = """
        UPDATE students
        SET age = @age
        WHERE id = @id;
        """;

        using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add("@age", NpgsqlTypes.NpgsqlDbType.Integer,
            4, "age");

        command.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer,
            4, "id");

        using var adapter = new NpgsqlDataAdapter();
        adapter.UpdateCommand = command;

        int affectedRows = adapter.Update(table);
        Console.WriteLine($"Сохранено изменений: {affectedRows}");

        return affectedRows;
    }
}