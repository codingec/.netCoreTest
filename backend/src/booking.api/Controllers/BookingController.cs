using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using booking.core.Constants;
using booking.core.DTOs;
using booking.infrastructure.Services;

namespace booking.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _svc;
    public BookingController(IBookingService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var (ok, data, code) = await _svc.GetAllAsync(ct);
        return Ok(new { code, data });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var (ok, data, code) = await _svc.GetByIdAsync(id, ct);
        if (!ok || data is null) return NotFound(new { code = Codes.NotFound, message = "Booking not found." });
        return Ok(new { code, data });
    }

    [HttpGet("Service")]
    public async Task<IActionResult> GetByServiceId(
        [FromQuery] int? serviceId,
        CancellationToken ct)
    {
        if (!serviceId.HasValue)
            return BadRequest(new { code = Codes.NotFound, message = "serviceId is required." });

        var (ok, data, code) = await _svc.GetByServiceIdAsync(serviceId.Value, ct);

        if (!ok || data is null || !data.Any())
            return NotFound(new { code = Codes.NotFound, message = "Bookings not found." });

        return Ok(new { code, data });
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookingUpsertDto body, CancellationToken ct)
    {
        var (ok, id, code, message) = await _svc.CreateAsync(body, ct);
        if (!ok) return BadRequest(new { code, message });
        return CreatedAtAction(nameof(GetById), new { id }, new { code = Codes.Ok, data = new { id } });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookingUpsertDto body, CancellationToken ct)
    {
        var (ok, code, message) = await _svc.UpdateAsync(id, body, ct);
        if (!ok && code == Codes.NotFound) return NotFound(new { code, message });
        if (!ok) return BadRequest(new { code, message });
        return Ok(new { code = Codes.Ok });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var (ok, code, message) = await _svc.DeleteAsync(id, ct);
        if (!ok && code == Codes.NotFound) return NotFound(new { code, message });
        if (!ok) return BadRequest(new { code, message });
        return Ok(new { code = Codes.Ok });
    }

    [HttpPost("availability")]
    public async Task<IActionResult> GetAvailability([FromBody] Availability body, CancellationToken ct)
    {
        var (ok, data, message) = await _svc.GetAvailability(body, ct);

        if (!ok || data is null)
        {
            return NotFound(new
            {
                code = Codes.NotFound,
                message = message ?? "Availability not found."
            });
        }

        return Ok(new { code = Codes.Ok, data });
    }

}