using System.Text.Json;

namespace DicomCFindTestTool;

public partial class MainForm : Form
{
    // ── Connection ──────────────────────────────────────────────
    private TextBox _txtHost = null!;
    private NumericUpDown _numPort = null!;
    private TextBox _txtCallingAET = null!;
    private TextBox _txtCalledAET = null!;
    private Button _btnEcho = null!;
    private Label _lblEchoStatus = null!;

    // ── Tab control ─────────────────────────────────────────────
    private TabControl _tabs = null!;

    // ── Study / Patient tab ─────────────────────────────────────
    private TextBox _txtStudyPatientID = null!;
    private TextBox _txtStudyPatientName = null!;
    private TextBox _txtStudyDOB = null!;
    private ComboBox _cmbStudySex = null!;
    private CheckBox _chkStudyDateFrom = null!;
    private DateTimePicker _dtpStudyDateFrom = null!;
    private CheckBox _chkStudyDateTo = null!;
    private DateTimePicker _dtpStudyDateTo = null!;
    private TextBox _txtStudyAccession = null!;
    private TextBox _txtStudyUID = null!;
    private ComboBox _cmbStudyModality = null!;
    private TextBox _txtStudyDesc = null!;
    private TextBox _txtStudyReferringPhysician = null!;
    private RadioButton _radStudyRoot = null!;
    private RadioButton _radPatientRoot = null!;
    private Button _btnStudyQuery = null!;
    private Button _btnStudyClear = null!;
    private Button _btnStudyExport = null!;
    private Label _lblStudyCount = null!;
    private DataGridView _dgvStudy = null!;

    // ── Series tab ───────────────────────────────────────────────
    private TextBox _txtSeriesStudyUID = null!;
    private TextBox _txtSeriesSeriesUID = null!;
    private ComboBox _cmbSeriesModality = null!;
    private TextBox _txtSeriesDesc = null!;
    private Button _btnSeriesQuery = null!;
    private Button _btnSeriesClear = null!;
    private Button _btnSeriesExport = null!;
    private Label _lblSeriesCount = null!;
    private DataGridView _dgvSeries = null!;

    // ── Instance tab ─────────────────────────────────────────────
    private TextBox _txtInstStudyUID = null!;
    private TextBox _txtInstSeriesUID = null!;
    private Button _btnInstQuery = null!;
    private Button _btnInstClear = null!;
    private Button _btnInstExport = null!;
    private Label _lblInstCount = null!;
    private DataGridView _dgvInst = null!;

    // ── Worklist (MWL) tab ───────────────────────────────────────
    private TextBox _txtWlPatientID = null!;
    private TextBox _txtWlPatientName = null!;
    private CheckBox _chkWlToday = null!;
    private CheckBox _chkWlDateFrom = null!;
    private DateTimePicker _dtpWlDateFrom = null!;
    private CheckBox _chkWlDateTo = null!;
    private DateTimePicker _dtpWlDateTo = null!;
    private ComboBox _cmbWlModality = null!;
    private TextBox _txtWlStationAE = null!;
    private TextBox _txtWlPhysician = null!;
    private TextBox _txtWlAccession = null!;
    private TextBox _txtWlRequestedProcID = null!;
    private ComboBox _cmbWlProcedureType = null!;
    private TextBox _txtWlProtocolCode = null!;
    private Button _btnWlQuery = null!;
    private Button _btnWlClear = null!;
    private Button _btnWlExport = null!;
    private Label _lblWlCount = null!;
    private DataGridView _dgvWorklist = null!;

    // ── Log ─────────────────────────────────────────────────────
    private RichTextBox _rtbLog = null!;

    // ── State ───────────────────────────────────────────────────
    private CancellationTokenSource? _cts;
    private bool _querying;
    private readonly string _settingsFile;

    public MainForm()
    {
        _settingsFile = Path.Combine(
            Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
            "dicomcfind_settings.json");

        Text = "DICOM C-FIND Test Tool  v1.0  —  by George Hutchings";
        Size = new Size(1200, 880);
        MinimumSize = new Size(1000, 720);
        StartPosition = FormStartPosition.CenterScreen;

        BuildUI();
        LoadSettings();
        WireEvents();
    }

