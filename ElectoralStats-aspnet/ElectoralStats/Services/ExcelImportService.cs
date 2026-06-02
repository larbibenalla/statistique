using ClosedXML.Excel;
using ElectoralStats.Data;
using ElectoralStats.Models;

namespace ElectoralStats.Services;

public class ImportResult
{
    public string FileName { get; set; } = "";
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public string? Error { get; set; }
}

public class ExcelImportService
{
    private readonly AppDbContext _db;
    public ExcelImportService(AppDbContext db) { _db = db; }

    // Map of normalized Arabic header => property setter
    private static readonly Dictionary<string, Action<VoterRecord, string?>> Map = new()
    {
        ["رقم الوثيقة"] = (r,v) => r.DocumentNumber = v,
        ["وثيقة التعريف"] = (r,v) => r.IdDocument = v,
        ["مكان الاقامة"] = (r,v) => r.ResidencePlace = v,
        ["جماعة الاقامة"] = (r,v) => r.ResidenceCommune = v,
        ["العنوان"] = (r,v) => r.Address = v,
        ["نوع العمل"] = (r,v) => r.WorkType = v,
        ["المهنة"] = (r,v) => r.Profession = v,
        ["المستوى الدراسي"] = (r,v) => r.EducationLevel = v,
        ["عدد الأطفال"] = (r,v) => r.ChildrenCount = int.TryParse(v, out var n) ? n : null,
        ["الحالة العائلية"] = (r,v) => r.MaritalStatus = v,
        ["جنس"] = (r,v) => r.Gender = v,
        ["تاريخ الازدياد"] = (r,v) => r.BirthDate = ParseDate(v),
        ["مكان الازدياد آخر"] = (r,v) => r.BirthPlace = v,
        ["جماعة الازدياد"] = (r,v) => r.BirthCommune = v,
        ["الاسم العائلي بالفرنسية"] = (r,v) => r.FamilyNameFr = v,
        ["الاسم الشخصي بالفرنسية"] = (r,v) => r.FirstNameFr = v,
        ["اسم الأب و الأم"] = (r,v) => r.ParentsName = v,
        ["الاسم العائلي"] = (r,v) => r.FamilyNameAr = v,
        ["الاسم الشخصي"] = (r,v) => r.FirstNameAr = v,
        ["علاقة التسجيل بالجماعة"] = (r,v) => r.RegistrationRelation = v,
        ["الرقم الترتيبي"] = (r,v) => r.OrderNumber = v,
        ["الدائرة الانتخابية"] = (r,v) => r.Circumscription = v,
        ["الجماعة"] = (r,v) => r.Commune = v,
        ["رمز المستعمل"] = (r,v) => r.UserCode = v,
        ["الدائرة الانتخابية سابقا"] = (r,v) => r.PreviousCircumscription = v,
        ["الجماعة سابقا"] = (r,v) => r.PreviousCommune = v,
        ["تاريخ التسجيل"] = (r,v) => r.RegistrationDate = ParseDate(v),
        ["تاريخ التعديل"] = (r,v) => r.ModificationDate = ParseDate(v),
        ["رمز الناخب"] = (r,v) => r.VoterCode = v,
        ["رقم مكتب التصويت"] = (r,v) => r.PollingStationNumber = v,
        ["اسم مكتب التصويت"] = (r,v) => r.PollingStationName = v,
        ["سبب التشطيب"] = (r,v) => r.RadiationReason = v,
    };

    private static DateTime? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, out var d)) return d;
        foreach (var f in new[] { "dd/MM/yyyy", "yyyy-MM-dd", "d/M/yyyy", "dd-MM-yyyy" })
            if (DateTime.TryParseExact(s, f, null, System.Globalization.DateTimeStyles.None, out d)) return d;
        return null;
    }

    public async Task<ImportResult> ImportAsync(Stream stream, string fileName)
    {
        var result = new ImportResult { FileName = fileName };
        try
        {
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            var headerRow = ws.FirstRowUsed();
            if (headerRow == null) { result.Error = "Empty sheet"; return result; }

            var headers = headerRow.Cells().ToDictionary(
                c => c.Address.ColumnNumber,
                c => (c.GetString() ?? "").Trim());

            bool hasRadiation = headers.Values.Any(h => h.Contains("سبب التشطيب"));
            var kind = hasRadiation ? RecordKind.Radiation : RecordKind.Inscription;

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var rec = new VoterRecord { Kind = kind, SourceFile = fileName };
                bool anySet = false;
                foreach (var (col, header) in headers)
                {
                    if (!Map.TryGetValue(header, out var setter)) continue;
                    var val = row.Cell(col).GetString()?.Trim();
                    if (!string.IsNullOrEmpty(val)) { setter(rec, val); anySet = true; }
                }
                if (!anySet) { result.Skipped++; continue; }
                if (kind == RecordKind.Inscription && rec.ModificationDate.HasValue
                    && rec.RegistrationDate.HasValue
                    && rec.ModificationDate > rec.RegistrationDate)
                    rec.Kind = RecordKind.Modification;

                _db.Voters.Add(rec);
                result.Imported++;
            }
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) { result.Error = ex.Message; }
        return result;
    }
}
