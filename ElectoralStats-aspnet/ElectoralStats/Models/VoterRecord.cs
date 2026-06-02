namespace ElectoralStats.Models;

public enum RecordKind { Inscription = 0, Radiation = 1, Modification = 2 }

public class VoterRecord
{
    public int Id { get; set; }
    public RecordKind Kind { get; set; }
    public string? SourceFile { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    // Mapped from Arabic headers
    public string? VoterCode { get; set; }              // رمز الناخب
    public string? DocumentNumber { get; set; }          // رقم الوثيقة
    public string? IdDocument { get; set; }              // وثيقة التعريف
    public string? FamilyNameAr { get; set; }            // الاسم العائلي
    public string? FirstNameAr { get; set; }             // الاسم الشخصي
    public string? FamilyNameFr { get; set; }            // الاسم العائلي بالفرنسية
    public string? FirstNameFr { get; set; }             // الاسم الشخصي بالفرنسية
    public string? ParentsName { get; set; }             // اسم الأب و الأم
    public string? Gender { get; set; }                  // جنس
    public DateTime? BirthDate { get; set; }             // تاريخ الازدياد
    public string? BirthPlace { get; set; }              // مكان الازدياد آخر
    public string? BirthCommune { get; set; }            // جماعة الازدياد
    public string? ResidencePlace { get; set; }          // مكان الاقامة
    public string? ResidenceCommune { get; set; }        // جماعة الاقامة
    public string? Address { get; set; }                 // العنوان
    public string? WorkType { get; set; }                // نوع العمل
    public string? Profession { get; set; }              // المهنة
    public string? EducationLevel { get; set; }          // المستوى الدراسي
    public int? ChildrenCount { get; set; }              // عدد الأطفال
    public string? MaritalStatus { get; set; }           // الحالة العائلية
    public string? RegistrationRelation { get; set; }    // علاقة التسجيل بالجماعة
    public string? OrderNumber { get; set; }             // الرقم الترتيبي
    public string? Circumscription { get; set; }         // الدائرة الانتخابية
    public string? Commune { get; set; }                 // الجماعة
    public string? PreviousCircumscription { get; set; } // الدائرة الانتخابية سابقا
    public string? PreviousCommune { get; set; }         // الجماعة سابقا
    public string? UserCode { get; set; }                // رمز المستعمل
    public DateTime? RegistrationDate { get; set; }      // تاريخ التسجيل
    public DateTime? ModificationDate { get; set; }      // تاريخ التعديل
    public string? PollingStationNumber { get; set; }    // رقم مكتب التصويت
    public string? PollingStationName { get; set; }      // اسم مكتب التصويت
    public string? RadiationReason { get; set; }         // سبب التشطيب
}
