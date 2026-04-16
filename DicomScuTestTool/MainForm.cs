using System.Drawing;

namespace DicomScuTestTool;

public partial class MainForm : Form
{
    // ── Connection ──────────────────────────────────────────────
    private TextBox _txtHost = null!;
    private NumericUpDown _numPort = null!;
    private TextBox _txtCallingAET = null!;
    private TextBox _txtCalledAET = null!;
    private Button _btnEcho = null!;
    private Label _lblEchoStatus = null!;

    // ── File list ───────────────────────────────────────────────
    private ListView _lvFiles = null!;
    private Button _btnAddFiles = null!;
    private Button _btnAddFolder = null!;
    private Button _btnRemove = null!;
    private Button _btnClearAll = null!;
    private Label _lblFileCount = null!;

    // ── Demographics ────────────────────────────────────────────
    private CheckBox _chkOverride = null!;
    private TextBox _txtPatientName = null!;
    private TextBox _txtPatientID = null!;
    private TextBox _txtDOB = null!;
    private ComboBox _cmbSex = null!;
    private TextBox _txtAccession = null!;
    private TextBox _txtStudyDate = null!;
    private TextBox _txtStudyTime = null!;
    private TextBox _txtStudyDesc = null!;
    private Button _btnRandomize = null!;
    private Button _btnLoadFromFile = null!;
    private Button _btnCopyTemplate = null!;
    private Button _btnPasteAll = null!;

    // ── Procedure ───────────────────────────────────────────────
    private CheckBox _chkOverrideProcedure = null!;
    private TextBox _txtStudyUID = null!;
    private TextBox _txtSeriesUID = null!;
    private ComboBox _cmbModality = null!;
    private CheckBox _chkNewStudyUID = null!;
    private CheckBox _chkNewSeriesUID = null!;
    private CheckBox _chkNewSOPUID = null!;

    // ── Lookup (Patient/Orders) ───────────────────────────────
    private TextBox _txtLookupConnectionString = null!;
    private CheckBox _chkLookupTrustServerCert = null!;
    private NumericUpDown _numLookupDays = null!;
    private TextBox _txtLookupPatientId = null!;
    private TextBox _txtLookupPatientName = null!;
    private TextBox _txtLookupAccession = null!;
    private Button _btnLookupTestConnection = null!;
    private Button _btnLookupPatients = null!;
    private Button _btnLookupOrders = null!;
    private Button _btnLookupProcedures = null!;
    private Button _btnApplySelectedPatient = null!;
    private Button _btnApplySelectedOrder = null!;
    private Button _btnApplySelectedBoth = null!;
    private CheckBox _chkApplyNewStudyUID = null!;
    private DataGridView _dgvLookupPatients = null!;
    private DataGridView _dgvLookupOrders = null!;

    // ── Actions ─────────────────────────────────────────────────
    private Button _btnSendAll = null!;
    private Button _btnSendSelected = null!;
    private Button _btnCancelSend = null!;
    private ProgressBar _progressBar = null!;
    private Label _lblProgress = null!;

    // ── Log ─────────────────────────────────────────────────────
    private RichTextBox _rtbLog = null!;

    // ── State ───────────────────────────────────────────────────
    private readonly List<DicomFileEntry> _files = new();
    private CancellationTokenSource? _cts;
    private readonly string _settingsFile;
    private bool _sending;

    public MainForm()
    {
        _settingsFile = Path.Combine(
            Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
            "dicomscu_settings.json");

        Text = "DICOM SCU Test Tool  v1.0.1  —  by George Hutchings";
        Size = new Size(1160, 820);
        MinimumSize = new Size(960, 700);
        StartPosition = FormStartPosition.CenterScreen;

        BuildUI();
        LoadSettings();
        WireEvents();
    }

    private void WireEvents()
    {
        _btnAddFiles.Click += BtnAddFiles_Click;
        _btnAddFolder.Click += BtnAddFolder_Click;
        _btnRemove.Click += BtnRemove_Click;
        _btnClearAll.Click += (_, _) => ClearFiles();
        _btnEcho.Click += BtnEcho_Click;
        _btnRandomize.Click += (_, _) => RandomizeDemographics();
        _btnCopyTemplate.Click += (_, _) => CopyDemographicsTemplate();
        _btnPasteAll.Click += (_, _) => PasteDemographics();
        _btnLoadFromFile.Click += BtnLoadFromFile_Click;
        _btnLookupTestConnection.Click += async (_, _) => await TestLookupConnectionAsync();
        _btnLookupPatients.Click += async (_, _) => await SearchPatientsAsync();
        _btnLookupOrders.Click += async (_, _) => await SearchOrdersAsync();
        _btnLookupProcedures.Click += async (_, _) => await SearchProceduresAsync();
        _chkLookupTrustServerCert.CheckedChanged += (_, _) => UpdateLookupTrustServerCertificateInConnectionString();
        _btnApplySelectedPatient.Click += (_, _) => ApplySelectedPatientToDemographics();
        _btnApplySelectedOrder.Click += (_, _) => ApplySelectedOrderToDemographics();
        _btnApplySelectedBoth.Click += (_, _) =>
        {
            ApplySelectedPatientToDemographics();
            ApplySelectedOrderToDemographics();
        };
        _btnSendAll.Click += async (_, _) => await SendAsync(sendAll: true);
        _btnSendSelected.Click += async (_, _) => await SendAsync(sendAll: false);
        _btnCancelSend.Click += (_, _) => _cts?.Cancel();
        _lvFiles.SelectedIndexChanged += (_, _) => UpdateButtonStates();
        _chkOverride.CheckedChanged += (_, _) => SetDemographicsEnabled(_chkOverride.Checked);
        _chkOverrideProcedure.CheckedChanged += (_, _) => SetProcedureEnabled(_chkOverrideProcedure.Checked);
        FormClosing += (_, _) => { SaveSettings(); _cts?.Cancel(); Environment.Exit(0); };
    }

    internal void UpdateButtonStates()
    {
        _btnSendAll.Enabled = _files.Count > 0 && !_sending;
        _btnSendSelected.Enabled = _lvFiles.SelectedItems.Count > 0 && !_sending;
        _btnRemove.Enabled = _lvFiles.SelectedItems.Count > 0;
        _btnCancelSend.Enabled = _sending;
    }

    internal void SetFileStatus(DicomFileEntry entry, string status, Color color)
    {
        if (entry.ListViewItem == null) return;
        if (InvokeRequired) { Invoke(() => SetFileStatus(entry, status, color)); return; }
        entry.ListViewItem.SubItems[7].Text = status;
        entry.ListViewItem.ForeColor = color;
    }

    internal void Log(string message, Color? color = null)
    {
        if (InvokeRequired) { Invoke(() => Log(message, color)); return; }

        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.SelectionLength = 0;
        _rtbLog.SelectionColor = Color.Gray;
        _rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] ");
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.SelectionColor = color ?? Color.LightGray;
        _rtbLog.AppendText(message + "\n");
        _rtbLog.ScrollToCaret();
    }

    private static Label MakeLabel(string text) => new Label
    {
        Text = text,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Panel MakeSpacer(int width) => new Panel { Width = width, Height = 1 };

    private void ShowAboutDialog()
    {
        MessageBox.Show(
            "DICOM SCU Test Tool\nVersion 1\nMade by George Hutchings",
            "About",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
