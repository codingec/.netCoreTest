using Microsoft.EntityFrameworkCore;
using booking.core.Constants;
using booking.core.DTOs;
using booking.core.Models;
using booking.infrastructure.Persistence;

namespace booking.infrastructure.Services;

public interface IOfficeService
{
    Task<(bool ok, List<OfficeModel> data, string code)> GetAllAsync(CancellationToken ct);
    Task<(bool ok, OfficeModel? data, string code)> GetByIdAsync(int id, CancellationToken ct);
    Task<(bool ok, int id, string code, string? message)> CreateAsync(OfficeUpsertDto dto, CancellationToken ct);
    Task<(bool ok, string code, string? message)> UpdateAsync(int id, OfficeUpsertDto dto, CancellationToken ct);
    Task<(bool ok, string code, string? message)> DeleteAsync(int id, CancellationToken ct);
}

public class OfficeService : IOfficeService
{
    private readonly AppDbContext _db;
    public OfficeService(AppDbContext db) => _db = db;

    public async Task<(bool ok, List<OfficeModel> data, string code)> GetAllAsync(CancellationToken ct)
    {
        var items = await _db.Offices
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => MapToModel(s))
            .ToListAsync(ct);

        return (true, items, Codes.Ok);
    }

    public async Task<(bool ok, OfficeModel? data, string code)> GetByIdAsync(int id, CancellationToken ct)
    {
        var s = await _db.Offices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (s is null) return (false, null, Codes.NotFound);
        return (true, MapToModel(s), Codes.Ok);
    }

    public async Task<(bool ok, int id, string code, string? message)> CreateAsync(OfficeUpsertDto dto, CancellationToken ct)
    {
        var s = new Office
        {
            Id = dto.id,
            Name = dto.name,
            Image = dto.image
        };

        _db.Offices.Add(s);
        await _db.SaveChangesAsync(ct);
        return (true, s.Id, Codes.Ok, null);
    }

    public async Task<(bool ok, string code, string? message)> UpdateAsync(int id, OfficeUpsertDto dto, CancellationToken ct)
    {
        var s = await _db.Offices.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return (false, Codes.NotFound, "Office not found.");

        s.Name = dto.name;
        s.Image = dto.image;

        await _db.SaveChangesAsync(ct);
        return (true, Codes.Ok, null);
    }

    public async Task<(bool ok, string code, string? message)> DeleteAsync(int id, CancellationToken ct)
    {
        var s = await _db.Offices.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return (false, Codes.NotFound, "Office not found.");

        _db.Offices.Remove(s);
        await _db.SaveChangesAsync(ct);
        return (true, Codes.Ok, null);
    }

    private static OfficeModel MapToModel(Office s) =>
        new(
            s.Id,
            s.Name,
            s.Image
        );
}