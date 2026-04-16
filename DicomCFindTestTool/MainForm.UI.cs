namespace DicomCFindTestTool;

public partial class MainForm
{
    private static readonly string[] Modalities =
        ["", "CT", "MR", "US", "ECG", "XA", "NM", "PT", "MG", "CR", "DX", "RF", "OPT",
         "HD", "EPS", "IVUS", "ES", "SC", "SR", "PR", "KO", "OT"];

    private static readonly string[] ProcedureTypes =
    [
        "",
        // Cardiology
        "ECHOCARDIOGRAPHY", "ECG", "EKG", "STRESS TEST", "CARDIAC CATH",
        "HOLTER MONITOR", "EP STUDY", "NUCLEAR CARDIOLOGY", "CARDIAC MRI", "CARDIAC CT",
        "CORONARY ANGIOGRAPHY", "PACEMAKER CHECK", "TILT TABLE TEST",
        // Radiology / General
        "CHEST X-RAY", "CT CHEST", "CT ABDOMEN", "CT PELVIS", "CT HEAD", "CT NECK",
        "CT CHEST ABDOMEN PELVIS", "CT ANGIOGRAPHY",
        "MRI BRAIN", "MRI SPINE", "MRI KNEE", "MRI SHOULDER", "MRI ABDOMEN",
        "ULTRASOUND ABDOMEN", "ULTRASOUND PELVIS", "ULTRASOUND THYROID",
        "MAMMOGRAPHY", "BONE DENSITY (DEXA)", "PET SCAN", "PET CT",
        "FLUOROSCOPY", "BARIUM SWALLOW", "BARIUM ENEMA",
        "INTERVENTIONAL", "BIOPSY",
    ];

