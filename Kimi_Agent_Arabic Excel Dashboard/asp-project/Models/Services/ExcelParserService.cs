using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VoterRegistrationDashboard.Models.Services
{
    /// <summary>
    /// خدمة قراءة ملفات Excel باستخدام EPPlus
    /// Excel parsing service using EPPlus - no Microsoft Office required
    /// </summary>
    public class ExcelParserService : IExcelParserService
    {
        // Arabic column header to property mapping
        // ربط رؤوس الأعمدة العربية بالخصائص
        private static readonly Dictionary<string, string> HeaderMappings = new()
        {
            // معلومات مكان التسجيل
            { "جماعة أو مقاطعة التسجيل", nameof(Inscription.RegistrationCommune) },
            { "الدائرة الإنتخابية للتسجيل", nameof(Inscription.RegistrationDistrict) },
            { "مكتب التصويت للتسجيل", nameof(Inscription.RegistrationOffice) },
            { "الملحقة الإدارية للتسجيل", nameof(Inscription.RegistrationAnnex) },

            // معلومات صاحب الطلب
            { "رقم الطلب", nameof(Inscription.RequestNumber) },
            { "تاريخ الطلب", nameof(Inscription.RequestDate) },
            { "نوع الطلب", nameof(Inscription.RequestType) },
            { "فئة المواطن", nameof(Inscription.CitizenCategory) },
            { "رقم البطاقة الوطنية", nameof(Inscription.NationalId) },
            { "الإسم العائلي", nameof(Inscription.LastName) },
            { "الإسم الشخصي", nameof(Inscription.FirstName) },
            { "الجنس", nameof(Inscription.Gender) },

            // معلومات مكان الإزدياد
            { "تاريخ الإزدياد", nameof(Inscription.BirthDate) },
            { "بلد الإزدياد", nameof(Inscription.BirthCountry) },
            { "مدينة الإزدياد", nameof(Inscription.BirthCity) },
            { "عمالة أو إقليم الإزدياد", nameof(Inscription.BirthProvince) },
            { "جماعة الإزدياد", nameof(Inscription.BirthCommune) },

            // معلومات مكان السكن
            { "بلد السكن", nameof(Inscription.ResidenceCountry) },
            { "مدينة السكن", nameof(Inscription.ResidenceCity) },
            { "عمالة أو إقليم السكن", nameof(Inscription.ResidenceProvince) },
            { "جماعة السكن", nameof(Inscription.ResidenceCommune) },
            { "عنوان السكن", nameof(Inscription.ResidenceAddress) },

            // معلومات إضافية
            { "الحالةالعائلية", nameof(Inscription.MaritalStatus) },
            { "الحالة العائلية", nameof(Inscription.MaritalStatus) },
            { "المستوى الدراسي", nameof(Inscription.EducationLevel) },

            // الوضعية
            { "وضعية الطلب", nameof(Inscription.RequestStatus) },
            { "سبب الوضعية", nameof(Inscription.StatusReason) },
            { "تاريخ الوضعية", nameof(Inscription.StatusDate) },
            { "سبب رفض اللجنة", nameof(Inscription.RejectionReason) },

            // Variations with different spacing
            { "الحالة العائلية ", nameof(Inscription.MaritalStatus) },
            { " الحالة العائلية", nameof(Inscription.MaritalStatus) },
        };

        private readonly ILogger<ExcelParserService> _logger;

        public ExcelParserService(ILogger<ExcelParserService> logger)
        {
            _logger = logger;
            // Set EPPlus license context (required for non-commercial use)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Validates that the uploaded file is a valid Excel file
        /// التحقق من أن الملف هو ملف Excel صالح
        /// </summary>
        public bool IsValidExcelFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return extension == ".xlsx" || extension == ".xls";
        }

        /// <summary>
        /// Parses the Excel file and returns a list of Inscription records
        /// قراءة ملف Excel وإرجاع قائمة التسجيلات
        /// </summary>
        public async Task<List<Inscription>> ParseExcelFileAsync(IFormFile file)
        {
            var inscriptions = new List<Inscription>();

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    _logger.LogWarning("No worksheet found in Excel file");
                    return inscriptions;
                }

                // Find header row - look for row containing known Arabic headers
                // البحث عن صف الرأس الذي يحتوي على العناوين العربية المعروفة
                int headerRow = FindHeaderRow(worksheet);

                if (headerRow == 0)
                {
                    _logger.LogWarning("Could not find header row with Arabic headers");
                    return inscriptions;
                }

                // Build column index mapping based on header row
                // بناء خريطة أعمدة بناءً على صف الرأس
                var columnMapping = BuildColumnMapping(worksheet, headerRow);

                if (columnMapping.Count == 0)
                {
                    _logger.LogWarning("No recognizable columns found in header row {HeaderRow}", headerRow);
                    return inscriptions;
                }

                _logger.LogInformation("Found {ColumnCount} mapped columns at row {HeaderRow}", 
                    columnMapping.Count, headerRow);

                // Parse data rows
                for (int row = headerRow + 1; row <= worksheet.Dimension.End.Row; row++)
                {
                    var inscription = ParseRow(worksheet, row, columnMapping);
                    
                    // Only add if we have at least a request number or national ID
                    if (!string.IsNullOrWhiteSpace(inscription.RequestNumber) || 
                        !string.IsNullOrWhiteSpace(inscription.NationalId))
                    {
                        inscriptions.Add(inscription);
                    }
                }

                _logger.LogInformation("Successfully parsed {RecordCount} records from Excel file", 
                    inscriptions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Excel file");
                throw;
            }

            return inscriptions;
        }

        /// <summary>
        /// Finds the row containing Arabic headers
        /// البحث عن الصف الذي يحتوي على الرؤوس العربية
        /// </summary>
        private int FindHeaderRow(ExcelWorksheet worksheet)
        {
            if (worksheet.Dimension == null) return 0;

            // Search first 5 rows for known Arabic headers
            for (int row = 1; row <= Math.Min(5, worksheet.Dimension.End.Row); row++)
            {
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Text?.Trim() ?? string.Empty;
                    
                    // Check if this cell contains any known header
                    if (HeaderMappings.Keys.Any(h => 
                        cellValue.Contains(h, StringComparison.OrdinalIgnoreCase) ||
                        h.Contains(cellValue, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Verify it's actually a header row by checking multiple known headers
                        int matchCount = 0;
                        for (int checkCol = 1; checkCol <= worksheet.Dimension.End.Column; checkCol++)
                        {
                            var checkValue = worksheet.Cells[row, checkCol].Text?.Trim() ?? string.Empty;
                            if (HeaderMappings.ContainsKey(checkValue))
                                matchCount++;
                        }

                        if (matchCount >= 3) // At least 3 recognized headers
                        {
                            _logger.LogDebug("Found header row {Row} with {MatchCount} matching headers", 
                                row, matchCount);
                            return row;
                        }
                    }
                }
            }

            // Fallback: assume row 2 (common for files with grouped headers)
            return worksheet.Dimension.End.Row >= 2 ? 2 : 1;
        }

        /// <summary>
        /// Builds a mapping of column index to property name
        /// بناء خريطة ربط الأعمدة بالخصائص
        /// </summary>
        private Dictionary<int, string> BuildColumnMapping(ExcelWorksheet worksheet, int headerRow)
        {
            var mapping = new Dictionary<int, string>();

            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var headerText = worksheet.Cells[headerRow, col].Text?.Trim() ?? string.Empty;

                // Try exact match first
                if (HeaderMappings.TryGetValue(headerText, out var propertyName))
                {
                    mapping[col] = propertyName;
                    continue;
                }

                // Try fuzzy match - remove extra spaces and normalize
                var normalizedHeader = NormalizeArabicText(headerText);
                foreach (var kvp in HeaderMappings)
                {
                    var normalizedKey = NormalizeArabicText(kvp.Key);
                    if (normalizedHeader == normalizedKey ||
                        normalizedHeader.Contains(normalizedKey) ||
                        normalizedKey.Contains(normalizedHeader))
                    {
                        mapping[col] = kvp.Value;
                        break;
                    }
                }
            }

            return mapping;
        }

        /// <summary>
        /// Normalizes Arabic text for comparison
        /// توحيد النص العربي للمقارنة
        /// </summary>
        private string NormalizeArabicText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            return text.Trim()
                .Replace("  ", " ") // Double spaces
                .Replace("\u0640", "") // Tatweel
                .Replace("\u200B", "") // Zero-width space
                .Replace("\uFEFF", ""); // BOM
        }

        /// <summary>
        /// Parses a single row into an Inscription object
        /// تحويل صف واحد إلى كائن تسجيل
        /// </summary>
        private Inscription ParseRow(ExcelWorksheet worksheet, int row, Dictionary<int, string> columnMapping)
        {
            var inscription = new Inscription();

            foreach (var kvp in columnMapping)
            {
                int col = kvp.Key;
                string propertyName = kvp.Value;
                string cellValue = worksheet.Cells[row, col].Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(cellValue)) continue;

                try
                {
                    switch (propertyName)
                    {
                        case nameof(Inscription.RegistrationCommune):
                            inscription.RegistrationCommune = cellValue;
                            break;
                        case nameof(Inscription.RegistrationDistrict):
                            inscription.RegistrationDistrict = cellValue;
                            break;
                        case nameof(Inscription.RegistrationOffice):
                            inscription.RegistrationOffice = cellValue;
                            break;
                        case nameof(Inscription.RegistrationAnnex):
                            inscription.RegistrationAnnex = cellValue;
                            break;
                        case nameof(Inscription.RequestNumber):
                            inscription.RequestNumber = cellValue;
                            break;
                        case nameof(Inscription.RequestDate):
                            inscription.RequestDate = ParseDate(cellValue);
                            break;
                        case nameof(Inscription.RequestType):
                            inscription.RequestType = cellValue;
                            break;
                        case nameof(Inscription.CitizenCategory):
                            inscription.CitizenCategory = cellValue;
                            break;
                        case nameof(Inscription.NationalId):
                            inscription.NationalId = cellValue;
                            break;
                        case nameof(Inscription.LastName):
                            inscription.LastName = cellValue;
                            break;
                        case nameof(Inscription.FirstName):
                            inscription.FirstName = cellValue;
                            break;
                        case nameof(Inscription.Gender):
                            inscription.Gender = cellValue;
                            break;
                        case nameof(Inscription.BirthDate):
                            inscription.BirthDate = ParseDate(cellValue);
                            break;
                        case nameof(Inscription.BirthCountry):
                            inscription.BirthCountry = cellValue;
                            break;
                        case nameof(Inscription.BirthCity):
                            inscription.BirthCity = cellValue;
                            break;
                        case nameof(Inscription.BirthProvince):
                            inscription.BirthProvince = cellValue;
                            break;
                        case nameof(Inscription.BirthCommune):
                            inscription.BirthCommune = cellValue;
                            break;
                        case nameof(Inscription.ResidenceCountry):
                            inscription.ResidenceCountry = cellValue;
                            break;
                        case nameof(Inscription.ResidenceCity):
                            inscription.ResidenceCity = cellValue;
                            break;
                        case nameof(Inscription.ResidenceProvince):
                            inscription.ResidenceProvince = cellValue;
                            break;
                        case nameof(Inscription.ResidenceCommune):
                            inscription.ResidenceCommune = cellValue;
                            break;
                        case nameof(Inscription.ResidenceAddress):
                            inscription.ResidenceAddress = cellValue;
                            break;
                        case nameof(Inscription.MaritalStatus):
                            inscription.MaritalStatus = cellValue;
                            break;
                        case nameof(Inscription.EducationLevel):
                            inscription.EducationLevel = cellValue;
                            break;
                        case nameof(Inscription.RequestStatus):
                            inscription.RequestStatus = cellValue;
                            break;
                        case nameof(Inscription.StatusReason):
                            inscription.StatusReason = cellValue;
                            break;
                        case nameof(Inscription.StatusDate):
                            inscription.StatusDate = ParseDate(cellValue);
                            break;
                        case nameof(Inscription.RejectionReason):
                            inscription.RejectionReason = cellValue;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Error parsing cell at row {Row}, col {Col}: {Error}", 
                        row, col, ex.Message);
                }
            }

            return inscription;
        }

        /// <summary>
        /// Parses a date string with multiple format support
        /// قراءة التاريخ بعدة تنسيقات
        /// </summary>
        private DateTime? ParseDate(string dateValue)
        {
            if (string.IsNullOrWhiteSpace(dateValue)) return null;

            // If it's already a DateTime in Excel
            if (double.TryParse(dateValue, out double oaDate))
            {
                try
                {
                    return DateTime.FromOADate(oaDate);
                }
                catch { /* fallback to string parsing */ }
            }

            // Try various date formats common in Arabic/Moroccan context
            string[] formats = new[]
            {
                "yyyy-MM-dd",
                "dd/MM/yyyy",
                "dd-MM-yyyy",
                "yyyy/MM/dd",
                "dd.MM.yyyy",
                "yyyy.MM.dd",
                "dd MMM yyyy",
                "dd MMMM yyyy",
                "d/M/yyyy",
                "d-M-yyyy",
            };

            if (DateTime.TryParseExact(dateValue, formats, 
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result;
            }

            // Last resort: general parse
            if (DateTime.TryParse(dateValue, out result))
            {
                return result;
            }

            return null;
        }
    }
}
