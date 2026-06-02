using System;
using System.ComponentModel.DataAnnotations;

namespace VoterRegistrationDashboard.Models
{
    /// <summary>
    /// نموذج بيانات التسجيل الانتخابي
    /// Represents a single voter registration record from the Excel file
    /// </summary>
    public class Inscription
    {
        // معلومات مكان التسجيل (Registration Location Info)
        [Display(Name = "جماعة أو مقاطعة التسجيل")]
        public string RegistrationCommune { get; set; } = string.Empty;

        [Display(Name = "الدائرة الإنتخابية للتسجيل")]
        public string RegistrationDistrict { get; set; } = string.Empty;

        [Display(Name = "مكتب التصويت للتسجيل")]
        public string RegistrationOffice { get; set; } = string.Empty;

        [Display(Name = "الملحقة الإدارية للتسجيل")]
        public string RegistrationAnnex { get; set; } = string.Empty;

        // معلومات صاحب الطلب (Applicant Info)
        [Display(Name = "رقم الطلب")]
        public string RequestNumber { get; set; } = string.Empty;

        [Display(Name = "تاريخ الطلب")]
        public DateTime? RequestDate { get; set; }

        [Display(Name = "نوع الطلب")]
        public string RequestType { get; set; } = string.Empty;

        [Display(Name = "فئة المواطن")]
        public string CitizenCategory { get; set; } = string.Empty;

        [Display(Name = "رقم البطاقة الوطنية")]
        public string NationalId { get; set; } = string.Empty;

        [Display(Name = "الإسم العائلي")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "الإسم الشخصي")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "الجنس")]
        public string Gender { get; set; } = string.Empty;

        // معلومات مكان الإزدياد (Birth Info)
        [Display(Name = "تاريخ الإزدياد")]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "بلد الإزدياد")]
        public string BirthCountry { get; set; } = string.Empty;

        [Display(Name = "مدينة الإزدياد")]
        public string BirthCity { get; set; } = string.Empty;

        [Display(Name = "عمالة أو إقليم الإزدياد")]
        public string BirthProvince { get; set; } = string.Empty;

        [Display(Name = "جماعة الإزدياد")]
        public string BirthCommune { get; set; } = string.Empty;

        // معلومات مكان السكن (Residence Info)
        [Display(Name = "بلد السكن")]
        public string ResidenceCountry { get; set; } = string.Empty;

        [Display(Name = "مدينة السكن")]
        public string ResidenceCity { get; set; } = string.Empty;

        [Display(Name = "عمالة أو إقليم السكن")]
        public string ResidenceProvince { get; set; } = string.Empty;

        [Display(Name = "جماعة السكن")]
        public string ResidenceCommune { get; set; } = string.Empty;

        [Display(Name = "عنوان السكن")]
        public string ResidenceAddress { get; set; } = string.Empty;

        // معلومات إضافية (Additional Info)
        [Display(Name = "الحالة العائلية")]
        public string MaritalStatus { get; set; } = string.Empty;

        [Display(Name = "المستوى الدراسي")]
        public string EducationLevel { get; set; } = string.Empty;

        // الوضعية (Status)
        [Display(Name = "وضعية الطلب")]
        public string RequestStatus { get; set; } = string.Empty;

        [Display(Name = "سبب الوضعية")]
        public string StatusReason { get; set; } = string.Empty;

        [Display(Name = "تاريخ الوضعية")]
        public DateTime? StatusDate { get; set; }

        [Display(Name = "سبب رفض اللجنة")]
        public string RejectionReason { get; set; } = string.Empty;

        // Helper property for age calculation
        public int? Age
        {
            get
            {
                if (!BirthDate.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        // Helper for full name
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
