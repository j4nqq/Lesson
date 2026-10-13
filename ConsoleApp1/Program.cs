using Npgsql;
using System;
using System.Data;
using System.Data.Common;

namespace dot.net.Lesson13
{
    public class Result
    {
        const string SourceConn = "Host=wotacoma.ru;Port=5090;Username=students;Password=asd123qwe4ewq321dsa;Database=9/3-RPO-24/1";
        const string TargetConn = "Host=wotacoma.ru;Port=5090;Username=j4nqq;Password=bananas228;Database=j4nqq";

        public static void Main()
        {
            DbProviderFactory factory = NpgsqlFactory.Instance;

            var source = new DataTable();
            using (DbConnection src = factory.CreateConnection()!)
            {
                src.ConnectionString = SourceConn;
                src.Open();

                DbCommand cmd = factory.CreateCommand()!;
                cmd.Connection = src;
                cmd.CommandText = "SELECT nickname, first_name, last_name FROM students_lesson13";

                DbDataAdapter ad = factory.CreateDataAdapter()!;
                ad.SelectCommand = cmd;
                ad.Fill(source);
            }
            Console.WriteLine($"Прочитано из источника: {source.Rows.Count}");

            using (DbConnection dst = factory.CreateConnection()!)
            {
                dst.ConnectionString = TargetConn;
                dst.Open();

                Exec(factory, dst, """
                    CREATE TABLE IF NOT EXISTS students (
                        id         SERIAL PRIMARY KEY,
                        nickname   TEXT NOT NULL,
                        first_name TEXT NOT NULL,
                        last_name  TEXT NOT NULL,
                        UNIQUE (nickname, first_name, last_name)
                    )
                    """);

                int added = 0;
                foreach (DataRow r in source.Rows)
                {
                    added += Exec(factory, dst, """
                        INSERT INTO students (nickname, first_name, last_name)
                        VALUES (@n, @f, @l)
                        ON CONFLICT DO NOTHING
                        """,
                        ("@n", r["nickname"]),
                        ("@f", r["first_name"]),
                        ("@l", r["last_name"]));
                }
                Console.WriteLine($"Перенесено в вашу БД: {added}");
            }
        }

        private static int Exec(DbProviderFactory factory, DbConnection conn, string sql,
                                params (string name, object value)[] args)
        {
            DbCommand cmd = factory.CreateCommand()!;
            cmd.Connection = conn;
            cmd.CommandText = sql;
            foreach (var (name, value) in args)
            {
                DbParameter p = cmd.CreateParameter();
                p.ParameterName = name;
                p.Value = value;
                cmd.Parameters.Add(p);
            }
            return cmd.ExecuteNonQuery();
        }
    }
}