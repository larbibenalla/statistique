using ElectoralStats.Hubs;
using ElectoralStats.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ElectoralStats.Controllers;

public class UploadController : Controller
{
    private readonly ExcelImportService _importer;
    private readonly StatsService _stats;
    private readonly IHubContext<StatsHub> _hub;

    public UploadController(ExcelImportService importer, StatsService stats, IHubContext<StatsHub> hub)
    { _importer = importer; _stats = stats; _hub = hub; }

    public IActionResult Index() => View();

    [HttpPost, RequestSizeLimit(500_000_000)]
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        var results = new List<ImportResult>();
        foreach (var f in files)
        {
            if (f.Length == 0) continue;
            using var s = f.OpenReadStream();
            var r = await _importer.ImportAsync(s, f.FileName);
            results.Add(r);
            await _hub.Clients.All.SendAsync("FileImported", r);
        }
        var snap = await _stats.ComputeAsync();
        await _hub.Clients.All.SendAsync("StatsUpdated", snap);
        return Json(new { results, stats = snap });
    }
}
