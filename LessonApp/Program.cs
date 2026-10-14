using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace dot.net.Lesson13
{
    public class Result
    {
        const string ConnString = "Host=wotacoma.ru;Port=5090;Username=j4nqq;Password=bananas228;Database=j4nqq";

        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            DbProviderFactory factory = NpgsqlFactory.Instance;

            DataTable table = LoadTable(factory);
            List<string> original = Snapshot(table);

            Console.WriteLine($"Загружено строк: {table.Rows.Count}. Соединение с БД закрыто.");

            while (true)
            {
                Console.WriteLine("""

                    ========== МЕНЮ ==========
                    1. Показать все строки и их состояния
                    2. Изменить имя студента
                    3. Добавить нового студента
                    4. Удалить студента
                    5. Вывести журнал изменений
                    6. RejectChanges() и проверка, что таблица вернулась к исходной
                    7. Перезагрузить данные из БД
                    8. Сохранить изменения в БД (Update в транзакции)
                    9. Проверить содержимое БД отдельным запросом
                    0. Выход
                    """);
                Console.Write("Ваш выбор: ");
                string? choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        ShowAll(table);
                        break;

                    case "2":
                        {
                            DataRow? row = PickRow(table, "Кого переименовать");
                            if (row == null) break;
                            row["first_name"] = Ask("Новое имя");
                            Console.WriteLine($"Состояние строки: {row.RowState}");
                        }
                        break;

                    case "3":
                        {
                            DataRow newRow = table.NewRow();
                            newRow["nickname"] = Ask("nickname");
                            newRow["first_name"] = Ask("Имя (first_name)");
                            newRow["last_name"] = Ask("Фамилия (last_name)");
                            table.Rows.Add(newRow);
                            Console.WriteLine($"Студент добавлен. Состояние строки: {newRow.RowState}");
                        }
                        break;

                    case "4":
                        {
                            DataRow? row = PickRow(table, "Кого удалить");
                            if (row == null) break;
                            row.Delete();
                            Console.WriteLine($"Состояние строки: {row.RowState}");
                        }
                        break;

                    case "5":
                        Console.WriteLine("\n===== Журнал изменений =====");
                        PrintJournal(table);
                        break;

                    case "6":
                        table.RejectChanges();
                        Console.WriteLine("\n===== RejectChanges() =====");

                        List<string> after = Snapshot(table);
                        bool allUnchanged = table.Rows.Cast<DataRow>()
                            .All(r => r.RowState == DataRowState.Unchanged);
                        bool same = original.SequenceEqual(after);

                        Console.WriteLine($"Строк в исходной таблице: {original.Count}, сейчас: {after.Count}");
                        Console.WriteLine($"Все строки Unchanged: {(allUnchanged ? "да" : "нет")}");
                        Console.WriteLine($"Содержимое совпадает с исходным: {(same ? "да" : "нет")}");
                        Console.WriteLine(same && allUnchanged
                            ? "Таблица вернулась к состоянию до эксперимента."
                            : "Таблица отличается от исходной!");

                        Console.WriteLine("\nЖурнал после RejectChanges (должен быть пустым):");
                        PrintJournal(table);
                        break;

                    case "7":
                        table = LoadTable(factory);
                        original = Snapshot(table);
                        Console.WriteLine($"Данные перезагружены, строк: {table.Rows.Count}. Соединение закрыто.");
                        break;

                    case "8":
                        if (SaveChanges(table, factory))
                            original = Snapshot(table);
                        break;

                    case "9":
                        VerifyInDatabase(factory);
                        break;

                    case "0":
                        return;

                    default:
                        Console.WriteLine("Неизвестный пункт меню.");
                        break;
                }
            }
        }

        // ---------------------------------------------------------------
        //  СОХРАНЕНИЕ: параметризованные команды + транзакция
        // ---------------------------------------------------------------

        /// <summary>
        /// Сохраняет все изменения одной транзакцией.
        /// Возвращает true, если всё записано; false — если была ошибка и произошёл откат.
        /// </summary>
        private static bool SaveChanges(DataTable table, DbProviderFactory factory)
        {
            // Копия только изменённых строк (Added / Modified / Deleted).
            // Update() выполняем над копией: если что-то упадёт, состояния
            // строк в основной таблице останутся нетронутыми.
            DataTable? changes = table.GetChanges();
            if (changes == null)
            {
                Console.WriteLine("Нет изменений для сохранения.");
                return true;
            }

            Console.WriteLine("\n===== RowState перед сохранением =====");
            ShowAll(table);

            using DbConnection connection = factory.CreateConnection()
                ?? throw new InvalidOperationException();
            connection.ConnectionString = ConnString;
            connection.Open();

            using DbTransaction transaction = connection.BeginTransaction();

            try
            {
                using DbDataAdapter adapter = factory.CreateDataAdapter()
                    ?? throw new InvalidOperationException();

                ConfigureCommands(adapter, connection, transaction);

                int affected = adapter.Update(changes);
                transaction.Commit();

                // Только после успешного COMMIT фиксируем изменения в памяти:
                // Added/Modified -> Unchanged, Deleted-строки физически исчезают.
                table.AcceptChanges();

                Console.WriteLine($"\nСохранено строк: {affected}. Транзакция зафиксирована (COMMIT).");
                Console.WriteLine("\n===== RowState после сохранения =====");
                ShowAll(table);
                return true;
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); }
                catch (Exception rbEx) { Console.WriteLine($"Ошибка при откате: {rbEx.Message}"); }

                Console.WriteLine("\n!!! ОШИБКА СОХРАНЕНИЯ !!!");
                Console.WriteLine(ex is PostgresException pg
                    ? $"PostgreSQL [{pg.SqlState}]: {pg.MessageText}"
                    : ex.Message);

                Console.WriteLine("""

                    Транзакция откатена (ROLLBACK): в БД не попало НИ ОДНО из изменений этой группы.
                    В памяти ничего не потеряно: AcceptChanges() не вызывался, поэтому все строки
                    сохранили свои состояния (Added / Modified / Deleted) и ждут повторного сохранения.
                    Исправьте проблему (например, уникальный nickname) или откатите правки пунктом 6.
                    """);

                Console.WriteLine("Состояния строк в памяти после ошибки:");
                ShowAll(table);
                return false;
            }
        }

        private static void ConfigureCommands(DbDataAdapter adapter, DbConnection connection, DbTransaction transaction)
        {
            // Предполагается, что nickname — первичный ключ таблицы.
            // Для UPDATE/DELETE ключ берём из версии Original (значение, которое было в БД).

            adapter.InsertCommand = MakeCommand(connection, transaction,
                "INSERT INTO students_lesson13 (nickname, first_name, last_name) " +
                "VALUES (@nickname, @first_name, @last_name)",
                ("nickname", "nickname", DataRowVersion.Current),
                ("first_name", "first_name", DataRowVersion.Current),
                ("last_name", "last_name", DataRowVersion.Current));

            adapter.UpdateCommand = MakeCommand(connection, transaction,
                "UPDATE students_lesson13 " +
                "SET nickname = @nickname, first_name = @first_name, last_name = @last_name " +
                "WHERE nickname = @old_nickname",
                ("nickname", "nickname", DataRowVersion.Current),
                ("first_name", "first_name", DataRowVersion.Current),
                ("last_name", "last_name", DataRowVersion.Current),
                ("old_nickname", "nickname", DataRowVersion.Original));

            adapter.DeleteCommand = MakeCommand(connection, transaction,
                "DELETE FROM students_lesson13 WHERE nickname = @old_nickname",
                ("old_nickname", "nickname", DataRowVersion.Original));
        }

        private static DbCommand MakeCommand(
            DbConnection connection,
            DbTransaction transaction,
            string sql,
            params (string Name, string Column, DataRowVersion Version)[] parameters)
        {
            DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = transaction;
            cmd.UpdatedRowSource = UpdateRowSource.None;

            foreach (var (name, column, version) in parameters)
            {
                DbParameter p = cmd.CreateParameter();
                p.ParameterName = name;
                p.DbType = DbType.String;
                p.SourceColumn = column;
                p.SourceVersion = version;
                cmd.Parameters.Add(p);
            }
            return cmd;
        }

        /// <summary>Отдельное соединение и отдельный SELECT — смотрим, что реально лежит в БД.</summary>
        private static void VerifyInDatabase(DbProviderFactory factory)
        {
            Console.WriteLine("\n===== Содержимое БД (отдельный запрос) =====");

            using DbConnection connection = factory.CreateConnection()
                ?? throw new InvalidOperationException();
            connection.ConnectionString = ConnString;
            connection.Open();

            using DbCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT nickname, first_name, last_name FROM students_lesson13 ORDER BY nickname";

            using DbDataReader reader = command.ExecuteReader();
            int count = 0;
            while (reader.Read())
            {
                Console.WriteLine($"{reader.GetString(0),-15} | {reader.GetString(1),-18} | {reader.GetString(2),-15}");
                count++;
            }
            Console.WriteLine($"Всего строк в БД: {count}");
        }

        // ---------------------------------------------------------------
        //  Остальной код без изменений
        // ---------------------------------------------------------------

        private static void PrintJournal(DataTable table)
        {
            int count = 0;

            foreach (DataRow row in table.Rows)
            {
                switch (row.RowState)
                {
                    case DataRowState.Modified:
                        Console.WriteLine(
                            $"[MODIFIED] {FullName(row, DataRowVersion.Original)} → {FullName(row, DataRowVersion.Current)}");
                        count++;
                        break;

                    case DataRowState.Added:
                        Console.WriteLine($"[ADDED]    {FullName(row, DataRowVersion.Current)}");
                        count++;
                        break;

                    case DataRowState.Deleted:
                        Console.WriteLine($"[DELETED]  {FullName(row, DataRowVersion.Original)}");
                        count++;
                        break;
                }
            }

            if (count == 0)
                Console.WriteLine("(изменений нет)");
        }

        private static string FullName(DataRow row, DataRowVersion version)
            => $"{row["first_name", version]} {row["last_name", version]}";

        private static List<string> Snapshot(DataTable table)
        {
            return table.Rows.Cast<DataRow>()
                .Select(r => $"{r["nickname"]}|{r["first_name"]}|{r["last_name"]}")
                .ToList();
        }

        private static DataTable LoadTable(DbProviderFactory factory)
        {
            var table = new DataTable("Students");

            using (DbConnection connection = factory.CreateConnection()
                   ?? throw new InvalidOperationException())
            {
                connection.ConnectionString = ConnString;
                connection.Open();

                using DbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT nickname, first_name, last_name FROM students_lesson13";

                using DbDataAdapter adapter = factory.CreateDataAdapter()
                    ?? throw new InvalidOperationException();
                adapter.SelectCommand = command;
                adapter.Fill(table);
            }
            return table;
        }

        private static string Ask(string prompt)
        {
            Console.Write($"{prompt}: ");
            return Console.ReadLine()?.Trim() ?? "";
        }

        private static DataRow? PickRow(DataTable table, string prompt)
        {
            if (table.Rows.Count == 0)
            {
                Console.WriteLine("В таблице нет строк.");
                return null;
            }

            ShowAll(table);
            Console.Write($"{prompt} (номер строки): ");

            if (!int.TryParse(Console.ReadLine(), out int index)
                || index < 0 || index >= table.Rows.Count)
            {
                Console.WriteLine("Неверный номер.");
                return null;
            }

            DataRow row = table.Rows[index];
            if (row.RowState == DataRowState.Deleted)
            {
                Console.WriteLine("Эта строка уже удалена.");
                return null;
            }
            return row;
        }

        private static void ShowAll(DataTable table)
        {
            Console.WriteLine($"\nСтрок в таблице: {table.Rows.Count}");
            for (int i = 0; i < table.Rows.Count; i++)
                Console.WriteLine($"{i,3} | {Format(table.Rows[i])}");
        }

        private static string Format(DataRow r)
        {
            DataRowVersion v = r.RowState == DataRowState.Deleted
                ? DataRowVersion.Original
                : DataRowVersion.Current;

            return $"{r.RowState,-10} | {r["nickname", v],-15} | {r["first_name", v],-18} | {r["last_name", v],-15}";
        }
    }
}