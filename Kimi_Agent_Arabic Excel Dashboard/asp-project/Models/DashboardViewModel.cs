using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoterRegistrationDashboard.Models
{
    /// <summary>
    /// نموذج عرض لوحة التحكم الرئيسية
    /// Main dashboard view model with all statistics
    /// </summary>
    public class DashboardViewModel
    {
        // Summary cards
        public int TotalInscriptions { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int PendingCount { get; set; }
        public double ApprovalRate { get; set; }

        // Chart data
        public List<ChartDataPoint> DailyTrends { get; set; } = new();
        public List<ChartDataPoint> ProvinceBreakdown { get; set; } = new();
        public List<ChartDataPoint> CommuneBreakdown { get; set; } = new();
        public List<ChartDataPoint> GenderBreakdown { get; set; } = new();
        public List<ChartDataPoint> RequestTypeBreakdown { get; set; } = new();
        public List<ChartDataPoint> MaritalStatusBreakdown { get; set; } = new();
        public List<ChartDataPoint> EducationBreakdown { get; set; } = new();
        public List<ChartDataPoint> StatusBreakdown { get; set; } = new();
        public List<ChartDataPoint> RejectionReasons { get; set; } = new();

        // Filters
        public FilterViewModel Filters { get; set; } = new();

        // Filter options for dropdowns
        public List<string> ProvinceOptions { get; set; } = new();
        public List<string> CommuneOptions { get; set; } = new();
        public List<string> GenderOptions { get; set; } = new();
        public List<string> RequestTypeOptions { get; set; } = new();
        public List<string> StatusOptions { get; set; } = new();

        // Paged data table
        public List<Inscription> Inscriptions { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalPages => (int)Math.Ceiling(TotalInscriptions / (double)PageSize);
    }

    /// <summary>
    /// نقطة بيانات للرسوم البيانية
    /// Single data point for charts
    /// </summary>
    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public string? Color { get; set; }
    }

    /// <summary>
    /// نموذج عوامل التصفية
    /// Filter model for dashboard
    /// </summary>
    public class FilterViewModel
    {
        [Display(Name = "من تاريخ")]
        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [Display(Name = "إلى تاريخ")]
        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }

        [Display(Name = "عمالة أو إقليم")]
        public string? Province { get; set; }

        [Display(Name = "جماعة")]
        public string? Commune { get; set; }

        [Display(Name = "الجنس")]
        public string? Gender { get; set; }

        [Display(Name = "نوع الطلب")]
        public string? RequestType { get; set; }

        [Display(Name = "وضعية الطلب")]
        public string? Status { get; set; }

        [Display(Name = "بحث")]
        public string? SearchTerm { get; set; }
    }

    /// <summary>
    /// نموذج رفع الملف
    /// Upload view model
    /// </summary>
    public class UploadViewModel
    {
        [Required(ErrorMessage = "يرجى اختيار ملف Excel")]
        [Display(Name = "ملف Excel")]
        public IFormFile? ExcelFile { get; set; }

        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RecordsImported { get; set; }
    }
}
