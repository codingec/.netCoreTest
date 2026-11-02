namespace booking.core.DTOs;

public record BookingModel(
    int id,
    int serviceDetailId,
    int employeeId,
    DateTime? startDatetime,
    DateTime? endDatetime,
    DateTime? createdAt
);

public record BookingUpsertDto(
    int id,
    int serviceDetailId,
    int employeeId,
    DateTime startDatetime,
    DateTime endDatetime,
    DateTime createdAt
);

public record AvailabilityTimes(
    string time,
    bool isAvailable
);

public record Availability(
    int id,
    string date,
    string frequency
);

public class BookingRow
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ServiceDetailName { get; set; } = string.Empty;
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
}
