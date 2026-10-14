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

        private static string _filter = "age > 18";
        private static string _sort = "first_name ASC";

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
                    2. Изменить студента (имя или возраст)
                    3. Добавить нового студента
                    4. Удалить студента
                    5. Вывести журнал изменений
                    6. RejectChanges() и проверка, что таблица вернулась к исходной
                    7. Перезагрузить данные из БД
                    8. Сохранить изменения в БД (Update в транзакции)
                    9. Проверить содержимое БД отдельным запросом
                    10. Фильтрация и сортировка (DataView)
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
                            DataRow? row = PickRow(table, "Кого изменить");
                            if (row == null) break;

                            string field = Ask("Что менять: 1 - имя, 2 - возраст");
                            if (field == "2")
                                row["age"] = AskAge();
                            else
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
                            newRow["age"] = AskAge();
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

                    case "10":
                        FilterMenu(table, factory);
                        break;

                    case "0":
                        return;

                    default:
                        Console.WriteLine("Неизвестный пункт меню.");
                        break;
                }
            }
        }

        private static void FilterMenu(DataTable table, DbProviderFactory factory)
        {
            while (true)
            {
                string filterText = string.IsNullOrEmpty(_filter) ? "(нет)" : _filter;
                string sortText = string.IsNullOrEmpty(_sort) ? "(нет)" : _sort;

                Console.WriteLine($"""

                    ----- Фильтрация и сортировка -----
                    Текущий RowFilter: {filterText}
                    Текущий Sort:      {sortText}

                    1. Показать представление с текущими настройками
                    2. Фильтр: студенты старше N лет
                    3. Фильтр: имя начинается с...
                    4. Задать сортировку
                    5. Сбросить фильтр и сортировку
                    6. Доказать, что БД не меняется от RowFilter/Sort
                    0. Назад
                    """);
                Console.Write("Ваш выбор: ");
                string? choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        ShowView(table);
                        break;

                    case "2":
                        {
                            string s = Ask("N (возраст строго больше)");
                            if (!int.TryParse(s, out int n))
                            {
                                Console.WriteLine("Нужно целое число.");
                                break;
                            }
                            _filter = $"age > {n}";
                            ShowView(table);
                        }
                        break;

                    case "3":
                        {
                            string prefix = Ask("Начало имени");
                            _filter = $"first_name LIKE '{EscapeLike(prefix)}%'";
                            ShowView(table);
                        }
                        break;

                    case "4":
                        {
                            string col = Ask("Столбец (nickname / first_name / last_name / age)");
                            string[] allowed = { "nickname", "first_name", "last_name", "age" };
                            if (!allowed.Contains(col))
                            {
                                Console.WriteLine("Неизвестный столбец.");
                                break;
                            }
                            string dir = Ask("Направление: 1 - по возрастанию, 2 - по убыванию");
                            _sort = $"{col} {(dir == "2" ? "DESC" : "ASC")}";
                            ShowView(table);
                        }
                        break;

                    case "5":
                        _filter = "";
                        _sort = "";
                        Console.WriteLine("Фильтр и сортировка сброшены.");
                        ShowView(table);
                        break;

                    case "6":
                        ProveDatabaseUntouched(table, factory);
                        break;

                    case "0":
                        return;

                    default:
                        Console.WriteLine("Неизвестный пункт меню.");
                        break;
                }
            }
        }

        private static void ShowView(DataTable table)
        {
            var view = new DataView(table);

            try
            {
                view.RowFilter = _filter;
                view.Sort = _sort;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Некорректный фильтр или сортировка: {ex.Message}");
                return;
            }

            Console.WriteLine($"\nRowFilter: {(string.IsNullOrEmpty(_filter) ? "(нет)" : _filter)}");
            Console.WriteLine($"Sort:      {(string.IsNullOrEmpty(_sort) ? "(нет)" : _sort)}");
            Console.WriteLine($"Строк в представлении: {view.Count} из {table.Rows.Count}");

            int i = 0;
            foreach (DataRowView drv in view)
                Console.WriteLine($"{i++,3} | {Format(drv.Row)}");
        }

        private static void ProveDatabaseUntouched(DataTable table, DbProviderFactory factory)
        {
            Console.WriteLine("\n===== Проверка: меняет ли RowFilter/Sort данные =====");

            List<string> dbBefore = ReadDbSnapshot(factory);
            List<DataRowState> statesBefore = table.Rows.Cast<DataRow>().Select(r => r.RowState).ToList();
            int rowsBefore = table.Rows.Count;

            Console.WriteLine($"До: в БД строк - {dbBefore.Count}, в DataTable - {rowsBefore}");
            Console.WriteLine("Применяем текущие RowFilter и Sort...");
            ShowView(table);

            List<string> dbAfter = ReadDbSnapshot(factory);
            List<DataRowState> statesAfter = table.Rows.Cast<DataRow>().Select(r => r.RowState).ToList();

            Console.WriteLine($"\nПосле: в БД строк - {dbAfter.Count}, в DataTable - {table.Rows.Count}");
            Console.WriteLine($"Содержимое БД не изменилось: {(dbBefore.SequenceEqual(dbAfter) ? "да" : "НЕТ")}");
            Console.WriteLine($"Состояния строк DataTable не изменились: {(statesBefore.SequenceEqual(statesAfter) ? "да" : "НЕТ")}");
            Console.WriteLine($"Число строк DataTable не изменилось: {(rowsBefore == table.Rows.Count ? "да" : "НЕТ")}");
        }

        private static string EscapeLike(string value)
        {
            var sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c == '\'') sb.Append("''");
                else if (c is '*' or '%' or '[' or ']') sb.Append('[').Append(c).Append(']');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static bool SaveChanges(DataTable table, DbProviderFactory factory)
        {
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
            adapter.InsertCommand = MakeCommand(connection, transaction,
                "INSERT INTO students_lesson14 (nickname, first_name, last_name, age) " +
                "VALUES (@nickname, @first_name, @last_name, @age)",
                ("nickname", "nickname", DataRowVersion.Current, DbType.String),
                ("first_name", "first_name", DataRowVersion.Current, DbType.String),
                ("last_name", "last_name", DataRowVersion.Current, DbType.String),
                ("age", "age", DataRowVersion.Current, DbType.Int32));

            adapter.UpdateCommand = MakeCommand(connection, transaction,
                "UPDATE students_lesson14 " +
                "SET nickname = @nickname, first_name = @first_name, last_name = @last_name, age = @age " +
                "WHERE nickname = @old_nickname",
                ("nickname", "nickname", DataRowVersion.Current, DbType.String),
                ("first_name", "first_name", DataRowVersion.Current, DbType.String),
                ("last_name", "last_name", DataRowVersion.Current, DbType.String),
                ("age", "age", DataRowVersion.Current, DbType.Int32),
                ("old_nickname", "nickname", DataRowVersion.Original, DbType.String));

            adapter.DeleteCommand = MakeCommand(connection, transaction,
                "DELETE FROM students_lesson14 WHERE nickname = @old_nickname",
                ("old_nickname", "nickname", DataRowVersion.Original, DbType.String));
        }

        private static DbCommand MakeCommand(
            DbConnection connection,
            DbTransaction transaction,
            string sql,
            params (string Name, string Column, DataRowVersion Version, DbType Type)[] parameters)
        {
            DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = transaction;
            cmd.UpdatedRowSource = UpdateRowSource.None;

            foreach (var (name, column, version, type) in parameters)
            {
                DbParameter p = cmd.CreateParameter();
                p.ParameterName = name;
                p.DbType = type;
                p.SourceColumn = column;
                p.SourceVersion = version;
                cmd.Parameters.Add(p);
            }
            return cmd;
        }

        private static List<string> ReadDbSnapshot(DbProviderFactory factory)
        {
            var result = new List<string>();

            using DbConnection connection = factory.CreateConnection()
                ?? throw new InvalidOperationException();
            connection.ConnectionString = ConnString;
            connection.Open();

            using DbCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT nickname, first_name, last_name, age FROM students_lesson14 ORDER BY nickname";

            using DbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add($"{Convert.ToString(reader.GetValue(0)),-15} | " +
                           $"{Convert.ToString(reader.GetValue(1)),-18} | " +
                           $"{Convert.ToString(reader.GetValue(2)),-15} | " +
                           $"{Convert.ToString(reader.GetValue(3)),-4}");
            }
            return result;
        }

        private static void VerifyInDatabase(DbProviderFactory factory)
        {
            Console.WriteLine("\n===== Содержимое БД (отдельный запрос) =====");
            List<string> rows = ReadDbSnapshot(factory);
            foreach (string line in rows)
                Console.WriteLine(line);
            Console.WriteLine($"Всего строк в БД: {rows.Count}");
        }

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
            => $"{row["first_name", version]} {row["last_name", version]} ({row["age", version]})";

        private static List<string> Snapshot(DataTable table)
        {
            return table.Rows.Cast<DataRow>()
                .Select(r => $"{r["nickname"]}|{r["first_name"]}|{r["last_name"]}|{r["age"]}")
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
                command.CommandText = "SELECT nickname, first_name, last_name, age FROM students_lesson14";

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

        private static object AskAge()
        {
            string s = Ask("Возраст (пусто - NULL)");
            return int.TryParse(s, out int age) ? age : DBNull.Value;
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

            return $"{r.RowState,-10} | {r["nickname", v],-15} | {r["first_name", v],-18} | {r["last_name", v],-15} | {r["age", v],-4}";
        }
    }
}