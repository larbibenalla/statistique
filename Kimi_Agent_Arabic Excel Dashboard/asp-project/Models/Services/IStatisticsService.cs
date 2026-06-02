using System.Collections.Generic;
using System.Linq;

namespace VoterRegistrationDashboard.Models.Services
{
    /// <summary>
    /// واجهة خدمة الإحصائيات
    /// Interface for statistics calculation service
    /// </summary>
    public interface IStatisticsService
    {
        DashboardViewModel CalculateStatistics(
            List<Inscription> inscriptions, 
            FilterViewModel filters,
            int page = 1, 
            int pageSize = 25);

        List<Inscription> ApplyFilters(List<Inscription> inscriptions, FilterViewModel filters);
    }
}