    private void WireEvents()
    {
        _btnEcho.Click += async (_, _) => await RunEchoAsync();

        _btnStudyQuery.Click += async (_, _) => await RunStudyQueryAsync();
        _btnStudyClear.Click += (_, _) => { _dgvStudy.Rows.Clear(); _lblStudyCount.Text = "0 result(s)"; };
        _btnStudyExport.Click += (_, _) => ExportToCsv(_dgvStudy, "study_results");

        _btnSeriesQuery.Click += async (_, _) => await RunSeriesQueryAsync();
        _btnSeriesClear.Click += (_, _) => { _dgvSeries.Rows.Clear(); _lblSeriesCount.Text = "0 result(s)"; };
        _btnSeriesExport.Click += (_, _) => ExportToCsv(_dgvSeries, "series_results");

        _btnInstQuery.Click += async (_, _) => await RunInstanceQueryAsync();
        _btnInstClear.Click += (_, _) => { _dgvInst.Rows.Clear(); _lblInstCount.Text = "0 result(s)"; };
        _btnInstExport.Click += (_, _) => ExportToCsv(_dgvInst, "instance_results");

        _btnWlQuery.Click += async (_, _) => await RunWorklistQueryAsync();
        _btnWlClear.Click += (_, _) => { _dgvWorklist.Rows.Clear(); _lblWlCount.Text = "0 result(s)"; };
        _btnWlExport.Click += (_, _) => ExportToCsv(_dgvWorklist, "worklist_results");

        _chkStudyDateFrom.CheckedChanged += (_, _) => _dtpStudyDateFrom.Enabled = _chkStudyDateFrom.Checked;
        _chkStudyDateTo.CheckedChanged += (_, _) => _dtpStudyDateTo.Enabled = _chkStudyDateTo.Checked;
        _chkWlToday.CheckedChanged += (_, _) =>
        {
            if (_chkWlToday.Checked)
            {
                _chkWlDateFrom.Checked = true;
                _dtpWlDateFrom.Value = DateTime.Today;
                _chkWlDateTo.Checked = true;
                _dtpWlDateTo.Value = DateTime.Today;
                _chkWlDateFrom.Enabled = false;
                _chkWlDateTo.Enabled = false;
                _dtpWlDateFrom.Enabled = false;
                _dtpWlDateTo.Enabled = false;
            }
            else
            {
                _chkWlDateFrom.Enabled = true;
                _chkWlDateTo.Enabled = true;
                _dtpWlDateFrom.Enabled = _chkWlDateFrom.Checked;
                _dtpWlDateTo.Enabled = _chkWlDateTo.Checked;
            }
        };
        _chkWlDateFrom.CheckedChanged += (_, _) => _dtpWlDateFrom.Enabled = _chkWlDateFrom.Checked;
        _chkWlDateTo.CheckedChanged += (_, _) => _dtpWlDateTo.Enabled = _chkWlDateTo.Checked;

        // Context menus on study grid: drill down into series / instances
        _dgvStudy.CellMouseDown += DgvStudy_CellMouseDown;
        _dgvSeries.CellMouseDown += DgvSeries_CellMouseDown;

        FormClosing += (_, _) => { SaveSettings(); _cts?.Cancel(); Environment.Exit(0); };
    }

