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

                    case "0":
                        return;

                    default:
                        Console.WriteLine("Неизвестный пункт меню.");
                        break;
                }
            }
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