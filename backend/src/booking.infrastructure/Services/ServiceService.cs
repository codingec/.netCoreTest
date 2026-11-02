using Microsoft.EntityFrameworkCore;
using booking.core.Constants;
using booking.core.DTOs;
using booking.core.Models;
using booking.infrastructure.Persistence;

namespace booking.infrastructure.Services;

public interface IServicesService
{
    Task<(bool ok, List<ServiceModel> data, string code)> GetAllAsync(CancellationToken ct);
    Task<(bool ok, ServiceModel? data, string code)> GetByIdAsync(int id, CancellationToken ct);
    Task<(bool ok, int id, string code, string? message)> CreateAsync(ServiceUpsertDto dto, CancellationToken ct);
    Task<(bool ok, string code, string? message)> UpdateAsync(int id, ServiceUpsertDto dto, CancellationToken ct);
    Task<(bool ok, string code, string? message)> DeleteAsync(int id, CancellationToken ct);
}

public class ServicesService : IServicesService
{
    private readonly AppDbContext _db;
    public ServicesService(AppDbContext db) => _db = db;

    public async Task<(bool ok, List<ServiceModel> data, string code)> GetAllAsync(CancellationToken ct)
    {
        var items = await _db.Services
            .AsNoTracking()
            .Include(s => s.ServiceDetails)
            .OrderBy(s => s.Id)
            .Select(s => MapToModel(s))
            .ToListAsync(ct);

        return (true, items, Codes.Ok);
    }

    public async Task<(bool ok, ServiceModel? data, string code)> GetByIdAsync(int id, CancellationToken ct)
    {
        var s = await _db.Services
            .AsNoTracking()
            .Include(x => x.ServiceDetails)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (s is null) return (false, null, Codes.NotFound);
        return (true, MapToModel(s), Codes.Ok);
    }

    public async Task<(bool ok, int id, string code, string? message)> CreateAsync(ServiceUpsertDto dto, CancellationToken ct)
    {
        var s = new Service
        {
            OfficeId = dto.officeId,
            Name = dto.name,
            Description = dto.description,
            Quantity = dto.quantity,
            MaxBookingsPerWeek = dto.maxBookingsPerWeek,
            BookingPerTime = dto.bookingPerTime,
            MinMax = dto.minMax,
            ImageUrl = dto.imageUrl,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.serviceDetails is { Count: > 0 })
        {
            s.ServiceDetails = dto.serviceDetails.Select(d => new ServiceDetail
            {
                Name = d.name,
                Description = d.description,
                Capacity = d.capacity,
                IconUrl = d.iconUrl,
                CreatedAt = DateTime.UtcNow
            }).ToList();
        }

        _db.Services.Add(s);
        await _db.SaveChangesAsync(ct);
        return (true, s.Id, Codes.Ok, null);
    }

    public async Task<(bool ok, string code, string? message)> UpdateAsync(int id, ServiceUpsertDto dto, CancellationToken ct)
    {
        var s = await _db.Services.Include(x => x.ServiceDetails).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return (false, Codes.NotFound, "Service not found.");

        s.OfficeId = dto.officeId;
        s.Name = dto.name;
        s.Description = dto.description;
        s.Quantity = dto.quantity;
        s.MaxBookingsPerWeek = dto.maxBookingsPerWeek;
        s.BookingPerTime = dto.bookingPerTime;
        s.MinMax = dto.minMax;
        s.ImageUrl = dto.imageUrl;

        _db.ServiceDetails.RemoveRange(s.ServiceDetails);
        s.ServiceDetails.Clear();

        if (dto.serviceDetails is { Count: > 0 })
        {
            foreach (var d in dto.serviceDetails)
            {
                s.ServiceDetails.Add(new ServiceDetail
                {
                    Name = d.name,
                    Description = d.description,
                    Capacity = d.capacity,
                    IconUrl = d.iconUrl,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return (true, Codes.Ok, null);
    }

    public async Task<(bool ok, string code, string? message)> DeleteAsync(int id, CancellationToken ct)
    {
        var s = await _db.Services.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return (false, Codes.NotFound, "Service not found.");

        _db.Services.Remove(s);
        await _db.SaveChangesAsync(ct);
        return (true, Codes.Ok, null);
    }

    private static ServiceModel MapToModel(Service s) =>
        new(
            s.Id,
            s.OfficeId,
            s.Name,
            s.Description,
            s.Quantity,
            s.MaxBookingsPerWeek,
            s.BookingPerTime,
            s.MinMax,
            s.ImageUrl,
            s.ServiceDetails
                .OrderBy(d => d.Id)
                .Select(d => new ServiceDetailModel(d.Id, d.ServiceId, d.Name, d.Description, d.Capacity, d.IconUrl))
                .ToList()
        );
}