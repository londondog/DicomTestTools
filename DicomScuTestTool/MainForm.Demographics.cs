using System.Drawing;

namespace DicomScuTestTool;

public partial class MainForm
{
    private static readonly string[] FirstNames =
    [
        "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael",
        "Linda", "William", "Barbara", "David", "Elizabeth", "Richard", "Susan",
        "Joseph", "Jessica", "Thomas", "Sarah", "Charles", "Karen", "Christopher",
        "Lisa", "Daniel", "Nancy", "Matthew", "Betty", "Anthony", "Margaret", "Mark", "Sandra"
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller",
        "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson",
        "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez",
        "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson"
    ];

    private static readonly string[] StudyDescriptions =
    [
        "CT Chest Abdomen Pelvis", "MRI Brain Without Contrast", "X-Ray Chest PA and Lateral",
        "Ultrasound Abdomen", "CT Head Without Contrast", "MRI Spine Lumbar",
        "Nuclear Medicine Bone Scan", "CT Angiography Chest", "Echo Cardiogram", "PET CT Whole Body"
    ];

    private void BtnLoadFromFile_Click(object? sender, EventArgs e)
    {
        if (_lvFiles.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select a file in the list first.", "No file selected",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var lvi = _lvFiles.SelectedItems[0];
        var entry = _files.FirstOrDefault(f => f.ListViewItem == lvi);
        if (entry != null)
            PopulateDemographicsFromEntry(entry);
    }

    internal void PopulateDemographicsFromEntry(DicomFileEntry entry)
    {
        _txtPatientName.Text = entry.PatientName;
        _txtPatientID.Text = entry.PatientID;
        _txtDOB.Text = entry.DOB;
        _cmbSex.SelectedItem = _cmbSex.Items.Contains(entry.Sex) ? entry.Sex : "";
        _txtAccession.Text = entry.AccessionNumber;
        _txtStudyDate.Text = entry.StudyDate;
        _txtStudyTime.Text = entry.StudyTime;
        _txtStudyDesc.Text = entry.StudyDescription;

        // Populate procedure fields too
        _txtStudyUID.Text = entry.StudyInstanceUID;
        _txtSeriesUID.Text = entry.SeriesInstanceUID;
        if (_cmbModality.Items.Contains(entry.Modality))
            _cmbModality.SelectedItem = entry.Modality;
    }

    private void CopyDemographicsTemplate()
    {
        var template = $"""
            Patient Name  : {_txtPatientName.Text}
            Patient ID    : {_txtPatientID.Text}
            Date of Birth : {_txtDOB.Text}
            Sex           : {_cmbSex.SelectedItem}
            Accession     : {_txtAccession.Text}
            Study Date    : {_txtStudyDate.Text}
            Study Time    : {_txtStudyTime.Text}
            Study Desc    : {_txtStudyDesc.Text}
            Study UID     : {_txtStudyUID.Text}
            Series UID    : {_txtSeriesUID.Text}
            """;
        Clipboard.SetText(template);
        Log("[INFO] Template copied to clipboard — edit and paste back with the Paste button.", Color.Cyan);
    }

    private void PasteDemographics()
    {
        var text = Clipboard.GetText();
        if (string.IsNullOrWhiteSpace(text))
        {
            Log("[WARN] Clipboard is empty.", Color.Orange);
            return;
        }

        var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            string key;
            string val;

            var tabParts = trimmed.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tabParts.Length >= 3)
            {
                // Supports common DICOM dump format: (gggg,eeee) <tab> Name <tab> Value
                key = tabParts[1];
                val = tabParts[2];
            }
            else if (tabParts.Length == 2)
            {
                key = tabParts[0];
                val = tabParts[1];
            }
            else
            {
                var colon = trimmed.IndexOf(':');
                if (colon < 0) continue;
                key = trimmed[..colon];
                val = trimmed[(colon + 1)..];
            }

            key = key.Trim().Replace(" ", "").Replace("-", "");
            val = val.Trim();
            parsed[key] = val;
        }

