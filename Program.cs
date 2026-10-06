using System.Data.Common;
using System.Text;
using Npgsql;

Console.OutputEncoding = Encoding.UTF8;

DbProviderFactories.RegisterFactory("npgsql", NpgsqlFactory.Instance);
DbProviderFactory factory = DbProviderFactories.GetFactory("npgsql");

using DbConnection connection = factory.CreateConnection()!;
connection.ConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
connection.Open();

using (DbCommand create = factory.CreateCommand()!)
{
    create.Connection = connection;
    create.CommandText = "CREATE TABLE IF NOT EXISTS test01 (last_name TEXT, first_name TEXT)";
    create.ExecuteNonQuery();
}

using (DbCommand clear = factory.CreateCommand()!)
{
    clear.Connection = connection;
    clear.CommandText = "DELETE FROM test01";
    clear.ExecuteNonQuery();
}

using (DbCommand insert = factory.CreateCommand()!)
{
    insert.Connection = connection;
    insert.CommandText = "INSERT INTO test01 (last_name, first_name) VALUES (@last, @first)";

    DbParameter pLast = insert.CreateParameter();
    pLast.ParameterName = "@last";
    pLast.Value = "Колегов";
    insert.Parameters.Add(pLast);

    DbParameter pFirst = insert.CreateParameter();
    pFirst.ParameterName = "@first";
    pFirst.Value = "Иосиф";
    insert.Parameters.Add(pFirst);

    insert.ExecuteNonQuery();
}

using (DbCommand select = factory.CreateCommand()!)
{
    select.Connection = connection;
    select.CommandText = "SELECT last_name, first_name FROM test01";

    using DbDataReader reader = select.ExecuteReader();
    while (reader.Read())
    {
        Console.WriteLine(reader.GetString(0) + " " + reader.GetString(1));
    }
}