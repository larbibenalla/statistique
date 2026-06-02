using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VoterRegistrationDashboard.Models.Services
{
    /// <summary>
    /// واجهة خدمة قراءة ملفات Excel
    /// Interface for Excel parsing service
    /// </summary>
    public interface IExcelParserService
    {
        /// <summary>
        /// قراءة ملف Excel وتحويله إلى قائمة تسجيلات
        /// Parse uploaded Excel file into inscription records
        /// </summary>
        Task<List<Inscription>> ParseExcelFileAsync(IFormFile file);

        /// <summary>
        /// التحقق من صحة ملف Excel
        /// Validate Excel file format
        /// </summary>
        bool IsValidExcelFile(IFormFile file);
    }
}
