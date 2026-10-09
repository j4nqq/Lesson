using System;
using System.Data;

namespace ConsoleApp1
{
    public class mydatabase
    {
        public static void Main()
        {
            DataSet ds = new DataSet("University");

            DataTable groups = new DataTable("Groups");
            groups.Columns.Add("Id", typeof(int));
            groups.Columns.Add("Name", typeof(string));
            groups.Columns["Name"].AllowDBNull = false;
            groups.PrimaryKey = new DataColumn[] { groups.Columns["Id"] };

            DataTable students = new DataTable("Students");
            students.Columns.Add("Id", typeof(int));
            students.Columns.Add("Name", typeof(string));
            students.Columns.Add("Age", typeof(int));
            students.Columns.Add("GroupId", typeof(int));
            students.PrimaryKey = new DataColumn[] { students.Columns["Id"] };

            ds.Tables.Add(groups);
            ds.Tables.Add(students);

            DataRelation relation = new DataRelation(
                "GroupStudents",
                groups.Columns["Id"],
                students.Columns["GroupId"]
            );
            ds.Relations.Add(relation);

            groups.Rows.Add(1, "banana_GANG");
            groups.Rows.Add(2, "strawberry_GANG");
            groups.Rows.Add(3, "pineapple_GANG");

            students.Rows.Add(1, "Виталий Паровозов", 19, 1);
            students.Rows.Add(2, "Сергей Левчук", 20, 1);
            students.Rows.Add(3, "Василий Северов", 17, 2);
            students.Rows.Add(4, "Егор Дёмин", 55, 3);
            students.Rows.Add(5, "Дмитрий Щеменюк", 16, 2);
            students.Rows.Add(6, "Андрей Металов", 21, 1);
            students.Rows.Add(7, "Максим Перешев", 18, 2);
            students.Rows.Add(8, "Артём Николаев", 22, 3);
            students.Rows.Add(9, "Никита Лопухов", 17, 3);
            students.Rows.Add(10, "Олег Шмалев", 20, 1);

            Console.WriteLine("Задача 1 ");

            int selectedGroupId = 1;
            DataRow selectedGroup = groups.Rows.Find(selectedGroupId);

            Console.WriteLine($"Студенты группы {selectedGroup["Name"]}:");
            foreach (DataRow student in selectedGroup.GetChildRows(relation))
            {
                Console.WriteLine(
                    $"#{student["Id"],-5} - {student["Name"],-20} - {student["Age"],-3} лет"
                );
            }

            Console.WriteLine();
            Console.WriteLine("Задача 2");

            try
            {
                groups.Rows.Add(1, "duplicate_GANG");
            }
            catch (ConstraintException ex)
            {
                Console.WriteLine("1) Дубликат Id: " + ex.Message);
            }

            AddGroup(groups, 4, "");
            AddGroup(groups, 5, DBNull.Value);

            ds.EnforceConstraints = false;
            students.Rows.Add(11, "Без группы", 18, 99);

            Console.WriteLine("3) Студенты без подходящей группы:");
            foreach (DataRow student in students.Rows)
            {
                if (groups.Rows.Find(student["GroupId"]) == null)
                {
                    Console.WriteLine(
                        $"   #{student["Id"],-5} - {student["Name"],-20} (GroupId = {student["GroupId"]})"
                    );
                }
            }
        }

        static void AddGroup(DataTable groups, int id, object name)
        {
            if (name == null || name == DBNull.Value || string.IsNullOrWhiteSpace(name.ToString()))
            {
                Console.WriteLine($"2) Группу с Id = {id} добавить нельзя: название не может быть пустым.");
                return;
            }

            groups.Rows.Add(id, name);
        }
    }
}