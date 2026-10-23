
using System.Data;
using DatabaseCourseExam.Data;
using Npgsql;

namespace DatabaseCourseExam.AdoNet;

public static class DisconnectedTasks
{
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

    //ЗАДАНИЕ 6
    public static void AddStudentInMemory(
        DataTable table,
        string firstName,
        string lastName,
        int age,
        int groupId)
    {
        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
        {
            Console.WriteLine("Имя и фамилия не должны быть пустыми.");
            return;
        }

        if (age is < 14 or > 100)
        {
            Console.WriteLine("Возраст должен быть от 14 до 100.");
            return;
        }

        if (groupId <= 0)
        {
            Console.WriteLine("ID группы должен быть положительным числом.");
            return;
        }

        int newId = 1;

        foreach (DataRow existingRow in table.Rows)
        {
            if (existingRow.RowState == DataRowState.Deleted)
            {
                continue;
            }

            newId = Math.Max(newId, Convert.ToInt32(existingRow["id"]) + 1);
        }

        DataRow row = table.NewRow();
        Console.WriteLine($"После NewRow(): {row.RowState}");

        row["id"] = newId;
        row["first_name"] = firstName.Trim();
        row["last_name"] = lastName.Trim();
        row["age"] = age;
        row["group_id"] = groupId;

        table.Rows.Add(row);
        Console.WriteLine($"После Rows.Add(): {row.RowState}");
        Console.WriteLine(
            $"Добавлена строка: ID {newId} (временный), {row["first_name"]} " +
            $"{row["last_name"]}, возраст {age}, группа {groupId}. " +
            "В БД она не сохраняется.");
    }
    //ЗАДАНИЕ 6

    public static void PrintTable(DataTable table)
    {
        foreach (DataRow row in table.Rows)
        {
            Console.WriteLine(
                $"{row["id"]}: {row["first_name"]} " +
                $"{row["last_name"]}, возраст {row["age"]}  [{row.RowState}]");
        }
    }

    public static void ChangeAgeInMemory(
        DataTable table,
        int studentId,
        int newAge)
    {
        //ЗАДАНИЕ 1
        DataRow? row = table.Rows.Find(studentId);

        if (row is null)
        {
            Console.WriteLine("Студент не найден в DataTable.");
            return;
        }

        if (!row.IsNull("age") && Convert.ToInt32(row["age"]) == newAge)
        {
            Console.WriteLine(
                $"Возраст не изменился, строка не тронута. " +
                $"Состояние строки: {row.RowState}");
            return;
        }

        int oldAge = row.IsNull("age") ? 0 : Convert.ToInt32(row["age"]);
        row["age"] = newAge;
        //ЗАДАНИЕ 1

        Console.WriteLine(
            $"ID {studentId}: возраст {oldAge} → {newAge} (только в памяти). " +
            $"Состояние строки: {row.RowState}. " +
            "В БД изменение попадёт после пункта 7.");
    }

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

        //ЗАДАНИЕ 5
        DataRow[] modifiedRows =
            table.Select(null, null, DataViewRowState.ModifiedCurrent);

        if (modifiedRows.Length == 0)
        {
            Console.WriteLine("Нет изменённых строк, сохранять нечего.");
            return 0;
        }

        Console.WriteLine("Будет сохранено в БД:");

        foreach (DataRow modifiedRow in modifiedRows)
        {
            Console.WriteLine(
                $"  ID {modifiedRow["id"]}: возраст " +
                $"{modifiedRow["age", DataRowVersion.Original]} → " +
                $"{modifiedRow["age", DataRowVersion.Current]}");
        }

        int affectedRows = adapter.Update(modifiedRows);
        Console.WriteLine($"Сохранено изменений: {affectedRows}");

        int stillModified = 0;
        int pendingAdded = 0;

        foreach (DataRow row in table.Rows)
        {
            if (row.RowState == DataRowState.Modified)
            {
                stillModified++;
            }
            else if (row.RowState == DataRowState.Added)
            {
                pendingAdded++;
            }
        }

        Console.WriteLine(
            stillModified == 0
                ? "Строк в состоянии Modified не осталось."
                : $"Внимание: строк в состоянии Modified осталось: {stillModified}");

        if (pendingAdded > 0)
        {
            Console.WriteLine(
                $"Добавленные строки (Added) в БД не сохраняются: {pendingAdded}");
        }
        //ЗАДАНИЕ 5

        return affectedRows;
    }
}