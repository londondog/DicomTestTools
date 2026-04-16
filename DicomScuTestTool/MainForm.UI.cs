using System.Drawing;

namespace DicomScuTestTool;

public partial class MainForm
{
    private void BuildUI()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(6, 4, 6, 4)
        };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // menu
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // connection
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // files + demographics
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // action bar
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f));  // log
        Controls.Add(outer);

        outer.Controls.Add(BuildMainMenu(), 0, 0);
        outer.Controls.Add(BuildConnectionGroup(), 0, 1);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical
        };
        split.Panel1.Controls.Add(BuildFilesGroup());

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill
        };
        tabs.TabPages.Add(new TabPage("Demographics and Study") { Padding = new Padding(4) });
        tabs.TabPages[0].Controls.Add(BuildDemographicsGroup());
        tabs.TabPages.Add(new TabPage("Procedure") { Padding = new Padding(4) });
        tabs.TabPages[1].Controls.Add(BuildProcedureGroup());
        tabs.TabPages.Add(new TabPage("Lookup") { Padding = new Padding(4) });
        tabs.TabPages[2].Controls.Add(BuildLookupTab());

        split.Panel2.Controls.Add(tabs);
        outer.Controls.Add(split, 0, 2);

        // Set splitter and min sizes after the form has a real width
        Load += (_, _) =>
        {
            split.Panel1MinSize = 300;
            split.Panel2MinSize = 280;
            split.SplitterDistance = (int)(ClientSize.Width * 0.6);
        };

        outer.Controls.Add(BuildActionBar(), 0, 3);
        outer.Controls.Add(BuildLogGroup(), 0, 4);
    }

    private MenuStrip BuildMainMenu()
    {
        var menu = new MenuStrip
        {
            Dock = DockStyle.Fill
        };

        var help = new ToolStripMenuItem("Help");
        var about = new ToolStripMenuItem("About");
        about.Click += (_, _) => ShowAboutDialog();
        help.DropDownItems.Add(about);

        menu.Items.Add(help);
        MainMenuStrip = menu;
        return menu;
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
            Text = "Demographics and Study Override",
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

        _txtStudyTime = new TextBox { PlaceholderText = "HHMM" };
        AddField("Study Time:", _txtStudyTime);

        _txtStudyDesc = new TextBox { PlaceholderText = "Study description" };
        AddField("Study Desc.:", _txtStudyDesc);

        _cmbModality = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbModality.Items.AddRange(new object[] { "", "CR", "CT", "DX", "ECG", "EP", "ES", "MG", "MR", "NM", "OPT", "OT", "PT", "RF", "SC", "US", "XA" });
        AddField("Modality:", _cmbModality);

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

    private Control BuildLookupTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(2)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 44f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 56f));

        var connectionBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false
        };
        connectionBar.Controls.Add(MakeLabel("Connection String:"));
        _txtLookupConnectionString = new TextBox
        {
            Width = 460,
            PlaceholderText = "Data Source=localhost;Initial Catalog=Medcon;Integrated Security=True;TrustServerCertificate=True;",
            Text = "Data Source=localhost;Initial Catalog=Medcon;Integrated Security=True;TrustServerCertificate=True;"
        };
        connectionBar.Controls.Add(_txtLookupConnectionString);
        connectionBar.Controls.Add(MakeSpacer(6));
        _chkLookupTrustServerCert = new CheckBox
        {
            Text = "Trust Server Cert",
            AutoSize = true,
            Checked = true
        };
        connectionBar.Controls.Add(_chkLookupTrustServerCert);
        connectionBar.Controls.Add(MakeSpacer(6));
        _btnLookupTestConnection = new Button { Text = "Test", Width = 64, Height = 26 };
        connectionBar.Controls.Add(_btnLookupTestConnection);
        connectionBar.Controls.Add(MakeSpacer(8));
        connectionBar.Controls.Add(MakeLabel("Days:"));
        _numLookupDays = new NumericUpDown { Minimum = 1, Maximum = 60, Value = 7, Width = 58 };
        connectionBar.Controls.Add(_numLookupDays);
        root.Controls.Add(connectionBar, 0, 0);

        var searchBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false
        };
        searchBar.Controls.Add(MakeLabel("Patient ID:"));
        _txtLookupPatientId = new TextBox { Width = 110, PlaceholderText = "MRN" };
        searchBar.Controls.Add(_txtLookupPatientId);
        searchBar.Controls.Add(MakeSpacer(6));
        searchBar.Controls.Add(MakeLabel("Name:"));
        _txtLookupPatientName = new TextBox { Width = 150, PlaceholderText = "Last, First or partial" };
        searchBar.Controls.Add(_txtLookupPatientName);
        searchBar.Controls.Add(MakeSpacer(6));
        searchBar.Controls.Add(MakeLabel("Accession:"));
        _txtLookupAccession = new TextBox { Width = 110, PlaceholderText = "Accession No." };
        searchBar.Controls.Add(_txtLookupAccession);
        searchBar.Controls.Add(MakeSpacer(10));
        _btnLookupPatients = new Button { Text = "Find Patients", Width = 104, Height = 28 };
        _btnLookupOrders = new Button { Text = "Find Orders", Width = 94, Height = 28 };
        _btnLookupProcedures = new Button { Text = "Find Procedures", Width = 112, Height = 28 };
        searchBar.Controls.Add(_btnLookupPatients);
        searchBar.Controls.Add(MakeSpacer(4));
        searchBar.Controls.Add(_btnLookupOrders);
        searchBar.Controls.Add(MakeSpacer(4));
        searchBar.Controls.Add(_btnLookupProcedures);
        root.Controls.Add(searchBar, 0, 1);

        _dgvLookupPatients = CreateLookupGrid();
        _dgvLookupPatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientId", HeaderText = "Patient ID", DataPropertyName = "PatientId", Width = 90 });
        _dgvLookupPatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "Name", DataPropertyName = "FullName", Width = 180 });
        _dgvLookupPatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "DateOfBirth", HeaderText = "DOB", DataPropertyName = "DateOfBirth", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        _dgvLookupPatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sex", HeaderText = "Sex", DataPropertyName = "Sex", Width = 50 });
        _dgvLookupPatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Age", HeaderText = "Age", DataPropertyName = "Age", Width = 50 });
        root.Controls.Add(_dgvLookupPatients, 0, 2);

        var applyBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false
        };
        _btnApplySelectedPatient = new Button { Text = "Apply Selected Patient", Width = 150, Height = 28 };
        _btnApplySelectedOrder = new Button { Text = "Apply Selected Order", Width = 140, Height = 28 };
        _btnApplySelectedBoth = new Button { Text = "Apply Both", Width = 88, Height = 28, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _chkApplyNewStudyUID = new CheckBox { Text = "New Study UID", AutoSize = true, Checked = true };
        applyBar.Controls.Add(_btnApplySelectedPatient);
        applyBar.Controls.Add(MakeSpacer(4));
        applyBar.Controls.Add(_btnApplySelectedOrder);
        applyBar.Controls.Add(MakeSpacer(8));
        applyBar.Controls.Add(_chkApplyNewStudyUID);
        applyBar.Controls.Add(MakeSpacer(16));
        applyBar.Controls.Add(_btnApplySelectedBoth);
        root.Controls.Add(applyBar, 0, 3);

        _dgvLookupOrders = CreateLookupGrid();
        _dgvLookupOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccessionNumber", HeaderText = "Accession", DataPropertyName = "AccessionNumber", Width = 110 });
        _dgvLookupOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientId", HeaderText = "Patient ID", DataPropertyName = "PatientId", Width = 90 });
        _dgvLookupOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientName", HeaderText = "Name", DataPropertyName = "PatientName", Width = 150 });
        _dgvLookupOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "StartTime", HeaderText = "Start Time", DataPropertyName = "StartTime", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });
        _dgvLookupOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ScheduledProcedureStepId", HeaderText = "Procedure", DataPropertyName = "ScheduledProcedureStepId", Width = 140 });
        _dgvLookupOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProcedureCode", HeaderText = "Procedure Code", DataPropertyName = "ProcedureCode", Width = 140 });
        _dgvLookupOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "WorkflowStatus", HeaderText = "Status", DataPropertyName = "WorkflowStatus", Width = 110 });
        _dgvLookupOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "Gender", HeaderText = "Gender", DataPropertyName = "Gender", Width = 70 });
        root.Controls.Add(_dgvLookupOrders, 0, 4);

        return root;
    }

    private static DataGridView CreateLookupGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false
        };
    }
}
