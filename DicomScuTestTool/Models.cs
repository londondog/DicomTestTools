namespace DicomScuTestTool;

public class DicomFileEntry
{
    public string FilePath { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string PatientID { get; set; } = "";
    public string AccessionNumber { get; set; } = "";
    public string StudyDate { get; set; } = "";
    public string StudyTime { get; set; } = "";
    public string Modality { get; set; } = "";
    public string DOB { get; set; } = "";
    public string Sex { get; set; } = "";
    public string StudyDescription { get; set; } = "";
    public string StudyInstanceUID { get; set; } = "";
    public string SeriesInstanceUID { get; set; } = "";
    public string SOPInstanceUID { get; set; } = "";
    public ListViewItem? ListViewItem { get; set; }
}

public class DemographicsSnapshot
{
    public string PatientName { get; set; } = "";
    public string PatientID { get; set; } = "";
    public string DOB { get; set; } = "";
    public string Sex { get; set; } = "";
    public string AccessionNumber { get; set; } = "";
    public string StudyDate { get; set; } = "";
    public string StudyTime { get; set; } = "";
    public string StudyDescription { get; set; } = "";
    public string Modality { get; set; } = "";
}

public class ProcedureSnapshot
{
    public string StudyUID { get; set; } = "";
    public string SeriesUID { get; set; } = "";
    public bool GenerateNewStudyUID { get; set; }
    public bool GenerateNewSeriesUID { get; set; }
    public bool GenerateNewSOPUID { get; set; }
}

public class AppSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 11112;
    public string CallingAET { get; set; } = "SCU_TEST";
    public string CalledAET { get; set; } = "ANY-SCP";
    public bool OverrideDemographics { get; set; } = true;
    public bool OverrideProcedure { get; set; } = false;
    public string LookupConnectionString { get; set; } = "";
    public int LookupRangeDays { get; set; } = 7;
    public bool LookupTrustServerCertificate { get; set; } = true;
}

public class PatientLookupResult
{
    public string PatientId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
    public string Sex { get; set; } = "";
    public int Age { get; set; }
}

public class OrderLookupResult
{
    public string PatientId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string AccessionNumber { get; set; } = "";
    public int VisitId { get; set; }
    public string ScheduledProcedureStepId { get; set; } = "";
    public string ProcedureCode { get; set; } = "";
    public DateTime? StartTime { get; set; }
    public string WorkflowStatus { get; set; } = "";
    public string Gender { get; set; } = "";
}