    // ── Settings ─────────────────────────────────────────────────

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsFile)) return;
            var json = File.ReadAllText(_settingsFile);
            var s = JsonSerializer.Deserialize<AppSettings>(json);
            if (s == null) return;
            _txtHost.Text = s.Host;
            _numPort.Value = Math.Clamp(s.Port, 1, 65535);
            _txtCallingAET.Text = s.CallingAET;
            _txtCalledAET.Text = s.CalledAET;
            _radPatientRoot.Checked = s.UsePatientRoot;
            _radStudyRoot.Checked = !s.UsePatientRoot;
        }
        catch { /* ignore */ }
    }

    private void SaveSettings()
    {
        try
        {
            var s = new AppSettings
            {
                Host = _txtHost.Text.Trim(),
                Port = (int)_numPort.Value,
                CallingAET = _txtCallingAET.Text.Trim(),
                CalledAET = _txtCalledAET.Text.Trim(),
                UsePatientRoot = _radPatientRoot.Checked
            };
            File.WriteAllText(_settingsFile, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* ignore */ }
    }

    // ── Helpers ──────────────────────────────────────────────────

    internal void SetQuerying(bool querying)
    {
        if (InvokeRequired) { Invoke(() => SetQuerying(querying)); return; }
        _querying = querying;
        _btnStudyQuery.Enabled = !querying;
        _btnSeriesQuery.Enabled = !querying;
        _btnInstQuery.Enabled = !querying;
        _btnWlQuery.Enabled = !querying;
        _btnEcho.Enabled = !querying;
    }

    internal void Log(string message, Color? color = null)
    {
        if (InvokeRequired) { Invoke(() => Log(message, color)); return; }
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.SelectionColor = Color.Gray;
        _rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] ");
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.SelectionColor = color ?? Color.LightGray;
        _rtbLog.AppendText(message + "\n");
        _rtbLog.ScrollToCaret();
    }

    private static void ExportToCsv(DataGridView dgv, string defaultName)
    {
        if (dgv.Rows.Count == 0) return;
        using var dlg = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"{defaultName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        using var sw = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8);
        // Header
        var headers = dgv.Columns.Cast<DataGridViewColumn>().Select(c => QuoteCsv(c.HeaderText));
        sw.WriteLine(string.Join(",", headers));
        // Rows
        foreach (DataGridViewRow row in dgv.Rows)
        {
            var cells = row.Cells.Cast<DataGridViewCell>().Select(c => QuoteCsv(c.Value?.ToString() ?? ""));
            sw.WriteLine(string.Join(",", cells));
        }
    }

    private static string QuoteCsv(string s) => $"\"{s.Replace("\"", "\"\"")}\"";

    private static Label MakeLabel(string text) => new Label
    {
        Text = text,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Panel MakeSpacer(int width) => new Panel { Width = width, Height = 1 };

    // ── Grid context menus ────────────────────────────────────────

    private void DgvStudy_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
        _dgvStudy.ClearSelection();
        _dgvStudy.Rows[e.RowIndex].Selected = true;

        var studyUID = _dgvStudy.Rows[e.RowIndex].Cells["StudyUID"].Value?.ToString() ?? "";

        var menu = new ContextMenuStrip();
        menu.Items.Add("Copy Cell", null, (_, _) =>
        {
            if (e.ColumnIndex >= 0)
                Clipboard.SetText(_dgvStudy.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "");
        });
        menu.Items.Add("Copy Row (Tab-separated)", null, (_, _) =>
        {
            var row = _dgvStudy.Rows[e.RowIndex];
            var vals = row.Cells.Cast<DataGridViewCell>().Select(c => c.Value?.ToString() ?? "");
            Clipboard.SetText(string.Join("\t", vals));
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Query Series for this Study", null, async (_, _) =>
        {
            _txtSeriesStudyUID.Text = studyUID;
            _tabs.SelectedIndex = 1;
            await RunSeriesQueryAsync();
        });
        menu.Items.Add("Query Instances for this Study", null, async (_, _) =>
        {
            _txtInstStudyUID.Text = studyUID;
            _txtInstSeriesUID.Text = "";
            _tabs.SelectedIndex = 2;
            await RunInstanceQueryAsync();
        });
        menu.Show(_dgvStudy, _dgvStudy.PointToClient(Cursor.Position));
    }

    private void DgvSeries_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
        _dgvSeries.ClearSelection();
        _dgvSeries.Rows[e.RowIndex].Selected = true;

        var studyUID = _dgvSeries.Rows[e.RowIndex].Cells["StudyUID_S"].Value?.ToString() ?? "";
        var seriesUID = _dgvSeries.Rows[e.RowIndex].Cells["SeriesUID"].Value?.ToString() ?? "";

        var menu = new ContextMenuStrip();
        menu.Items.Add("Copy Cell", null, (_, _) =>
        {
            if (e.ColumnIndex >= 0)
                Clipboard.SetText(_dgvSeries.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "");
        });
        menu.Items.Add("Copy Row (Tab-separated)", null, (_, _) =>
        {
            var row = _dgvSeries.Rows[e.RowIndex];
            var vals = row.Cells.Cast<DataGridViewCell>().Select(c => c.Value?.ToString() ?? "");
            Clipboard.SetText(string.Join("\t", vals));
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Query Instances for this Series", null, async (_, _) =>
        {
            _txtInstStudyUID.Text = studyUID;
            _txtInstSeriesUID.Text = seriesUID;
            _tabs.SelectedIndex = 2;
            await RunInstanceQueryAsync();
        });
        menu.Show(_dgvSeries, _dgvSeries.PointToClient(Cursor.Position));
    }
}
