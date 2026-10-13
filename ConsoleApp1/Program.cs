using Npgsql;
using System;
using System.Data;
using System.Data.Common;

namespace dot.net.Lesson13
{
    public class Result
    {
        public static void Main()
        {
            string connString = "Host=wotacoma.ru;Port=5090;Username=students;Password=asd123qwe4ewq321dsa;Database=9/3-RPO-24/1";

            string nickname = "j4nqq";
            string firstName = "Иосиф";
            string lastName = "Колегов";

            DbProviderFactory factory = NpgsqlFactory.Instance;
            var table = new DataTable();

            DbConnection conn = factory.CreateConnection()!;
            conn.ConnectionString = connString;

            try
            {
                conn.Open();

                DbCommand insertCmd = factory.CreateCommand()!;
                insertCmd.Connection = conn;
                insertCmd.CommandText = """
                    INSERT INTO students_lesson13 (nickname, first_name, last_name)
                    VALUES (@nickname, @first_name, @last_name)
                    ON CONFLICT DO NOTHING
                    """;

                AddParam(insertCmd, "@nickname", nickname);
                AddParam(insertCmd, "@first_name", firstName);
                AddParam(insertCmd, "@last_name", lastName);

                int rows = insertCmd.ExecuteNonQuery();
                Console.WriteLine(rows > 0
                    ? "Студент добавлен"
                    : "Уже добавлен, вставки не буде! ");

                DbCommand selectCmd = factory.CreateCommand()!;
                selectCmd.Connection = conn;
                selectCmd.CommandText = "SELECT * FROM students_lesson2";

                DbDataAdapter adapter = factory.CreateDataAdapter()!;
                adapter.SelectCommand = selectCmd;
                adapter.Fill(table);
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }

            Console.WriteLine($"Загружено строк: {table.Rows.Count}");

            foreach (DataColumn col in table.Columns)
                Console.Write($"{col.ColumnName,-20} | ");
            Console.WriteLine();

            foreach (DataRow row in table.Rows)
            {
                foreach (var item in row.ItemArray)
                    Console.Write($"{item,-20} | ");
                Console.WriteLine();
            }
        }

        private static void AddParam(DbCommand cmd, string name, object value)
        {
            DbParameter p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
    }
}