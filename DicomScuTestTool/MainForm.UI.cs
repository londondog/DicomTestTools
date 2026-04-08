using System.Drawing;

namespace DicomScuTestTool;

public partial class MainForm
{
    private void BuildUI()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(6, 4, 6, 4)
        };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // connection
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // files + demographics
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // action bar
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f));  // log
        Controls.Add(outer);

        outer.Controls.Add(BuildConnectionGroup(), 0, 0);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical
        };
        split.Panel1.Controls.Add(BuildFilesGroup());

        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));
        rightPanel.Controls.Add(BuildDemographicsGroup(), 0, 0);
        rightPanel.Controls.Add(BuildProcedureGroup(), 0, 1);
        split.Panel2.Controls.Add(rightPanel);
        outer.Controls.Add(split, 0, 1);

        // Set splitter and min sizes after the form has a real width
        Load += (_, _) =>
        {
            split.Panel1MinSize = 300;
            split.Panel2MinSize = 280;
            split.SplitterDistance = (int)(ClientSize.Width * 0.6);
        };

        outer.Controls.Add(BuildActionBar(), 0, 2);
        outer.Controls.Add(BuildLogGroup(), 0, 3);
    }

    private GroupBox BuildConnectionGroup()
    {
        var grp = new GroupBox
        {
            Text = "Destination",
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 4, 8, 6),
            Height = 58
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true
        };

        flow.Controls.Add(MakeLabel("Host:"));
        _txtHost = new TextBox { Text = "127.0.0.1", Width = 130 };
        flow.Controls.Add(_txtHost);
        flow.Controls.Add(MakeSpacer(6));

        flow.Controls.Add(MakeLabel("Port:"));
        _numPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 11112, Width = 68 };
        flow.Controls.Add(_numPort);
        flow.Controls.Add(MakeSpacer(6));

        flow.Controls.Add(MakeLabel("Calling AET:"));
        _txtCallingAET = new TextBox { Text = "SCU_TEST", Width = 110 };
        flow.Controls.Add(_txtCallingAET);
        flow.Controls.Add(MakeSpacer(6));

        flow.Controls.Add(MakeLabel("Called AET:"));
        _txtCalledAET = new TextBox { Text = "ANY-SCP", Width = 110 };
        flow.Controls.Add(_txtCalledAET);
        flow.Controls.Add(MakeSpacer(12));

        _btnEcho = new Button { Text = "C-ECHO", Width = 72, Height = 26 };
        flow.Controls.Add(_btnEcho);
        flow.Controls.Add(MakeSpacer(8));

        _lblEchoStatus = new Label
        {
            Text = "",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Gray,
            Width = 160
        };
        flow.Controls.Add(_lblEchoStatus);

        grp.Controls.Add(flow);
        return grp;
    }

    private GroupBox BuildFilesGroup()
    {
        var grp = new GroupBox { Text = "DICOM Files", Dock = DockStyle.Fill, Padding = new Padding(6) };

        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var btnBar = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false };
        _btnAddFiles = new Button { Text = "Add Files...", Width = 84, Height = 26 };
        _btnAddFolder = new Button { Text = "Add Folder...", Width = 90, Height = 26 };
        _btnRemove = new Button { Text = "Remove", Width = 72, Height = 26 };
        _btnClearAll = new Button { Text = "Clear All", Width = 72, Height = 26 };
        _lblFileCount = new Label { Text = "0 file(s)", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        btnBar.Controls.AddRange(new Control[] { _btnAddFiles, _btnAddFolder, _btnRemove, _btnClearAll, MakeSpacer(8), _lblFileCount });
        tbl.Controls.Add(btnBar, 0, 0);

        _lvFiles = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = true,
            AllowColumnReorder = true
        };
        _lvFiles.Columns.Add("#", 32);
        _lvFiles.Columns.Add("Filename", 180);
        _lvFiles.Columns.Add("Patient Name", 130);
        _lvFiles.Columns.Add("Patient ID", 90);
        _lvFiles.Columns.Add("Accession", 100);
        _lvFiles.Columns.Add("Study Date", 80);
        _lvFiles.Columns.Add("Modality", 60);
        _lvFiles.Columns.Add("Status", 90);
        tbl.Controls.Add(_lvFiles, 0, 1);

        grp.Controls.Add(tbl);
        return grp;
    }

    private GroupBox BuildDemographicsGroup()
    {
        var grp = new GroupBox
        {
            Text = "Demographics Override",
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 4, 8, 6)
        };

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = false,
            Padding = new Padding(0, 2, 0, 0)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        int row = 0;

        _chkOverride = new CheckBox
        {
            Text = "Enable demographics override",
            Checked = true,
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold)
        };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.Controls.Add(_chkOverride, 0, row);
        tbl.SetColumnSpan(_chkOverride, 2);
        row++;

        void AddField(string label, Control ctrl)
        {
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tbl.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 4, 0)
            }, 0, row);
            ctrl.Dock = DockStyle.Fill;
            tbl.Controls.Add(ctrl, 1, row);
            row++;
        }

        void AddSeparator(string title)
        {
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lbl = new Label
            {
                Text = $"── {title} ──────────────────────",
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(120, 180, 255),
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(0, 4, 0, 2)
            };
            tbl.Controls.Add(lbl, 0, row);
            tbl.SetColumnSpan(lbl, 2);
            row++;
        }

        AddSeparator("Patient");

        _txtPatientName = new TextBox { PlaceholderText = "LAST^FIRST^MIDDLE" };
        AddField("Patient Name:", _txtPatientName);

        _txtPatientID = new TextBox { PlaceholderText = "MRN / Patient ID" };
        AddField("Patient ID (MRN):", _txtPatientID);

        _txtDOB = new TextBox { PlaceholderText = "YYYYMMDD  e.g. 19850314" };
        AddField("Date of Birth:", _txtDOB);

        _cmbSex = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbSex.Items.AddRange(new object[] { "", "M", "F", "O" });
        AddField("Sex:", _cmbSex);

        AddSeparator("Study");

        _txtAccession = new TextBox { PlaceholderText = "Accession number" };
        AddField("Accession No.:", _txtAccession);

        _txtStudyDate = new TextBox { PlaceholderText = "YYYYMMDD" };
        AddField("Study Date:", _txtStudyDate);

        _txtStudyTime = new TextBox { PlaceholderText = "HHMMSS" };
        AddField("Study Time:", _txtStudyTime);

        _txtStudyDesc = new TextBox { PlaceholderText = "Study description" };
        AddField("Study Desc.:", _txtStudyDesc);

        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 8f));
        tbl.Controls.Add(new Label(), 0, row); row++;

        var btnFlow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        _btnRandomize = new Button { Text = "Randomize", Width = 88, Height = 28 };
        _btnLoadFromFile = new Button { Text = "Load from File", Width = 102, Height = 28 };
        _btnCopyTemplate = new Button { Text = "Copy Template", Width = 106, Height = 28 };
        _btnPasteAll = new Button { Text = "Paste", Width = 60, Height = 28, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnFlow.Controls.Add(_btnRandomize);
        btnFlow.Controls.Add(MakeSpacer(4));
        btnFlow.Controls.Add(_btnLoadFromFile);
        btnFlow.Controls.Add(MakeSpacer(4));
        btnFlow.Controls.Add(_btnCopyTemplate);
        btnFlow.Controls.Add(MakeSpacer(4));
        btnFlow.Controls.Add(_btnPasteAll);
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.Controls.Add(btnFlow, 0, row);
        tbl.SetColumnSpan(btnFlow, 2);

        grp.Controls.Add(tbl);
        return grp;
    }

    private GroupBox BuildProcedureGroup()
    {
        var grp = new GroupBox
        {
            Text = "Procedure Override",
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 4, 8, 6)
        };

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = false,
            Padding = new Padding(0, 2, 0, 0)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        int row = 0;

        _chkOverrideProcedure = new CheckBox
        {
            Text = "Enable procedure override",
            Checked = false,
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold)
        };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.Controls.Add(_chkOverrideProcedure, 0, row);
        tbl.SetColumnSpan(_chkOverrideProcedure, 2);
        row++;

        void AddField(string label, Control ctrl)
        {
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tbl.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 4, 0)
            }, 0, row);
            ctrl.Dock = DockStyle.Fill;
            tbl.Controls.Add(ctrl, 1, row);
            row++;
        }

        _cmbModality = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbModality.Items.AddRange(new object[] { "", "CT", "MR", "US", "OPT", "CR", "DX", "MG", "NM", "PT", "RF", "XA", "ES", "SC", "OT" });
        AddField("Modality:", _cmbModality);

        _txtProcedureDesc = new TextBox { PlaceholderText = "Procedure description" };
        AddField("Procedure Desc.:", _txtProcedureDesc);

        // Study UID row with Generate button
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.Controls.Add(new Label { Text = "Study UID:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 4, 0) }, 0, row);
        var studyUidPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
        studyUidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        studyUidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _txtStudyUID = new TextBox { PlaceholderText = "Leave blank to keep original", Dock = DockStyle.Fill };
        var btnNewStudy = new Button { Text = "New", Width = 40, Height = 22 };
        btnNewStudy.Click += (_, _) => _txtStudyUID.Text = DicomUIDGenerator.GenerateNew();
        studyUidPanel.Controls.Add(_txtStudyUID, 0, 0);
        studyUidPanel.Controls.Add(btnNewStudy, 1, 0);
        tbl.Controls.Add(studyUidPanel, 1, row); row++;

        // Series UID row with Generate button
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.Controls.Add(new Label { Text = "Series UID:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 4, 0) }, 0, row);
        var seriesUidPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
        seriesUidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        seriesUidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _txtSeriesUID = new TextBox { PlaceholderText = "Leave blank to keep original", Dock = DockStyle.Fill };
        var btnNewSeries = new Button { Text = "New", Width = 40, Height = 22 };
        btnNewSeries.Click += (_, _) => _txtSeriesUID.Text = DicomUIDGenerator.GenerateNew();
        seriesUidPanel.Controls.Add(_txtSeriesUID, 0, 0);
        seriesUidPanel.Controls.Add(btnNewSeries, 1, 0);
        tbl.Controls.Add(seriesUidPanel, 1, row); row++;

        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 6f));
        tbl.Controls.Add(new Label(), 0, row); row++;

        // UID generation checkboxes
        _chkNewStudyUID = new CheckBox { Text = "Generate new Study UID", AutoSize = true };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.Controls.Add(_chkNewStudyUID, 0, row);
        tbl.SetColumnSpan(_chkNewStudyUID, 2);
        row++;

        _chkNewSeriesUID = new CheckBox { Text = "Generate new Series UID", AutoSize = true };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.Controls.Add(_chkNewSeriesUID, 0, row);
        tbl.SetColumnSpan(_chkNewSeriesUID, 2);
        row++;

        _chkNewSOPUID = new CheckBox { Text = "Generate new SOP Instance UID (per file)", AutoSize = true, Checked = true };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.Controls.Add(_chkNewSOPUID, 0, row);
        tbl.SetColumnSpan(_chkNewSOPUID, 2);

        grp.Controls.Add(tbl);
        return grp;
    }

    private Panel BuildActionBar()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Height = 42, Padding = new Padding(0, 4, 0, 4) };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

        _btnSendAll = new Button { Text = "Send All", Width = 88, Height = 28, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnSendSelected = new Button { Text = "Send Selected", Width = 104, Height = 28, BackColor = Color.FromArgb(0, 153, 76), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnCancelSend = new Button { Text = "Cancel", Width = 72, Height = 28, Enabled = false };
        _progressBar = new ProgressBar { Width = 220, Height = 22, Style = ProgressBarStyle.Continuous };
        _lblProgress = new Label { Text = "", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Width = 200 };

        flow.Controls.AddRange(new Control[]
        {
            _btnSendAll, MakeSpacer(4), _btnSendSelected, MakeSpacer(8),
            _btnCancelSend, MakeSpacer(20), _progressBar, MakeSpacer(8), _lblProgress
        });

        panel.Controls.Add(flow);
        return panel;
    }

    private GroupBox BuildLogGroup()
    {
        var grp = new GroupBox { Text = "Log", Dock = DockStyle.Fill, Padding = new Padding(6) };

        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var btnBar = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        var btnClearLog = new Button { Text = "Clear Log", Width = 72, Height = 22 };
        btnClearLog.Click += (_, _) => _rtbLog.Clear();
        btnBar.Controls.Add(btnClearLog);
        tbl.Controls.Add(btnBar, 0, 0);

        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(18, 18, 18),
            ForeColor = Color.LightGray,
            Font = new Font("Consolas", 9f),
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = false
        };
        tbl.Controls.Add(_rtbLog, 0, 1);

        grp.Controls.Add(tbl);
        return grp;
    }
}
