namespace lesson19.Models;

public class Student
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
}
