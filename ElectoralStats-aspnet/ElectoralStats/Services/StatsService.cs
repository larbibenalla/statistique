using ElectoralStats.Data;
using ElectoralStats.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectoralStats.Services;

public record StatsSnapshot(
    int Total,
    int Inscriptions,
    int Radiations,
    int Modifications,
    Dictionary<string, int> PerDay,
    Dictionary<string, int> PerCommune,
    Dictionary<string, int> PerCircumscription,
    Dictionary<string, int> PerGender,
    Dictionary<string, int> PerType,
    Dictionary<string, int> PerAgeBucket);

public class StatsService
{
    private readonly AppDbContext _db;
    public StatsService(AppDbContext db) { _db = db; }

    public async Task<StatsSnapshot> ComputeAsync()
    {
        var all = await _db.Voters.AsNoTracking().ToListAsync();

        var perDay = all
            .Where(v => v.RegistrationDate.HasValue)
            .GroupBy(v => v.RegistrationDate!.Value.Date)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

        var perCommune = all.Where(v => !string.IsNullOrEmpty(v.Commune))
            .GroupBy(v => v.Commune!).OrderByDescending(g => g.Count()).Take(15)
            .ToDictionary(g => g.Key, g => g.Count());

        var perCircum = all.Where(v => !string.IsNullOrEmpty(v.Circumscription))
            .GroupBy(v => v.Circumscription!).OrderByDescending(g => g.Count()).Take(15)
            .ToDictionary(g => g.Key, g => g.Count());

        var perGender = all.Where(v => !string.IsNullOrEmpty(v.Gender))
            .GroupBy(v => v.Gender!).ToDictionary(g => g.Key, g => g.Count());

        var perType = new Dictionary<string, int>
        {
            ["Inscription"] = all.Count(v => v.Kind == RecordKind.Inscription),
            ["Radiation"]   = all.Count(v => v.Kind == RecordKind.Radiation),
            ["Modification"]= all.Count(v => v.Kind == RecordKind.Modification),
        };

        var today = DateTime.Today;
        var perAge = all.Where(v => v.BirthDate.HasValue).GroupBy(v =>
        {
            var age = today.Year - v.BirthDate!.Value.Year;
            if (v.BirthDate > today.AddYears(-age)) age--;
            return age switch
            {
                < 25 => "18-24",
                < 35 => "25-34",
                < 45 => "35-44",
                < 60 => "45-59",
                _    => "60+"
            };
        }).OrderBy(g => g.Key).ToDictionary(g => g.Key, g => g.Count());

        return new StatsSnapshot(
            all.Count,
            perType["Inscription"],
            perType["Radiation"],
            perType["Modification"],
            perDay, perCommune, perCircum, perGender, perType, perAge);
    }
}
