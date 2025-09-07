using CMS.Data;
using CMS.Models.Metrics;
using CMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CMS.Controllers.Metrics
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly ApiContext _db;
        private readonly ITenantProvider _tenant;
        public MetricsController(ApiContext db, ITenantProvider tenant)
        { _db = db; _tenant = tenant; }

        public record TrackDto(string path, string? referrer);

        [HttpPost("track")]
        [AllowAnonymous]
        public async Task<IActionResult> Track([FromBody] TrackDto dto)
        {
            var tid = _tenant.TenantId;
            if (tid is null || string.IsNullOrWhiteSpace(dto.path)) return NoContent();

            var day = DateTime.UtcNow.Date;

            // Check if the row already exists
            var existingEntity = await _db.PageViewDailies
                .SingleOrDefaultAsync(x => x.TenantId == tid && x.Path == dto.path && x.DayUtc == day);

            if (existingEntity != null)
            {
                // Row exists, increment the count
                existingEntity.Count += 1;
            }
            else
            {
                // Row does not exist, create a new one
                var entity = new PageViewDaily
                {
                    TenantId = tid,
                    Path = dto.path,
                    DayUtc = day,
                    Count = 1
                };

                _db.PageViewDailies.Add(entity);
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }





        // Read-only summary for your dashboard
        [HttpGet("summary")]
        [Authorize]
        public async Task<IActionResult> Summary([FromQuery] int days = 7)
        {
            var tid = _tenant.TenantId;
            if (tid is null) return BadRequest();

            var since = DateTime.UtcNow.Date.AddDays(-days + 1);
            var data = await _db.PageViewDailies
                .Where(x => x.TenantId == tid && x.DayUtc >= since)
                .OrderBy(x => x.DayUtc).ThenBy(x => x.Path)
                .Select(x => new { x.DayUtc, x.Path, x.Count })
                .ToListAsync();

            return Ok(data);
        }
        public record WeeklyBucketDto(DateTime WeekStartUtc, string? Path, int Count);

        [HttpGet("summary-weekly")]
        [Authorize] // your dashboard auth
        public async Task<IActionResult> SummaryWeekly(
            [FromQuery] int weeks = 8,
            [FromQuery] string? path = null,
            [FromQuery] bool groupByPath = false)
        {
            var tid = _tenant.TenantId;
            if (tid is null) return BadRequest();

            // Start from Monday of the first week in range (UTC)
            var today = DateTime.UtcNow.Date;
            var startGuess = today.AddDays(-(7 * weeks) + 1);
            var firstWeek = ISOWeek.GetWeekOfYear(startGuess);
            var firstYear = ISOWeek.GetYear(startGuess);
            var weekStart = ISOWeek.ToDateTime(firstYear, firstWeek, DayOfWeek.Monday).Date;

            var q = _db.PageViewDailies
                .Where(x => x.TenantId == tid && x.DayUtc >= weekStart);

            if (!string.IsNullOrWhiteSpace(path))
                q = q.Where(x => x.Path == path);

            // Group weekly in memory (safe for small ranges; keeps SQL portable)
            var rows = await q.ToListAsync();

            var data = rows
                .GroupBy(x => new
                {
                    Year = ISOWeek.GetYear(x.DayUtc),
                    Week = ISOWeek.GetWeekOfYear(x.DayUtc),
                    Path = groupByPath ? x.Path : null
                })
                .Select(g => new WeeklyBucketDto(
                    WeekStartUtc: ISOWeek.ToDateTime(g.Key.Year, g.Key.Week, DayOfWeek.Monday).Date,
                    Path: g.Key.Path,
                    Count: g.Sum(r => r.Count)))
                .OrderBy(b => b.WeekStartUtc)
                .ThenBy(b => b.Path) // nulls last
                .ToList();

            return Ok(data);
        }
    }
}
