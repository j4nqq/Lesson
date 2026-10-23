using DatabaseCourseExam.AdoNet;
using System.Data;
using DatabaseCourseExam.EntityFramework;
using DatabaseCourseExam.DapperTasks;

DataTable? studentsTable = null;

while (true)
{
    Console.WriteLine();
    Console.WriteLine("=== Зачёт по работе с БД ===");
    Console.WriteLine("1. Показать студентов");
    Console.WriteLine("2. Изменить возраст студента");
    Console.WriteLine("3. Записать студента на курс");
    Console.WriteLine("4. Загрузить студентов в DataTable");
    Console.WriteLine("5. Изменить возраст в DataTable");
    Console.WriteLine("6. Показать DataTable");
    Console.WriteLine("7. Сохранить изменения DataTable");
    Console.WriteLine("8. EF Core: показать студентов");
    Console.WriteLine("9. EF Core: изменить возраст студента");
    Console.WriteLine("10. EF Core: показать курсы");
    Console.WriteLine("11. Dapper: показать студентов");
    Console.WriteLine("12. Dapper: изменить возраст");
    Console.WriteLine("13. Dapper: показать курсы");
    Console.WriteLine("14. Dapper: записать студента на курс");
    Console.WriteLine("15. Добавить студента в DataTable");
    Console.WriteLine("0. Выход");
    Console.Write("Выберите действие: ");

    string? choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                await AdoNetTasks.ShowStudentsAsync();
                break;

            //ЗАДАНИЕ 4
            case "2":
                {
                    Console.Write("ID студента: ");

                    if (!int.TryParse(Console.ReadLine(), out int studentId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("Новый возраст: ");

                    if (!int.TryParse(Console.ReadLine(), out int age))
                    {
                        Console.WriteLine("Введите целое число.");
                        break;
                    }

                    await AdoNetTasks.UpdateStudentAgeAsync(studentId, age);
                    break;
                }

            case "3":
                {
                    Console.Write("ID студента: ");

                    if (!int.TryParse(
                            Console.ReadLine(),
                            out int enrollmentStudentId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("ID курса: ");

                    if (!int.TryParse(Console.ReadLine(), out int courseId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    await AdoNetTasks.EnrollStudentAsync(
                        enrollmentStudentId,
                        courseId);
                    break;
                }

            case "4":
                studentsTable = await DisconnectedTasks.LoadStudentsAsync();
                Console.WriteLine("Данные загружены в память.");
                DisconnectedTasks.PrintTable(studentsTable);
                break;

            case "5":
                {
                    if (studentsTable is null)
                    {
                        Console.WriteLine("Сначала загрузите данные (пункт 4).");
                        break;
                    }

                    Console.Write("ID студента: ");

                    if (!int.TryParse(
                            Console.ReadLine(),
                            out int disconnectedStudentId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("Новый возраст: ");

                    if (!int.TryParse(
                            Console.ReadLine(),
                            out int disconnectedAge))
                    {
                        Console.WriteLine("Введите целое число.");
                        break;
                    }

                    if (disconnectedAge is < 14 or > 100)
                    {
                        Console.WriteLine("Возраст должен быть от 14 до 100.");
                        break;
                    }

                    DisconnectedTasks.ChangeAgeInMemory(
                        studentsTable,
                        disconnectedStudentId,
                        disconnectedAge);
                    break;
                }
            //ЗАДАНИЕ 4

            case "6":
                if (studentsTable is null)
                {
                    Console.WriteLine("Сначала загрузите данные (пункт 4).");
                    break;
                }

                DisconnectedTasks.PrintTable(studentsTable);
                break;

            case "7":
                if (studentsTable is null)
                {
                    Console.WriteLine("Сначала загрузите данные (пункт 4).");
                    break;
                }

                DisconnectedTasks.SaveChanges(studentsTable);
                break;

            case "8":
                await EntityFrameworkTasks.ShowStudentsAsync();
                break;

            case "9":
                {
                    Console.Write("ID студента: ");

                    if (!int.TryParse(Console.ReadLine(), out int stId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("Новый возраст: ");

                    if (!int.TryParse(Console.ReadLine(), out int newAge)
                        || newAge is < 14 or > 100)
                    {
                        Console.WriteLine("Возраст должен быть от 14 до 100.");
                        break;
                    }

                    await EntityFrameworkTasks.UpdateStudentAgeAsync(
                        stId,
                        newAge);

                    break;
                }

            case "10":
                await EntityFrameworkTasks.ShowCoursesAsync();
                break;

            case "11":
                await DapperStudentTasks.ShowStudentsAsync();
                break;

            case "12":
                {
                    Console.Write("ID студента: ");

                    if (!int.TryParse(Console.ReadLine(), out int dStudentId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("Новый возраст: ");

                    if (!int.TryParse(Console.ReadLine(), out int newAge)
                        || newAge is < 14 or > 100)
                    {
                        Console.WriteLine("Возраст должен быть от 14 до 100.");
                        break;
                    }

                    await DapperStudentTasks.UpdateStudentAgeAsync(
                        dStudentId,
                        newAge);

                    break;
                }

            case "13":
                await DapperStudentTasks.ShowCoursesAsync();
                break;

            case "14":
                {
                    Console.Write("ID студента: ");

                    if (!int.TryParse(Console.ReadLine(), out int dStudentId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("ID курса: ");

                    if (!int.TryParse(Console.ReadLine(), out int dCourseId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    await DapperStudentTasks.EnrollStudentAsync(
                        dStudentId,
                        dCourseId);

                    break;
                }

            //ЗАДАНИЕ 6
            case "15":
                {
                    if (studentsTable is null)
                    {
                        Console.WriteLine("Сначала загрузите данные (пункт 4).");
                        break;
                    }

                    Console.Write("Имя: ");
                    string newFirstName = Console.ReadLine() ?? string.Empty;

                    Console.Write("Фамилия: ");
                    string newLastName = Console.ReadLine() ?? string.Empty;

                    Console.Write("Возраст: ");

                    if (!int.TryParse(Console.ReadLine(), out int newStudentAge))
                    {
                        Console.WriteLine("Введите целое число.");
                        break;
                    }

                    Console.Write("ID группы: ");

                    if (!int.TryParse(Console.ReadLine(), out int newStudentGroupId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    DisconnectedTasks.AddStudentInMemory(
                        studentsTable,
                        newFirstName,
                        newLastName,
                        newStudentAge,
                        newStudentGroupId);
                    break;
                }
            //ЗАДАНИЕ 6

            case "0":
                return;

            default:
                Console.WriteLine("Неизвестная команда.");
                break;
        }
    }
    catch (FormatException)
    {
        Console.WriteLine("Введите целое число.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}