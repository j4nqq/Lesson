using Npgsql;
using System;
using System.Data;
using System.Data.Common;
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
            DataRow? pendingRow = null;

            Console.WriteLine($"Загружено строк: {table.Rows.Count}. Соединение с БД закрыто.");

            while (true)
            {
                Console.WriteLine("""

                    ========== МЕНЮ ==========
                    1. Показать все строки и их состояния
                    2. Создать новую строку (NewRow) - состояние до добавления
                    3. Добавить созданную строку в таблицу (Rows.Add)
                    4. Изменить имя существующего студента
                    5. Удалить студента (Delete)
                    6. RejectChanges() - откатить изменения
                    7. AcceptChanges() - принять изменения
                    8. Сводка по количеству строк в каждом состоянии
                    9. Перезагрузить данные из БД (сброс локальных изменений)
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
                        pendingRow = table.NewRow();
                        pendingRow["nickname"] = Ask("nickname");
                        pendingRow["first_name"] = Ask("Имя (first_name)");
                        pendingRow["last_name"] = Ask("Фамилия (last_name)");
                        Console.WriteLine("Строка создана, но в таблицу не добавлена.");
                        ShowRow("Состояние новой строки", pendingRow);
                        break;

                    case "3":
                        if (pendingRow == null || pendingRow.RowState != DataRowState.Detached)
                        {
                            Console.WriteLine("Сначала создайте новую строку (пункт 2).");
                            break;
                        }
                        table.Rows.Add(pendingRow);
                        ShowRow("Состояние после Rows.Add()", pendingRow);
                        break;

                    case "4":
                        {
                            DataRow? row = PickRow(table, "Кого переименовать");
                            if (row == null) break;
                            row["first_name"] = Ask("Новое имя");
                            ShowRow("Состояние после изменения", row);
                        }
                        break;

                    case "5":
                        {
                            DataRow? row = PickRow(table, "Кого удалить");
                            if (row == null) break;
                            row.Delete();
                            ShowRow("Состояние после Delete()", row);
                        }
                        break;

                    case "6":
                        table.RejectChanges();
                        Console.WriteLine("RejectChanges() выполнен: изменённые и удалённые строки восстановлены, добавленные исчезли.");
                        if (pendingRow != null)
                            ShowRow("Ранее добавленная строка", pendingRow);
                        Console.WriteLine($"Строк в таблице: {table.Rows.Count}");
                        break;

                    case "7":
                        table.AcceptChanges();
                        Console.WriteLine("AcceptChanges() выполнен.");
                        if (pendingRow != null)
                            ShowRow("Ранее добавленная строка", pendingRow);
                        Console.WriteLine($"Строк в таблице: {table.Rows.Count}");
                        Console.WriteLine("""

                            Почему это НЕ сохраняет изменения в PostgreSQL:
                            AcceptChanges() работает только внутри DataTable: он делает текущие значения
                            «исходными» (Added/Modified -> Unchanged) и физически убирает Deleted-строки
                            из локальной коллекции. SQL-команды при этом не отправляются.
                            В БД изменения уходят только при DataAdapter.Update(), который по RowState
                            выполняет InsertCommand / UpdateCommand / DeleteCommand. После AcceptChanges
                            все строки уже Unchanged, и Update() было бы нечего отправлять.
                            """);
                        break;

                    case "8":
                        ShowSummary(table);
                        break;

                    case "9":
                        table = LoadTable(factory);
                        pendingRow = null;
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

        private static void ShowRow(string title, DataRow row)
        {
            Console.WriteLine($"--- {title} ---");
            Console.WriteLine(Format(row));
        }

        private static void ShowSummary(DataTable table)
        {
            Console.WriteLine($"\nВсего строк в коллекции: {table.Rows.Count}");
            foreach (DataRowState state in new[]
                     { DataRowState.Unchanged, DataRowState.Added,
                       DataRowState.Modified, DataRowState.Deleted })
            {
                int count = 0;
                foreach (DataRow r in table.Rows)
                    if (r.RowState == state) count++;
                Console.WriteLine($"{state,-10}: {count}");
            }
        }

        private static string Format(DataRow r)
        {
            if (r.RowState == DataRowState.Detached)
            {
                try
                {
                    return $"{r.RowState,-10} | {r["nickname"],-15} | {r["first_name"],-18} | {r["last_name"],-15}";
                }
                catch (RowNotInTableException)
                {
                    return $"{r.RowState,-10} | (строка удалена из таблицы, данных нет)";
                }
            }

            DataRowVersion v = r.RowState == DataRowState.Deleted
                ? DataRowVersion.Original
                : DataRowVersion.Current;

            return $"{r.RowState,-10} | {r["nickname", v],-15} | {r["first_name", v],-18} | {r["last_name", v],-15}";
        }
    }
}