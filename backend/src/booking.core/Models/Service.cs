namespace booking.core.Models;

public class Service
{
    public int Id { get; set; }
    public int OfficeId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int? Quantity { get; set; }
    public int? MaxBookingsPerWeek { get; set; }
    public int? BookingPerTime { get; set; }
    public string? MinMax { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? CreatedAt { get; set; }

    public List<ServiceDetail> ServiceDetails { get; set; } = new();
}