        string Get(params string[] keys)
        {
            foreach (var k in keys)
                if (parsed.TryGetValue(k, out var v)) return v;
            return "";
        }

        var name = Get("PatientName", "Name");
        if (!string.IsNullOrEmpty(name)) _txtPatientName.Text = name;

        var pid = Get("PatientID", "ID", "MRN");
        if (!string.IsNullOrEmpty(pid)) _txtPatientID.Text = pid;

        var dob = Get("DateofBirth", "DOB", "BirthDate");
        if (!string.IsNullOrEmpty(dob)) _txtDOB.Text = dob;

        var sex = Get("Sex", "Gender");
        if (!string.IsNullOrEmpty(sex) && _cmbSex.Items.Contains(sex.ToUpper()))
            _cmbSex.SelectedItem = sex.ToUpper();

        var acc = Get("Accession", "AccessionNumber", "AccessionNo");
        if (!string.IsNullOrEmpty(acc)) _txtAccession.Text = acc;

        var sd = Get("StudyDate", "Date");
        if (!string.IsNullOrEmpty(sd)) _txtStudyDate.Text = sd;

        var st = Get("StudyTime", "Time");
        if (!string.IsNullOrEmpty(st)) _txtStudyTime.Text = st;

        var desc = Get("StudyDesc", "StudyDescription", "Description");
        if (!string.IsNullOrEmpty(desc)) _txtStudyDesc.Text = desc;

        var studyUID = Get("StudyUID", "StudyInstanceUID");
        if (!string.IsNullOrEmpty(studyUID)) _txtStudyUID.Text = studyUID;

        var seriesUID = Get("SeriesUID", "SeriesInstanceUID");
        if (!string.IsNullOrEmpty(seriesUID)) _txtSeriesUID.Text = seriesUID;

        Log("[INFO] Demographics pasted from clipboard.", Color.LightGreen);
    }

    private void RandomizeDemographics()
    {
        var rng = new Random();

        string firstName = FirstNames[rng.Next(FirstNames.Length)];
        string lastName = LastNames[rng.Next(LastNames.Length)];
        _txtPatientName.Text = $"{lastName}^{firstName}";
        _txtPatientID.Text = rng.Next(10000000, 99999999).ToString();

        var dob = DateTime.Today.AddDays(-rng.Next(18 * 365, 85 * 365));
        _txtDOB.Text = dob.ToString("yyyyMMdd");

        _cmbSex.SelectedItem = rng.Next(2) == 0 ? "M" : "F";
        _txtAccession.Text = GenerateAccession(rng);
        _txtStudyDate.Text = DateTime.Today.ToString("yyyyMMdd");
        _txtStudyTime.Text = DateTime.Now.ToString("HHmm");
        _txtStudyDesc.Text = StudyDescriptions[rng.Next(StudyDescriptions.Length)];

        Log($"[INFO] Randomized: {_txtPatientName.Text} | ID: {_txtPatientID.Text} | DOB: {_txtDOB.Text}", Color.LightGreen);
    }

    private static string GenerateAccession(Random rng)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 8).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }

    private void SetDemographicsEnabled(bool enabled)
    {
        foreach (Control c in new Control[]
        {
            _txtPatientName, _txtPatientID, _txtDOB, _cmbSex,
            _txtAccession, _txtStudyDate, _txtStudyTime, _txtStudyDesc,
            _btnRandomize, _btnLoadFromFile
        })
        {
            c.Enabled = enabled;
        }
    }

    internal void SetProcedureEnabled(bool enabled)
    {
        foreach (Control c in new Control[]
        {
            _cmbModality, _txtProcedureDesc,
            _txtStudyUID, _txtSeriesUID,
            _chkNewStudyUID, _chkNewSeriesUID, _chkNewSOPUID
        })
        {
            c.Enabled = enabled;
        }
    }
}
