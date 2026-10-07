using System.Diagnostics;
using System.Text;
using Npgsql;

namespace SpaceStation;

record DiagResult(string Name, string Status);

class Program
{
    const string ConnectionString =
        "Host=wotacoma.ru;Port=5090;Username=j4nqq;Password=bananas228;Database=j4nqq";

    static NpgsqlDataSource dataSource = null!;

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        dataSource = NpgsqlDataSource.Create(ConnectionString);

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            await using var ping = new NpgsqlCommand("SELECT 1;", connection);
            await ping.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось подключиться к базе данных:\n{ex}");
            Console.ReadLine();
            return;
        }

        try
        {
            await InitDatabaseAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42501")
        {
            Console.WriteLine("Нет прав на создание таблиц. Продолжаю с существующими таблицами.");
            Console.WriteLine("Нажмите Enter...");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка инициализации БД: {ex.Message}");
            Console.ReadLine();
        }

        while (true)
        {
            PrintMenu();
            string? choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1": await CrewMenuAsync(); break;
                    case "2": await ShowSystemsAsync(); break;
                    case "3": await CheckConnectionAsync(); break;
                    case "4": await ShowMessagesAsync(); break;
                    case "5": await RunDiagnosticsAsync(emergency: false); break;
                    case "6": await RunDiagnosticsAsync(emergency: true); break;
                    case "0":
                        await dataSource.DisposeAsync();
                        return;
                    default:
                        Console.WriteLine("Неизвестная команда.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine("\nНажмите Enter...");
            Console.ReadLine();
        }
    }

    static void PrintMenu()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║      SPACE STATION CONTROL           ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        Console.WriteLine("║ 1. Экипаж                            ║");
        Console.WriteLine("║ 2. Состояние систем                  ║");
        Console.WriteLine("║ 3. Проверить связь                   ║");
        Console.WriteLine("║ 4. Получить сообщения станции        ║");
        Console.WriteLine("║ 5. Полная диагностика                ║");
        Console.WriteLine("║ 6. Аварийный режим                   ║");
        Console.WriteLine("║ 0. Выход                             ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Выберите действие:");
        Console.Write("> ");
    }

    static async Task CrewMenuAsync()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== ЭКИПАЖ ===");
            Console.WriteLine("1. Показать весь экипаж");
            Console.WriteLine("2. Поиск члена экипажа");
            Console.WriteLine("3. Изменить состояние экипажа");
            Console.WriteLine("0. Назад");
            Console.Write("> ");

            string? c = Console.ReadLine()?.Trim();
            if (c == "0") return;

            try
            {
                switch (c)
                {
                    case "1": await ShowCrewAsync(); break;
                    case "2": await SearchCrewAsync(); break;
                    case "3": await ChangeCrewStateAsync(); break;
                    default: Console.WriteLine("Неизвестная команда."); break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine("\nНажмите Enter...");
            Console.ReadLine();
        }
    }

    static async Task InitDatabaseAsync()
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        const string sql = """
            CREATE TABLE IF NOT EXISTS crew (
                id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name varchar(100) NOT NULL,
                role varchar(50) NOT NULL,
                health integer NOT NULL CHECK (health BETWEEN 0 AND 100),
                status varchar(30) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS station_systems (
                id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name varchar(100) NOT NULL,
                status varchar(30) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS station_messages (
                id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                sent_at timestamptz NOT NULL DEFAULT now(),
                sender varchar(100) NOT NULL,
                text varchar(300) NOT NULL
            );

            INSERT INTO crew (name, role, health, status)
            SELECT * FROM (VALUES
                ('Алексей Волков',  'Командир', 100, 'На станции'),
                ('Мария Орлова',    'Инженер',   85, 'На станции'),
                ('Дмитрий Соколов', 'Пилот',     92, 'На станции'),
                ('Анна Крылова',    'Медик',    100, 'На станции'),
                ('Илья Морозов',    'Инженер',   73, 'На станции')
            ) AS v(name, role, health, status)
            WHERE NOT EXISTS (SELECT 1 FROM crew);

            INSERT INTO station_systems (name, status)
            SELECT * FROM (VALUES
                ('Двигатели',        'OK'),
                ('Жизнеобеспечение', 'OK'),
                ('Навигация',        'WARNING'),
                ('Связь',            'OK')
            ) AS v(name, status)
            WHERE NOT EXISTS (SELECT 1 FROM station_systems);

            INSERT INTO station_messages (sender, text)
            SELECT * FROM (VALUES
                ('Центр управления полётами', 'Стыковка грузового корабля запланирована на завтра.'),
                ('Бортовой компьютер',        'Навигация: обнаружено отклонение датчика.'),
                ('Алексей Волков',            'Плановый выход в открытый космос завершён.')
            ) AS v(sender, text)
            WHERE NOT EXISTS (SELECT 1 FROM station_messages);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    static async Task ShowCrewAsync()
    {
        await using var connection = dataSource.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT id, name, role, health, status FROM crew ORDER BY id;", connection);

        await using var reader = await command.ExecuteReaderAsync();
        await PrintCrewTableAsync(reader);
    }

    static async Task PrintCrewTableAsync(NpgsqlDataReader reader)
    {
        Console.WriteLine("╔════╦══════════════════╦═══════════╦═════════╦════════════╗");
        Console.WriteLine("║ ID ║ Имя              ║ Роль      ║ Здоровье║ Статус     ║");
        Console.WriteLine("╠════╬══════════════════╬═══════════╬═════════╬════════════╣");

        bool any = false;
        while (await reader.ReadAsync())
        {
            any = true;
            Console.WriteLine(
                $"║ {reader.GetInt32(0),-2} ║ {reader.GetString(1),-16} ║ {reader.GetString(2),-9} ║ {reader.GetInt32(3),-7} ║ {reader.GetString(4),-10} ║");
        }

        Console.WriteLine("╚════╩══════════════════╩═══════════╩═════════╩════════════╝");
        if (!any) Console.WriteLine("Ничего не найдено.");
    }

    static async Task SearchCrewAsync()
    {
        Console.WriteLine("Введите имя:");
        Console.Write("> ");
        string name = Console.ReadLine()?.Trim() ?? "";

        await using var connection = dataSource.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT id, name, role, health, status FROM crew WHERE name ILIKE @name ORDER BY id;",
            connection);

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@name";
        parameter.Value = $"%{name}%";
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync();
        await PrintCrewTableAsync(reader);
    }

    static async Task ChangeCrewStateAsync()
    {
        Console.WriteLine("\n1. Нанести урон");
        Console.WriteLine("2. Восстановить здоровье");
        Console.WriteLine("3. Изменить статус");
        Console.WriteLine("4. Назад");
        Console.Write("> ");
        string? c = Console.ReadLine()?.Trim();

        if (c is not ("1" or "2" or "3")) return;

        await ShowCrewAsync();

        Console.WriteLine("\nВыберите ID:");
        Console.Write("> ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Некорректный ID.");
            return;
        }

        await using var connection = dataSource.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        if (c == "3")
        {
            Console.WriteLine("\nВведите новый статус:");
            Console.Write("> ");
            string status = Console.ReadLine()?.Trim() ?? "";
            if (status.Length == 0 || status.Length > 30)
            {
                Console.WriteLine("Статус должен быть от 1 до 30 символов.");
                return;
            }

            command.CommandText = """
                UPDATE crew c
                SET status = @status
                FROM (SELECT id, status AS old_status FROM crew WHERE id = @id) o
                WHERE c.id = o.id
                RETURNING c.name, o.old_status, c.status;
                """;
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                Console.WriteLine("Член экипажа не найден.");
                return;
            }

            Console.WriteLine($"\n{reader.GetString(0)}\n\nСтатус:\n{reader.GetString(1)} → {reader.GetString(2)}\n\nОперация выполнена.");
            return;
        }

        bool damage = c == "1";
        Console.WriteLine(damage ? "\nВведите урон:" : "\nВведите восстановление:");
        Console.Write("> ");
        if (!int.TryParse(Console.ReadLine(), out int amount) || amount < 0)
        {
            Console.WriteLine("Некорректное число.");
            return;
        }

        string expr = damage ? "GREATEST(c.health - @amount, 0)" : "LEAST(c.health + @amount, 100)";

        command.CommandText = $"""
            UPDATE crew c
            SET health = {expr}
            FROM (SELECT id, health AS old_health FROM crew WHERE id = @id) o
            WHERE c.id = o.id
            RETURNING c.name, o.old_health, c.health;
            """;
        command.Parameters.AddWithValue("@amount", amount);
        command.Parameters.AddWithValue("@id", id);

        await using var r = await command.ExecuteReaderAsync();
        if (!await r.ReadAsync())
        {
            Console.WriteLine("Член экипажа не найден.");
            return;
        }

        Console.WriteLine($"\n{r.GetString(0)}\n\nЗдоровье:\n{r.GetInt32(1)} → {r.GetInt32(2)}\n\nОперация выполнена.");
    }

    static async Task ShowSystemsAsync()
    {
        var systems = await CheckSystemsAsync();

        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║         СОСТОЯНИЕ СИСТЕМ             ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        foreach (var s in systems)
            Console.WriteLine($"║  {s.Name,-20} {s.Status,-14} ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
    }

    static async Task CheckConnectionAsync()
    {
        var result = await CheckCommunicationAsync();
        Console.WriteLine($"Связь: {result.Status} ({result.Name})");
    }

    static async Task ShowMessagesAsync()
    {
        await using var connection = dataSource.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT sent_at, sender, text FROM station_messages ORDER BY sent_at DESC LIMIT 10;",
            connection);

        await using var reader = await command.ExecuteReaderAsync();

        Console.WriteLine("=== СООБЩЕНИЯ СТАНЦИИ ===\n");
        bool any = false;
        while (await reader.ReadAsync())
        {
            any = true;
            Console.WriteLine($"[{reader.GetDateTime(0).ToLocalTime():dd.MM.yyyy HH:mm}] {reader.GetString(1)}");
            Console.WriteLine($"    {reader.GetString(2)}\n");
        }
        if (!any) Console.WriteLine("Сообщений нет.");
    }

    static async Task RunDiagnosticsAsync(bool emergency)
    {
        if (emergency)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ВНИМАНИЕ!\n");
            Console.ResetColor();
        }
        Console.WriteLine("Проверка систем...\n");

        var sw = Stopwatch.StartNew();

        Task<DiagResult> crewTask = CheckCrewAsync();
        Task<List<DiagResult>> systemsTask = CheckSystemsAsync();
        Task<DiagResult> communicationTask = CheckCommunicationAsync();

        await Task.WhenAll(crewTask, systemsTask, communicationTask);

        sw.Stop();

        var results = new List<DiagResult> { crewTask.Result };
        results.AddRange(systemsTask.Result.Where(s => s.Name != "Связь"));
        results.Add(new DiagResult("Связь", communicationTask.Result.Status));

        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║         РЕЗУЛЬТАТ ДИАГНОСТИКИ        ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        foreach (var r in results)
        {
            Console.Write($"║  {r.Name,-20}");
            Console.ForegroundColor = r.Status switch
            {
                "OK" => ConsoleColor.Green,
                "WARNING" => ConsoleColor.Yellow,
                _ => ConsoleColor.Red
            };
            Console.Write($" {r.Status,-14}");
            Console.ResetColor();
            Console.WriteLine(" ║");
        }
        Console.WriteLine("╚══════════════════════════════════════╝\n");

        if (results.Any(r => r.Status is "CRITICAL" or "ERROR"))
            Console.WriteLine("Станция в критическом состоянии!");
        else if (results.Any(r => r.Status == "WARNING"))
            Console.WriteLine("Станция работоспособна, но есть предупреждения.");
        else
            Console.WriteLine("Станция работоспособна.");

        Console.WriteLine($"\nДиагностика заняла {sw.ElapsedMilliseconds} мс (связь: {communicationTask.Result.Name}).");
    }

    static async Task<DiagResult> CheckCrewAsync()
    {
        try
        {
            await using var connection = dataSource.CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand("SELECT health FROM crew;", connection);
            await using var reader = await command.ExecuteReaderAsync();

            int min = 100;
            bool any = false;
            while (await reader.ReadAsync())
            {
                any = true;
                min = Math.Min(min, reader.GetInt32(0));
            }

            if (!any) return new DiagResult("Экипаж", "WARNING");
            if (min == 0) return new DiagResult("Экипаж", "CRITICAL");
            if (min < 50) return new DiagResult("Экипаж", "WARNING");
            return new DiagResult("Экипаж", "OK");
        }
        catch
        {
            return new DiagResult("Экипаж", "ERROR");
        }
    }

    static async Task<List<DiagResult>> CheckSystemsAsync()
    {
        var list = new List<DiagResult>();

        await using var connection = dataSource.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT name, status FROM station_systems ORDER BY id;", connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            list.Add(new DiagResult(reader.GetString(0), reader.GetString(1)));

        return list;
    }

    static async Task<DiagResult> CheckCommunicationAsync()
    {
        try
        {
            var sw = Stopwatch.StartNew();

            await using var connection = dataSource.CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand("SELECT now();", connection);
            await command.ExecuteScalarAsync();

            sw.Stop();
            string status = sw.ElapsedMilliseconds < 500 ? "OK" : "WARNING";
            return new DiagResult($"отклик {sw.ElapsedMilliseconds} мс", status);
        }
        catch
        {
            return new DiagResult("нет ответа", "ERROR");
        }
    }
}
