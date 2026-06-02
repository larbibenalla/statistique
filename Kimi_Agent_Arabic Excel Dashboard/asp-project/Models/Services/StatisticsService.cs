using System;
using System.Collections.Generic;
using System.Linq;

namespace VoterRegistrationDashboard.Models.Services
{
    /// <summary>
    /// خدمة حساب الإحصائيات
    /// Statistics calculation service with aggregation logic
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        /// <summary>
        /// Calculates all dashboard statistics from the inscription list
        /// حساب جميع إحصائيات لوحة التحكم
        /// </summary>
        public DashboardViewModel CalculateStatistics(
            List<Inscription> inscriptions, 
            FilterViewModel filters,
            int page = 1, 
            int pageSize = 25)
        {
            // Apply filters first
            var filtered = ApplyFilters(inscriptions, filters);

            var model = new DashboardViewModel
            {
                TotalInscriptions = filtered.Count,
                CurrentPage = page,
                PageSize = pageSize,
                Filters = filters
            };

            // Summary counts
            model.ApprovedCount = filtered.Count(i => 
                i.RequestStatus.Contains("مقبول") || i.RequestStatus.Contains("مقبولة"));
            
            model.RejectedCount = filtered.Count(i => 
                i.RequestStatus.Contains("مرفوض") || i.RequestStatus.Contains("مرفوضة"));
            
            model.PendingCount = filtered.Count(i => 
                i.RequestStatus.Contains("قيد") || i.RequestStatus.Contains("معلق"));

            model.ApprovalRate = model.TotalInscriptions > 0 
                ? Math.Round((double)model.ApprovedCount / model.TotalInscriptions * 100, 1) 
                : 0;

            // Daily trends - Number of inscriptions per day
            model.DailyTrends = filtered
                .Where(i => i.RequestDate.HasValue)
                .GroupBy(i => i.RequestDate.Value.Date)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key.ToString("yyyy-MM-dd"),
                    Value = g.Count()
                })
                .OrderBy(d => d.Label)
                .ToList();

            // Province breakdown (using residence province)
            model.ProvinceBreakdown = filtered
                .Where(i => !string.IsNullOrWhiteSpace(i.ResidenceProvince))
                .GroupBy(i => i.ResidenceProvince)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(d => d.Value)
                .Take(15)
                .ToList();

            // Commune breakdown
            model.CommuneBreakdown = filtered
                .Where(i => !string.IsNullOrWhiteSpace(i.ResidenceCommune))
                .GroupBy(i => i.ResidenceCommune)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(d => d.Value)
                .Take(15)
                .ToList();

            // Gender breakdown
            model.GenderBreakdown = filtered
                .Where(i => !string.IsNullOrWhiteSpace(i.Gender))
                .GroupBy(i => i.Gender)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count(),
                    Color = g.Key.Contains("ذكر") ? "#3B82F6" : "#EC4899"
                })
                .OrderByDescending(d => d.Value)
                .ToList();

            // Request type breakdown
            model.RequestTypeBreakdown = filtered
                .Where(i => !string.IsNullOrWhiteSpace(i.RequestType))
                .GroupBy(i => i.RequestType)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(d => d.Value)
                .ToList();

            // Marital status breakdown
            model.MaritalStatusBreakdown = filtered
                .Where(i => !string.IsNullOrWhiteSpace(i.MaritalStatus))
                .GroupBy(i => i.MaritalStatus)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(d => d.Value)
                .ToList();

            // Education level breakdown
            model.EducationBreakdown = filtered
                .Where(i => !string.IsNullOrWhiteSpace(i.EducationLevel))
                .GroupBy(i => i.EducationLevel)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(d => d.Value)
                .ToList();

            // Status breakdown
            model.StatusBreakdown = filtered
                .Where(i => !string.IsNullOrWhiteSpace(i.RequestStatus))
                .GroupBy(i => i.RequestStatus)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count(),
                    Color = GetStatusColor(g.Key)
                })
                .OrderByDescending(d => d.Value)
                .ToList();

            // Rejection reasons
            model.RejectionReasons = filtered
                .Where(i => !string.IsNullOrWhiteSpace(i.RejectionReason) && 
                            (i.RequestStatus.Contains("مرفوض") || i.RequestStatus.Contains("مرفوضة")))
                .GroupBy(i => i.RejectionReason)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(d => d.Value)
                .ToList();

            // Filter options for dropdowns
            model.ProvinceOptions = inscriptions
                .Select(i => i.ResidenceProvince)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            model.CommuneOptions = inscriptions
                .Select(i => i.ResidenceCommune)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            model.GenderOptions = inscriptions
                .Select(i => i.Gender)
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct()
                .OrderBy(g => g)
                .ToList();

            model.RequestTypeOptions = inscriptions
                .Select(i => i.RequestType)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            model.StatusOptions = inscriptions
                .Select(i => i.RequestStatus)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            // Paged data for table
            model.Inscriptions = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return model;
        }

        /// <summary>
        /// Applies filters to the inscription list
        /// تطبيق عوامل التصفية على قائمة التسجيلات
        /// </summary>
        public List<Inscription> ApplyFilters(List<Inscription> inscriptions, FilterViewModel filters)
        {
            if (filters == null) return inscriptions;

            var query = inscriptions.AsEnumerable();

            // Date range filter
            if (filters.DateFrom.HasValue)
                query = query.Where(i => i.RequestDate >= filters.DateFrom.Value);

            if (filters.DateTo.HasValue)
                query = query.Where(i => i.RequestDate <= filters.DateTo.Value);

            // Province filter
            if (!string.IsNullOrWhiteSpace(filters.Province))
                query = query.Where(i => i.ResidenceProvince == filters.Province);

            // Commune filter
            if (!string.IsNullOrWhiteSpace(filters.Commune))
                query = query.Where(i => i.ResidenceCommune == filters.Commune);

            // Gender filter
            if (!string.IsNullOrWhiteSpace(filters.Gender))
                query = query.Where(i => i.Gender == filters.Gender);

            // Request type filter
            if (!string.IsNullOrWhiteSpace(filters.RequestType))
                query = query.Where(i => i.RequestType == filters.RequestType);

            // Status filter
            if (!string.IsNullOrWhiteSpace(filters.Status))
                query = query.Where(i => i.RequestStatus == filters.Status);

            // Search term
            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
            {
                var term = filters.SearchTerm.Trim();
                query = query.Where(i =>
                    (i.FirstName?.Contains(term) ?? false) ||
                    (i.LastName?.Contains(term) ?? false) ||
                    (i.NationalId?.Contains(term) ?? false) ||
                    (i.RequestNumber?.Contains(term) ?? false) ||
                    (i.ResidenceAddress?.Contains(term) ?? false) ||
                    (i.ResidenceCity?.Contains(term) ?? false));
            }

            return query.ToList();
        }

        /// <summary>
        /// Gets a color for a given status
        /// الحصول على لون لكل وضعية
        /// </summary>
        private string GetStatusColor(string status)
        {
            if (status.Contains("مقبول")) return "#10B981"; // Green
            if (status.Contains("مرفوض")) return "#EF4444"; // Red
            if (status.Contains("قيد") || status.Contains("معلق")) return "#F59E0B"; // Yellow
            return "#6B7280"; // Gray
        }
    }
}
