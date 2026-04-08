namespace DicomCFindTestTool;

public class StudyResult
{
    public string PatientName { get; set; } = "";
    public string PatientID { get; set; } = "";
    public string PatientDOB { get; set; } = "";
    public string PatientSex { get; set; } = "";
    public string StudyDate { get; set; } = "";
    public string StudyTime { get; set; } = "";
    public string AccessionNumber { get; set; } = "";
    public string Modality { get; set; } = "";
    public string StudyDescription { get; set; } = "";
    public string ReferringPhysician { get; set; } = "";
    public string StudyID { get; set; } = "";
    public string StudyInstanceUID { get; set; } = "";
    public string NumberOfSeries { get; set; } = "";
    public string NumberOfInstances { get; set; } = "";
}

public class SeriesResult
{
    public string StudyInstanceUID { get; set; } = "";
    public string SeriesNumber { get; set; } = "";
    public string Modality { get; set; } = "";
    public string SeriesDescription { get; set; } = "";
    public string SeriesDate { get; set; } = "";
    public string SeriesTime { get; set; } = "";
    public string BodyPartExamined { get; set; } = "";
    public string NumberOfInstances { get; set; } = "";
    public string SeriesInstanceUID { get; set; } = "";
}

public class InstanceResult
{
    public string StudyInstanceUID { get; set; } = "";
    public string SeriesInstanceUID { get; set; } = "";
    public string InstanceNumber { get; set; } = "";
    public string SOPClassUID { get; set; } = "";
    public string SOPClassName { get; set; } = "";
    public string SOPInstanceUID { get; set; } = "";
    public string Rows { get; set; } = "";
    public string Columns { get; set; } = "";
    public string ContentDate { get; set; } = "";
}

public class WorklistResult
{
    public string PatientName { get; set; } = "";
    public string PatientID { get; set; } = "";
    public string PatientDOB { get; set; } = "";
    public string PatientSex { get; set; } = "";
    public string AccessionNumber { get; set; } = "";
    public string StudyInstanceUID { get; set; } = "";
    public string RequestedProcedureDescription { get; set; } = "";
    public string RequestedProcedureID { get; set; } = "";
    public string ScheduledDate { get; set; } = "";
    public string ScheduledTime { get; set; } = "";
    public string Modality { get; set; } = "";
    public string StationAETitle { get; set; } = "";
    public string StationName { get; set; } = "";
    public string PerformingPhysician { get; set; } = "";
    public string ProcedureStepID { get; set; } = "";
    public string ProcedureStepDescription { get; set; } = "";
    public string ProtocolCode { get; set; } = "";
    public string ProtocolCodeMeaning { get; set; } = "";
}

public class AppSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 11112;
    public string CallingAET { get; set; } = "CFIND_TEST";
    public string CalledAET { get; set; } = "ANY-SCP";
    public bool UsePatientRoot { get; set; } = false;
}
