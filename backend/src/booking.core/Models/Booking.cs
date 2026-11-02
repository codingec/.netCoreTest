namespace booking.core.Models;

public class Booking
{
    public int Id { get; set; }
    public int ServiceDetailId { get; set; }
    public int EmployeeId { get; set; } = default!;
    public DateTime StartDatetime { get; set; }
    public DateTime EndDatetime { get; set; }
    public DateTime? CreatedAt { get; set; }
    public ServiceDetail? ServiceDetail { get; set; }
    public Employee? Employee { get; set; }
}