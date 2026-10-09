using System;
using System.Data;

namespace ConsoleApp1
{
    public class mydatabase
    {
        public static void Main()
        {
            DataTable dt = new DataTable("Students");

            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Age", typeof(int));
            dt.Columns.Add("GroupName", typeof(string));

            dt.Rows.Add(1, "Виталий", 19, "banana_GANG");
            dt.Rows.Add(2, "Сергей", 20, "banana_GANG");
            dt.Rows.Add(3, "Василий", 17, "strawberry_GANG");
            dt.Rows.Add(4, "Егор", 55, "pineapple_GANG");
            dt.Rows.Add(5, "Дмитрий", 16, "strawberry-GANG");

            Console.WriteLine("студенты:");
            foreach (DataRow row in dt.Rows)
            {
                Console.WriteLine(
                    $"#{row["Id"],-5} - {row["Name"],-20} - {row["Age"],-3} лет - {row["GroupName"],-10}"
                );
            }

            DataRow oldest = dt.Rows[0];

            foreach (DataRow row in dt.Rows)
            {
                if ((int)row["Age"] > (int)oldest["Age"])
                {
                    oldest = row;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Самый старый:");
            Console.WriteLine(
                $"#{oldest["Id"],-5} - {oldest["Name"],-20} - {oldest["Age"],-3} лет - {oldest["GroupName"],-10}"
            );
        }
    }
}