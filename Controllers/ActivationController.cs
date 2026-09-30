using Microsoft.AspNetCore.Mvc;
using ActivationAPI.Models;
using System.Collections.Concurrent;

namespace ActivationAPI.Controllers;

[ApiController]
[Route("api/activation")]
public class ActivationController : ControllerBase
{
    // In-memory storage (Azure restart pe reset hoga — neeche explain karunga)
    private static ConcurrentDictionary<string, ActivationRecord> _store = new();

    // ── GENERATE CODE (sirf tu use karega) ──────────────────
    [HttpPost("generate")]
    public IActionResult Generate([FromBody] GenerateRequest req)
    {
        // Secret key check — sirf tera tool yeh call karega
        var secret = Request.Headers["X-Admin-Secret"].ToString();
        if (secret != "ABHIJAT_SECRET_2024")
            return Unauthorized(new { message = "Not authorized" });

        var allowed = new[] { 3, 7, 10, 15, 30, 90, 180, 360 };
        if (!allowed.Contains(req.DurationDays))
            return BadRequest(new { message = "Invalid duration" });

        // Code format: 20260327-ABCD1234-EFGH5678
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var rand1 = GenerateRandomPart(4) + GenerateRandomDigits(4);
        var rand2 = GenerateRandomPart(4) + GenerateRandomDigits(4);
        var code = $"{datePart}-{rand1}-{rand2}";

        var record = new ActivationRecord {
            ActivationCode = code,
            MachineKey = req.MachineKey,
            CreatedAt = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(req.DurationDays),
            DurationDays = req.DurationDays,
            IsUsed = false
        };

        _store[code] = record;
        return Ok(new { success = true, activationCode = code, 
                        expiryDate = record.ExpiryDate, 
                        durationDays = req.DurationDays });
    }

    // ── ACTIVATE (extension call karti hai) ─────────────────
    [HttpPost("activate")]
    public IActionResult Activate([FromBody] ActivateRequest req)
    {
        if (!_store.TryGetValue(req.ActivationCode.ToUpper().Trim(), out var record))
            return Ok(new { success = false, message = "Invalid activation code" });

        if (record.IsUsed && record.MachineKey != req.MachineKey)
            return Ok(new { success = false, message = "Code already used on another machine" });

        if (DateTime.UtcNow > record.ExpiryDate)
            return Ok(new { success = false, message = "Activation code expired" });

        // Mark as used & bind to this machine
        record.IsUsed = true;
        record.MachineKey = req.MachineKey;
        _store[req.ActivationCode] = record;

        return Ok(new { 
            success = true, 
            expiryDate = record.ExpiryDate,
            durationDays = record.DurationDays,
            message = "Activated successfully"
        });
    }

    // ── VALIDATE (extension har baar check karti hai) ────────
    [HttpPost("validate")]
    public IActionResult Validate([FromBody] ValidateRequest req)
    {
        if (!_store.TryGetValue(req.ActivationCode.ToUpper().Trim(), out var record))
            return Ok(new { success = false, message = "Invalid code" });

        if (record.MachineKey != req.MachineKey)
            return Ok(new { success = false, message = "Machine mismatch" });

        if (DateTime.UtcNow > record.ExpiryDate)
            return Ok(new { success = false, message = "License expired" });

        return Ok(new { 
            success = true, 
            expiryDate = record.ExpiryDate,
            daysRemaining = (int)(record.ExpiryDate - DateTime.UtcNow).TotalDays
        });
    }

    // ── HELPERS ──────────────────────────────────────────────
    private static string GenerateRandomPart(int len) {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var r = new Random();
        return new string(Enumerable.Range(0,len).Select(_ => chars[r.Next(chars.Length)]).ToArray());
    }

    private static string GenerateRandomDigits(int len) {
        var r = new Random();
        return new string(Enumerable.Range(0,len).Select(_ => (char)('0'+r.Next(10))).ToArray());
    }
}