    private void BuildUI()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(6, 4, 6, 4)
        };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // menu
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // connection
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // tabs
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 200f));  // log

        outer.Controls.Add(BuildMainMenu(), 0, 0);
        outer.Controls.Add(BuildConnectionGroup(), 0, 1);
        outer.Controls.Add(BuildTabControl(), 0, 2);
        outer.Controls.Add(BuildLogGroup(), 0, 3);

        Controls.Add(outer);
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
            Text = "Connection",
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
        _txtHost = new TextBox { Text = "", Width = 140 };
        flow.Controls.Add(_txtHost);
        flow.Controls.Add(MakeSpacer(6));

        flow.Controls.Add(MakeLabel("Port:"));
        _numPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 1177, Width = 68 };
        flow.Controls.Add(_numPort);
        flow.Controls.Add(MakeSpacer(6));

        flow.Controls.Add(MakeLabel("Calling AET:"));
        _txtCallingAET = new TextBox { Text = "CFIND_TEST", Width = 110 };
        flow.Controls.Add(_txtCallingAET);
        flow.Controls.Add(MakeSpacer(6));

        flow.Controls.Add(MakeLabel("Called AET:"));
        _txtCalledAET = new TextBox { Text = "CARD_WL", Width = 110 };
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
            Width = 200
        };
        flow.Controls.Add(_lblEchoStatus);

        grp.Controls.Add(flow);
        return grp;
    }

    private TabControl BuildTabControl()
    {
        _tabs = new TabControl { Dock = DockStyle.Fill };
        _tabs.TabPages.Add(BuildStudyTab());
        _tabs.TabPages.Add(BuildSeriesTab());
        _tabs.TabPages.Add(BuildInstanceTab());
        _tabs.TabPages.Add(BuildWorklistTab());
        return _tabs;
    }

    // ── Study / Patient Tab ───────────────────────────────────────

    private TabPage BuildStudyTab()
    {
        var page = new TabPage("Study / Patient");
        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(4)
        };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // filter fields
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // button bar
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // grid

        tbl.Controls.Add(BuildStudyFilterPanel(), 0, 0);
        tbl.Controls.Add(BuildStudyButtonBar(), 0, 1);
        tbl.Controls.Add(BuildStudyGrid(), 0, 2);

        page.Controls.Add(tbl);
        return page;
    }

    private Panel BuildStudyFilterPanel()
    {
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            Padding = new Padding(0, 4, 0, 4)
        };
        // 3 pairs of label+field columns, repeated twice
        for (int i = 0; i < 6; i++)
            grid.ColumnStyles.Add(i % 2 == 0
                ? new ColumnStyle(SizeType.AutoSize)
                : new ColumnStyle(SizeType.Percent, 16.6f));

        int row = 0;

        void Add(string label, Control ctrl, int col)
        {
            if (grid.RowCount <= row) grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 4, 0),
                Dock = DockStyle.Fill
            }, col * 2, row);
            ctrl.Dock = DockStyle.Fill;
            grid.Controls.Add(ctrl, col * 2 + 1, row);
        }

        // Row 0
        _txtStudyPatientID = new TextBox { PlaceholderText = "wildcard OK (e.g. 12345*)" };
        _txtStudyPatientName = new TextBox { PlaceholderText = "LAST^FIRST or SMITH*" };
        _cmbStudyModality = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbStudyModality.Items.AddRange(Modalities);
        Add("Patient ID:", _txtStudyPatientID, 0);
        Add("Patient Name:", _txtStudyPatientName, 1);
        Add("Modality:", _cmbStudyModality, 2);
        row++;

        // Row 1
        _txtStudyDOB = new TextBox { PlaceholderText = "YYYYMMDD" };
        _cmbStudySex = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbStudySex.Items.AddRange(new object[] { "", "M", "F", "O" });
        _txtStudyAccession = new TextBox { PlaceholderText = "wildcard OK" };
        Add("Date of Birth:", _txtStudyDOB, 0);
        Add("Sex:", _cmbStudySex, 1);
        Add("Accession No.:", _txtStudyAccession, 2);
        row++;

        // Row 2 - dates
        _txtStudyUID = new TextBox { PlaceholderText = "exact Study Instance UID" };
        _txtStudyDesc = new TextBox { PlaceholderText = "wildcard OK" };
        _txtStudyReferringPhysician = new TextBox { PlaceholderText = "wildcard OK" };
        Add("Study UID:", _txtStudyUID, 0);
        Add("Study Desc.:", _txtStudyDesc, 1);
        Add("Ref. Physician:", _txtStudyReferringPhysician, 2);
        row++;

        // Row 3 - date range + query root
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var datePanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Dock = DockStyle.Fill
        };
        _chkStudyDateFrom = new CheckBox { Text = "Study Date From:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Checked = false };
        _dtpStudyDateFrom = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Value = DateTime.Today.AddMonths(-1), Width = 110, Enabled = false };
        _chkStudyDateTo = new CheckBox { Text = "To:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Checked = false };
        _dtpStudyDateTo = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Value = DateTime.Today, Width = 110, Enabled = false };
        _radStudyRoot = new RadioButton { Text = "Study Root", Checked = true, AutoSize = true };
        _radPatientRoot = new RadioButton { Text = "Patient Root", AutoSize = true };
        datePanel.Controls.Add(_chkStudyDateFrom);
        datePanel.Controls.Add(MakeSpacer(2));
        datePanel.Controls.Add(_dtpStudyDateFrom);
        datePanel.Controls.Add(MakeSpacer(12));
        datePanel.Controls.Add(_chkStudyDateTo);
        datePanel.Controls.Add(MakeSpacer(2));
        datePanel.Controls.Add(_dtpStudyDateTo);
        datePanel.Controls.Add(MakeSpacer(24));
        datePanel.Controls.Add(new Label { Text = "Query Root:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        datePanel.Controls.Add(MakeSpacer(4));
        datePanel.Controls.Add(_radStudyRoot);
        datePanel.Controls.Add(MakeSpacer(4));
        datePanel.Controls.Add(_radPatientRoot);
        grid.Controls.Add(datePanel, 0, row);
        grid.SetColumnSpan(datePanel, 6);

        return grid;
    }

    private Panel BuildStudyButtonBar()
    {
        var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 4)
        };
        _btnStudyQuery = new Button { Text = "Query", Width = 88, Height = 28, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnStudyClear = new Button { Text = "Clear Results", Width = 90, Height = 28 };
        _btnStudyExport = new Button { Text = "Export CSV...", Width = 90, Height = 28 };
        _lblStudyCount = new Label { Text = "0 result(s)", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        flow.Controls.Add(_btnStudyQuery);
        flow.Controls.Add(MakeSpacer(6));
        flow.Controls.Add(_btnStudyClear);
        flow.Controls.Add(MakeSpacer(6));
        flow.Controls.Add(_btnStudyExport);
        flow.Controls.Add(MakeSpacer(16));
        flow.Controls.Add(_lblStudyCount);
        return flow;
    }

    private DataGridView BuildStudyGrid()
    {
        _dgvStudy = MakeGrid();
        _dgvStudy.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatName", HeaderText = "Patient Name", Width = 140 });
        _dgvStudy.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatID", HeaderText = "Patient ID", Width = 100 });
        _dgvStudy.Columns.Add(new DataGridViewTextBoxColumn { Name = "DOB", HeaderText = "DOB", Width = 80 });
        _dgvStudy.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sex", HeaderText = "Sex", Width = 40 });
        _dgvStudy.Columns.Add(new DataGridViewTextBoxColumn { Name = "StudyDate", HeaderText = "Study Date", Width = 80 });
        _dgvStudy.Columns.Add(new DataGridViewTextBoxColumn { Name = "Accession", HeaderText = "Accession", Width = 110 });
        _dgvStudy.Columns.Add(new DataGridViewTextBoxColumn { Name = "Modality", HeaderText = "Modality", Width = 75 });
        _dgvStudy.Columns.Add(new DataGridViewTextBoxColumn { Name = "StudyDesc", HeaderText = "Study Description", Width = 160, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _dgvStudy.Columns.Add(new DataGridViewTextBoxColumn { Name = "Series", HeaderText = "Series#", Width = 55 });
        _dgvStudy.Columns.Add(new DataGridViewTextBoxColumn { Name = "Images", HeaderText = "Images#", Width = 60 });
        _dgvStudy.Columns.Add(new DataGridViewTextBoxColumn { Name = "StudyUID", HeaderText = "Study Instance UID", Width = 280 });
        return _dgvStudy;
    }

    // ── Series Tab ───────────────────────────────────────────────

    private TabPage BuildSeriesTab()
    {
        var page = new TabPage("Series");
        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(4) };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        tbl.Controls.Add(BuildSeriesFilterPanel(), 0, 0);
        tbl.Controls.Add(BuildSeriesButtonBar(), 0, 1);
        tbl.Controls.Add(BuildSeriesGrid(), 0, 2);

        page.Controls.Add(tbl);
        return page;
    }

    private Panel BuildSeriesFilterPanel()
    {
        var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 4)
        };

        flow.Controls.Add(new Label { Text = "Study UID:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        _txtSeriesStudyUID = new TextBox { Width = 320, PlaceholderText = "required — paste from Study tab or type directly" };
        flow.Controls.Add(_txtSeriesStudyUID);
        flow.Controls.Add(MakeSpacer(16));

        flow.Controls.Add(new Label { Text = "Series UID:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        _txtSeriesSeriesUID = new TextBox { Width = 280, PlaceholderText = "optional — leave blank for all series" };
        flow.Controls.Add(_txtSeriesSeriesUID);
        flow.Controls.Add(MakeSpacer(16));

        flow.Controls.Add(new Label { Text = "Modality:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        _cmbSeriesModality = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
        _cmbSeriesModality.Items.AddRange(Modalities);
        flow.Controls.Add(_cmbSeriesModality);
        flow.Controls.Add(MakeSpacer(16));

        flow.Controls.Add(new Label { Text = "Series Desc.:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        _txtSeriesDesc = new TextBox { Width = 180, PlaceholderText = "wildcard OK" };
        flow.Controls.Add(_txtSeriesDesc);

        return flow;
    }

    private Panel BuildSeriesButtonBar()
    {
        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 4, 0, 4) };
        _btnSeriesQuery = new Button { Text = "Query", Width = 88, Height = 28, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnSeriesClear = new Button { Text = "Clear Results", Width = 90, Height = 28 };
        _btnSeriesExport = new Button { Text = "Export CSV...", Width = 90, Height = 28 };
        _lblSeriesCount = new Label { Text = "0 result(s)", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        flow.Controls.Add(_btnSeriesQuery);
        flow.Controls.Add(MakeSpacer(6));
        flow.Controls.Add(_btnSeriesClear);
        flow.Controls.Add(MakeSpacer(6));
        flow.Controls.Add(_btnSeriesExport);
        flow.Controls.Add(MakeSpacer(16));
        flow.Controls.Add(_lblSeriesCount);
        return flow;
    }

    private DataGridView BuildSeriesGrid()
    {
        _dgvSeries = MakeGrid();
        _dgvSeries.Columns.Add(new DataGridViewTextBoxColumn { Name = "SeriesNum", HeaderText = "Series#", Width = 60 });
        _dgvSeries.Columns.Add(new DataGridViewTextBoxColumn { Name = "Modality_S", HeaderText = "Modality", Width = 70 });
        _dgvSeries.Columns.Add(new DataGridViewTextBoxColumn { Name = "SeriesDesc", HeaderText = "Series Description", Width = 180, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _dgvSeries.Columns.Add(new DataGridViewTextBoxColumn { Name = "SeriesDate", HeaderText = "Series Date", Width = 80 });
        _dgvSeries.Columns.Add(new DataGridViewTextBoxColumn { Name = "BodyPart", HeaderText = "Body Part", Width = 90 });
        _dgvSeries.Columns.Add(new DataGridViewTextBoxColumn { Name = "Images_S", HeaderText = "Images#", Width = 60 });
        _dgvSeries.Columns.Add(new DataGridViewTextBoxColumn { Name = "StudyUID_S", HeaderText = "Study UID", Width = 260 });
        _dgvSeries.Columns.Add(new DataGridViewTextBoxColumn { Name = "SeriesUID", HeaderText = "Series Instance UID", Width = 280 });
        return _dgvSeries;
    }

    // ── Instance Tab ──────────────────────────────────────────────

    private TabPage BuildInstanceTab()
    {
        var page = new TabPage("Instance");
        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(4) };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        tbl.Controls.Add(BuildInstanceFilterPanel(), 0, 0);
        tbl.Controls.Add(BuildInstanceButtonBar(), 0, 1);
        tbl.Controls.Add(BuildInstanceGrid(), 0, 2);

        page.Controls.Add(tbl);
        return page;
    }

    private Panel BuildInstanceFilterPanel()
    {
        var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 4)
        };

        flow.Controls.Add(new Label { Text = "Study UID:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        _txtInstStudyUID = new TextBox { Width = 320, PlaceholderText = "required" };
        flow.Controls.Add(_txtInstStudyUID);
        flow.Controls.Add(MakeSpacer(16));

        flow.Controls.Add(new Label { Text = "Series UID:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        _txtInstSeriesUID = new TextBox { Width = 320, PlaceholderText = "optional — leave blank for all series" };
        flow.Controls.Add(_txtInstSeriesUID);

        return flow;
    }

    private Panel BuildInstanceButtonBar()
    {
        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 4, 0, 4) };
        _btnInstQuery = new Button { Text = "Query", Width = 88, Height = 28, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnInstClear = new Button { Text = "Clear Results", Width = 90, Height = 28 };
        _btnInstExport = new Button { Text = "Export CSV...", Width = 90, Height = 28 };
        _lblInstCount = new Label { Text = "0 result(s)", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        flow.Controls.Add(_btnInstQuery);
        flow.Controls.Add(MakeSpacer(6));
        flow.Controls.Add(_btnInstClear);
        flow.Controls.Add(MakeSpacer(6));
        flow.Controls.Add(_btnInstExport);
        flow.Controls.Add(MakeSpacer(16));
        flow.Controls.Add(_lblInstCount);
        return flow;
    }

    private DataGridView BuildInstanceGrid()
    {
        _dgvInst = MakeGrid();
        _dgvInst.Columns.Add(new DataGridViewTextBoxColumn { Name = "InstNum", HeaderText = "Instance#", Width = 70 });
        _dgvInst.Columns.Add(new DataGridViewTextBoxColumn { Name = "SOPClass", HeaderText = "SOP Class", Width = 200, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _dgvInst.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rows", HeaderText = "Rows", Width = 55 });
        _dgvInst.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cols", HeaderText = "Cols", Width = 55 });
        _dgvInst.Columns.Add(new DataGridViewTextBoxColumn { Name = "ContentDate", HeaderText = "Content Date", Width = 90 });
        _dgvInst.Columns.Add(new DataGridViewTextBoxColumn { Name = "SOPInstanceUID", HeaderText = "SOP Instance UID", Width = 280 });
        _dgvInst.Columns.Add(new DataGridViewTextBoxColumn { Name = "SOPClassUID", HeaderText = "SOP Class UID", Width = 240 });
        return _dgvInst;
    }

    // ── Worklist (MWL) Tab ────────────────────────────────────────

    private TabPage BuildWorklistTab()
    {
        var page = new TabPage("Worklist (MWL)");
        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(4) };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        tbl.Controls.Add(BuildWorklistFilterPanel(), 0, 0);
        tbl.Controls.Add(BuildWorklistButtonBar(), 0, 1);
        tbl.Controls.Add(BuildWorklistGrid(), 0, 2);

        page.Controls.Add(tbl);
        return page;
    }

    private Panel BuildWorklistFilterPanel()
    {
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            Padding = new Padding(0, 4, 0, 4)
        };
        for (int i = 0; i < 6; i++)
            grid.ColumnStyles.Add(i % 2 == 0
                ? new ColumnStyle(SizeType.AutoSize)
                : new ColumnStyle(SizeType.Percent, 16.6f));

        int row = 0;

        void Add(string label, Control ctrl, int col)
        {
            if (grid.RowCount <= row) grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 4, 0),
                Dock = DockStyle.Fill
            }, col * 2, row);
            ctrl.Dock = DockStyle.Fill;
            grid.Controls.Add(ctrl, col * 2 + 1, row);
        }

        // Row 0
        _txtWlPatientID = new TextBox { PlaceholderText = "wildcard OK" };
        _txtWlPatientName = new TextBox { PlaceholderText = "wildcard OK" };
        _cmbWlModality = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbWlModality.Items.AddRange(Modalities);
        Add("Patient ID:", _txtWlPatientID, 0);
        Add("Patient Name:", _txtWlPatientName, 1);
        Add("Modality:", _cmbWlModality, 2);
        row++;

        // Row 1
        _txtWlAccession = new TextBox { PlaceholderText = "wildcard OK" };
        _txtWlStationAE = new TextBox { PlaceholderText = "exact AE title of modality" };
        _txtWlPhysician = new TextBox { PlaceholderText = "wildcard OK" };
        Add("Accession No.:", _txtWlAccession, 0);
        Add("Station AE Title:", _txtWlStationAE, 1);
        Add("Performing Physician:", _txtWlPhysician, 2);
        row++;

        // Row 2
        _txtWlRequestedProcID = new TextBox { PlaceholderText = "exact or wildcard" };
        _cmbWlProcedureType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown };
        _cmbWlProcedureType.Items.AddRange(ProcedureTypes);
        _txtWlProtocolCode = new TextBox { PlaceholderText = "code value (e.g. LOINC 11218-5)" };
        Add("Requested Proc. ID:", _txtWlRequestedProcID, 0);
        Add("Procedure Step Desc.:", _cmbWlProcedureType, 1);
        Add("Protocol Code:", _txtWlProtocolCode, 2);
        row++;

        // Row 3 - date range
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var datePanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Dock = DockStyle.Fill
        };
        _chkWlToday = new CheckBox { Text = "Today Only", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Checked = false };
        _chkWlDateFrom = new CheckBox { Text = "Sched. Date From:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Checked = false };
        _dtpWlDateFrom = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Value = DateTime.Today, Width = 110, Enabled = false };
        _chkWlDateTo = new CheckBox { Text = "To:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Checked = false };
        _dtpWlDateTo = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Value = DateTime.Today.AddDays(7), Width = 110, Enabled = false };
        datePanel.Controls.Add(_chkWlToday);
        datePanel.Controls.Add(MakeSpacer(16));
        datePanel.Controls.Add(_chkWlDateFrom);
        datePanel.Controls.Add(MakeSpacer(2));
        datePanel.Controls.Add(_dtpWlDateFrom);
        datePanel.Controls.Add(MakeSpacer(12));
        datePanel.Controls.Add(_chkWlDateTo);
        datePanel.Controls.Add(MakeSpacer(2));
        datePanel.Controls.Add(_dtpWlDateTo);
        grid.Controls.Add(datePanel, 0, row);
        grid.SetColumnSpan(datePanel, 6);

        return grid;
    }

    private Panel BuildWorklistButtonBar()
    {
        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 4, 0, 4) };
        _btnWlQuery = new Button { Text = "Query Worklist", Width = 110, Height = 28, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnWlClear = new Button { Text = "Clear Results", Width = 90, Height = 28 };
        _btnWlExport = new Button { Text = "Export CSV...", Width = 90, Height = 28 };
        _lblWlCount = new Label { Text = "0 result(s)", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        flow.Controls.Add(_btnWlQuery);
        flow.Controls.Add(MakeSpacer(6));
        flow.Controls.Add(_btnWlClear);
        flow.Controls.Add(MakeSpacer(6));
        flow.Controls.Add(_btnWlExport);
        flow.Controls.Add(MakeSpacer(16));
        flow.Controls.Add(_lblWlCount);
        return flow;
    }

    private DataGridView BuildWorklistGrid()
    {
        _dgvWorklist = MakeGrid();
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_PatName", HeaderText = "Patient Name", Width = 140 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_PatID", HeaderText = "Patient ID", Width = 90 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_DOB", HeaderText = "DOB", Width = 75 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_Sex", HeaderText = "Sex", Width = 40 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_Accession", HeaderText = "Accession", Width = 100 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_SchedDate", HeaderText = "Sched. Date", Width = 90 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_SchedTime", HeaderText = "Sched. Time", Width = 80 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_Modality", HeaderText = "Modality", Width = 70 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_StationAE", HeaderText = "Station AE", Width = 90 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_StationName", HeaderText = "Station Name", Width = 90 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_Physician", HeaderText = "Performing Physician", Width = 140 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_ReqProcDesc", HeaderText = "Req. Proc. Description", Width = 160 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_StepDesc", HeaderText = "Step Description", Width = 160, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_StepID", HeaderText = "Step ID", Width = 80 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_ProtocolCode", HeaderText = "Protocol Code", Width = 100 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_ProtocolMeaning", HeaderText = "Protocol Meaning", Width = 140 });
        _dgvWorklist.Columns.Add(new DataGridViewTextBoxColumn { Name = "WL_StudyUID", HeaderText = "Study Instance UID", Width = 260 });
        return _dgvWorklist;
    }

    // ── Log Group ─────────────────────────────────────────────────

    private GroupBox BuildLogGroup()
    {
        var grp = new GroupBox { Text = "Log", Dock = DockStyle.Fill, Padding = new Padding(6) };

        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var btnBar = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        var btnClear = new Button { Text = "Clear Log", Width = 72, Height = 22 };
        btnClear.Click += (_, _) => _rtbLog.Clear();
        btnBar.Controls.Add(btnClear);
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

    // ── Grid factory ──────────────────────────────────────────────

    private static DataGridView MakeGrid() => new DataGridView
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = true,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
        RowHeadersVisible = false,
        BorderStyle = BorderStyle.None,
        GridColor = Color.FromArgb(60, 60, 60),
        BackgroundColor = SystemColors.Window
    };
}
