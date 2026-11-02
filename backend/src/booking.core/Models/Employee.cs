namespace booking.core.Models;

public class Employee
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? CreatedAt { get; set; }
}