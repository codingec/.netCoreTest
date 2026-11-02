namespace booking.core.Models;

public class ServiceDetail
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public string? IconUrl { get; set; }
    public DateTime? CreatedAt { get; set; }

    public Service? Service { get; set; }
}