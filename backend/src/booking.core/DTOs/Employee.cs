namespace booking.core.DTOs;

public record EmployeeModel(
    int id,
    string? firstName,
    string? lastName,
    string? email,
    string? phone,
    DateTime? createdAt
);