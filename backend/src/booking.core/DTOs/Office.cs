namespace booking.core.DTOs;

public record OfficeModel(
    int id,
    string? name,
    string? image
);

public record OfficeUpsertDto(
    int id,
    string? name,
    string? image
);