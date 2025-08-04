using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CEApi.Data;
using CEApi.Models;

namespace CEApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly MsSqlDatabaseContext _context;

        public StatisticsController(MsSqlDatabaseContext context)
        {
            _context = context;
        }

        // GET: api/Statistics
        [HttpGet]
        public async Task<ActionResult<List<Statistics>>> GetStatistics()
        {
            var statistics = await _context.Statistics.ToListAsync();

            if (statistics == null)
            {
                return NotFound();
            }

            // Calculate total products and users
            foreach (var statistic in statistics)
            {
                try
                {
                    statistic.TotalProducts = await _context.Products.CountAsync();
                    statistic.TotalUsers = await _context.UserAccounts.CountAsync();
                } catch (Exception)
                {
                    statistic.TotalProducts = -1;
                    statistic.TotalUsers = -1;
                    Console.Error.WriteLine("GetStatistics: Error calculating total products or users. Returning -1 for both.");
                }
            }

            return statistics;
        }

        // GET: api/Statistics/{search}
        [HttpGet("{search}")]
        public async Task<ActionResult<Statistics>> GetStatistics(string search)
        {
            search = search.Trim().ToLower();
            var statistics = await _context.Statistics.FirstOrDefaultAsync(stat => stat.Id == search || stat.Name.ToLower() == search);
            if (statistics == null)
            {
                return NotFound();
            }
            // Calculate total products and users
            try
            {
                statistics.TotalProducts = await _context.Products.CountAsync();
                statistics.TotalUsers = await _context.UserAccounts.CountAsync();
            }
            catch (Exception)
            {
                statistics.TotalProducts = -1;
                statistics.TotalUsers = -1;
                Console.Error.WriteLine("GetStatistics: Error calculating total products or users. Returning -1 for both.");
            }
            return statistics;
        }

        // POST: api/Statistics
        [HttpPost]
        public async Task<ActionResult<Statistics>> CreateStatistics(Statistics statistics)
        {
            statistics.Id = Guid.NewGuid().ToString();

            if (StatisticsExists(null, statistics.Name))
            {
                return Conflict();
            }

            _context.Statistics.Add(statistics);
            try
            {
                await _context.SaveChangesAsync();

            }
            catch (DbUpdateException)
            {
                if (StatisticsExists(statistics.Id, statistics.Name))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction(nameof(GetStatistics), new { search = statistics.Id }, statistics);
        }

        // DELETE: /api/Statistics/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStatistics(string id)
        {
            var stat = await _context.Statistics.FindAsync(id);
            if (stat == null)
            {
                return NotFound();
            }

            _context.Remove(stat);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool StatisticsExists(string? id, string? name)
        {
            return _context.Statistics.Any(e => e.Id == id || e.Name.ToLower() == name!.ToLower());
        }
    }
}
