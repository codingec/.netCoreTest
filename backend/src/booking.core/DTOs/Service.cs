namespace booking.core.DTOs;

public record ServiceDetailModel(
    int id,
    int serviceId,
    string name,
    string? description,
    int? capacity,
    string? iconUrl
);

public record ServiceModel(
    int id,
    int officeId,
    string name,
    string? description,
    int? quantity,
    int? maxBookingsPerWeek,
    int? bookingPerTime,
    string? minMax,
    string? imageUrl,
    List<ServiceDetailModel> serviceDetails
);

public record ServiceDetailUpsertDto(
    int? id,
    string name,
    string? description,
    int? capacity,
    string? iconUrl
);

public record ServiceUpsertDto(
    string name,
    int officeId,
    string? description,
    int? quantity,
    int? maxBookingsPerWeek,
    int? bookingPerTime,
    string? minMax,
    string? imageUrl,
    List<ServiceDetailUpsertDto> serviceDetails
);