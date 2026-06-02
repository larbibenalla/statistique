using ElectoralStats.Data;
using ElectoralStats.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectoralStats.Controllers;

public class StatsController : Controller
{
    private readonly StatsService _stats;
    private readonly AppDbContext _db;
    public StatsController(StatsService stats, AppDbContext db) { _stats = stats; _db = db; }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Data() => Json(await _stats.ComputeAsync());

    [HttpPost]
    public async Task<IActionResult> Reset()
    {
        _db.Voters.RemoveRange(_db.Voters);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
