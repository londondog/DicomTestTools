using FellowOakDicom;

namespace Dicom.Viewer;

public class DicomTagsForm : Form
{
    private readonly string _filePath;

    private TreeView _tree = null!;
    private Label _statusLabel = null!;
    private ListView _patientList = null!;
    private ListView _studyList = null!;
    private ListView _seriesList = null!;

    public DicomTagsForm(string filePath)
    {
        _filePath = filePath;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        Text = $"DICOM Tags — {Path.GetFileName(_filePath)}";
        Size = new Size(820, 650);
        MinimumSize = new Size(600, 400);
        BackColor = Color.FromArgb(30, 30, 30);

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(130, 24),
            Font = new Font("Segoe UI", 9f)
        };
        tabs.DrawItem += DrawTab;

        // Patient tab
        _patientList = BuildMetaList();
        var patientTab = new TabPage("Patient") { BackColor = Color.FromArgb(22, 22, 22), Padding = new Padding(0) };
        patientTab.Controls.Add(_patientList);

        // Study tab
        _studyList = BuildMetaList();
        var studyTab = new TabPage("Study") { BackColor = Color.FromArgb(22, 22, 22), Padding = new Padding(0) };
        studyTab.Controls.Add(_studyList);

        // Series / Instance tab
        _seriesList = BuildMetaList();
        var seriesTab = new TabPage("Series / Instance") { BackColor = Color.FromArgb(22, 22, 22), Padding = new Padding(0) };
        seriesTab.Controls.Add(_seriesList);

        // All Tags tab — toolbar + tree
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = Color.FromArgb(37, 37, 38),
            Padding = new Padding(6, 4, 6, 0)
        };

        var searchBox = new TextBox
        {
            Width = 200,
            Height = 22,
            Location = new Point(6, 5),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Search tags..."
        };
        searchBox.TextChanged += (_, _) => FilterNodes(searchBox.Text);

        var expandBtn = new Button
        {
            Text = "Expand All",
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(60, 60, 60),
            Size = new Size(85, 22),
            Location = new Point(214, 5)
        };
        expandBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        expandBtn.Click += (_, _) => _tree.ExpandAll();

        var collapseBtn = new Button
        {
            Text = "Collapse All",
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(60, 60, 60),
            Size = new Size(85, 22),
            Location = new Point(306, 5)
        };
        collapseBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        collapseBtn.Click += (_, _) => _tree.CollapseAll();

        toolbar.Controls.Add(searchBox);
        toolbar.Controls.Add(expandBtn);
        toolbar.Controls.Add(collapseBtn);

        _statusLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 22,
            BackColor = Color.FromArgb(37, 37, 38),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0),
            Font = new Font("Consolas", 8f)
        };

        _tree = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(22, 22, 22),
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Consolas", 8.5f),
            BorderStyle = BorderStyle.None,
            ShowLines = true,
            ShowPlusMinus = true,
            FullRowSelect = true,
            HideSelection = false
        };
        _tree.AfterSelect += (_, e) => _statusLabel.Text = e.Node?.Text ?? "";

        var allTagsPanel = new Panel { Dock = DockStyle.Fill };
        allTagsPanel.Controls.Add(_tree);
        allTagsPanel.Controls.Add(_statusLabel);
        allTagsPanel.Controls.Add(toolbar);

        var allTagsTab = new TabPage("All Tags") { BackColor = Color.FromArgb(22, 22, 22), Padding = new Padding(0) };
        allTagsTab.Controls.Add(allTagsPanel);

        tabs.TabPages.AddRange(new TabPage[] { patientTab, studyTab, seriesTab, allTagsTab });

        Controls.Add(tabs);
        ResumeLayout();
    }

    private void LoadData()
    {
        try
        {
            var file = DicomFile.Open(_filePath);
            var ds = file.Dataset;

            PopulateMetadata(ds);

            _tree.BeginUpdate();

            var metaNode = _tree.Nodes.Add("File Meta Information");
            metaNode.ForeColor = Color.FromArgb(150, 200, 255);
            AddDataset(metaNode.Nodes, file.FileMetaInfo);

            var dataNode = _tree.Nodes.Add("Dataset");
            dataNode.ForeColor = Color.FromArgb(150, 200, 255);
            AddDataset(dataNode.Nodes, ds);

            metaNode.Expand();
            dataNode.Expand();

            _statusLabel.Text = $"{CountNodes(_tree.Nodes)} tags";
            _tree.EndUpdate();
        }
        catch (Exception ex)
        {
            _tree.Nodes.Add($"Error loading file: {ex.Message}").ForeColor = Color.Red;
        }
    }

    private void PopulateMetadata(DicomDataset ds)
    {
        string Get(DicomTag tag) => ds.GetSingleValueOrDefault(tag, string.Empty);

        // Patient
        AddRow(_patientList, "Patient Name",  Get(DicomTag.PatientName));
        AddRow(_patientList, "Patient ID",    Get(DicomTag.PatientID));
        AddRow(_patientList, "Date of Birth", FormatDate(Get(DicomTag.PatientBirthDate)));
        AddRow(_patientList, "Sex",           Get(DicomTag.PatientSex));
        AddRow(_patientList, "Age",           Get(DicomTag.PatientAge));
        AddRow(_patientList, "Weight",        Suffix(Get(DicomTag.PatientWeight), "kg"));
        AddRow(_patientList, "Height",        Suffix(Get(DicomTag.PatientSize), "m"));
        AddRow(_patientList, "Comments",      Get(DicomTag.PatientComments));

        // Study
        AddRow(_studyList, "Study Date",          FormatDate(Get(DicomTag.StudyDate)));
        AddRow(_studyList, "Study Time",          FormatTime(Get(DicomTag.StudyTime)));
        AddRow(_studyList, "Description",         Get(DicomTag.StudyDescription));
        AddRow(_studyList, "Accession Number",    Get(DicomTag.AccessionNumber));
        AddRow(_studyList, "Referring Physician", Get(DicomTag.ReferringPhysicianName));
        AddRow(_studyList, "Study ID",            Get(DicomTag.StudyID));
        AddRow(_studyList, "Institution",         Get(DicomTag.InstitutionName));
        AddRow(_studyList, "Study Instance UID",  Get(DicomTag.StudyInstanceUID));

        // Series / Instance
        AddRow(_seriesList, "Modality",            Get(DicomTag.Modality));
        AddRow(_seriesList, "Series Description",  Get(DicomTag.SeriesDescription));
        AddRow(_seriesList, "Series Number",       Get(DicomTag.SeriesNumber));
        AddRow(_seriesList, "Series Date",         FormatDate(Get(DicomTag.SeriesDate)));
        AddRow(_seriesList, "Body Part",           Get(DicomTag.BodyPartExamined));
        AddRow(_seriesList, "Patient Position",    Get(DicomTag.PatientPosition));
        AddRow(_seriesList, "Protocol Name",       Get(DicomTag.ProtocolName));
        AddRow(_seriesList, "Series Instance UID", Get(DicomTag.SeriesInstanceUID));
        _seriesList.Items.Add(new ListViewItem("") { BackColor = Color.FromArgb(35, 35, 35) });
        AddRow(_seriesList, "Instance Number",     Get(DicomTag.InstanceNumber));
        AddRow(_seriesList, "SOP Class",           Get(DicomTag.SOPClassUID));
        AddRow(_seriesList, "SOP Instance UID",    Get(DicomTag.SOPInstanceUID));
        AddRow(_seriesList, "Transfer Syntax",     ds.InternalTransferSyntax?.UID?.Name ?? "");
        if (ds.TryGetSingleValue(DicomTag.Rows, out ushort rows) &&
            ds.TryGetSingleValue(DicomTag.Columns, out ushort cols))
        {
            _seriesList.Items.Add(new ListViewItem("") { BackColor = Color.FromArgb(35, 35, 35) });
            AddRow(_seriesList, "Dimensions",  $"{cols} × {rows}");
            AddRow(_seriesList, "Bits Stored", Get(DicomTag.BitsStored));
            AddRow(_seriesList, "Photometric", Get(DicomTag.PhotometricInterpretation));
            AddRow(_seriesList, "Frames",      ds.GetSingleValueOrDefault(DicomTag.NumberOfFrames, "1"));
        }
    }

    private static ListView BuildMetaList()
    {
        var lv = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            BackColor = Color.FromArgb(22, 22, 22),
            ForeColor = Color.FromArgb(210, 210, 210),
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9f),
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        lv.Columns.Add("Field", 200);
        lv.Columns.Add("Value", 500);
        return lv;
    }

    private static void AddRow(ListView lv, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var item = new ListViewItem(field) { ForeColor = Color.FromArgb(160, 160, 200) };
        item.SubItems.Add(value);
        lv.Items.Add(item);
    }

    private static string FormatDate(string raw) =>
        raw.Length == 8 ? $"{raw[..4]}-{raw[4..6]}-{raw[6..8]}" : raw;

    private static string FormatTime(string raw) =>
        raw.Length >= 6 ? $"{raw[..2]}:{raw[2..4]}:{raw[4..6]}" : raw;

    private static string Suffix(string raw, string unit) =>
        string.IsNullOrWhiteSpace(raw) ? "" : $"{raw} {unit}";

    private static void DrawTab(object? sender, DrawItemEventArgs e)
    {
        var tc = (TabControl)sender!;
        var tab = tc.TabPages[e.Index];
        var isSelected = e.Index == tc.SelectedIndex;
        using var brush = new SolidBrush(isSelected ? Color.FromArgb(45, 45, 48) : Color.FromArgb(30, 30, 30));
        e.Graphics.FillRectangle(brush, e.Bounds);
        TextRenderer.DrawText(e.Graphics, tab.Text, tc.Font, e.Bounds,
            isSelected ? Color.White : Color.FromArgb(160, 160, 160),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static void AddDataset(TreeNodeCollection nodes, DicomDataset ds)
    {
        foreach (var item in ds)
        {
            var tag  = item.Tag;
            var name = tag.DictionaryEntry?.Name ?? "Private";
            var vr   = item.ValueRepresentation.Code;

            if (item is DicomSequence seq)
            {
                var node = nodes.Add($"({tag.Group:X4},{tag.Element:X4}) {name}  [{vr}]  {seq.Items.Count} item(s)");
                node.ForeColor = Color.FromArgb(255, 210, 120);
                for (int i = 0; i < seq.Items.Count; i++)
                {
                    var itemNode = node.Nodes.Add($"Item {i}");
                    itemNode.ForeColor = Color.FromArgb(160, 160, 160);
                    AddDataset(itemNode.Nodes, seq.Items[i]);
                }
            }
            else
            {
                var value = GetTagValue(item);
                var node = nodes.Add($"({tag.Group:X4},{tag.Element:X4}) {name}  [{vr}]  =  {value}");
                node.ForeColor = Color.FromArgb(200, 230, 200);
            }
        }
    }

    private static string GetTagValue(DicomItem item)
    {
        try
        {
            if (item is DicomElement el)
            {
                if (el.Count == 0) return "(empty)";
                if (el is DicomOtherByte or DicomOtherWord or DicomOtherFloat or DicomOtherDouble or DicomOtherLong)
                    return $"(binary, {el.Buffer?.Size ?? 0} bytes)";
                var vals = el.Get<string[]>();
                if (vals is null || vals.Length == 0) return "(empty)";
                var joined = string.Join(" / ", vals.Take(8));
                return vals.Length > 8 ? $"{joined} ... ({vals.Length} values)" : joined;
            }
        }
        catch { }
        return "(unreadable)";
    }

    private void FilterNodes(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            LoadData();
            return;
        }

        _tree.BeginUpdate();
        SetNodeVisibility(_tree.Nodes, search.Trim().ToLower());
        _tree.ExpandAll();
        _tree.EndUpdate();
    }

    private static bool SetNodeVisibility(TreeNodeCollection nodes, string search)
    {
        var anyMatch = false;
        foreach (TreeNode node in nodes)
        {
            var childMatch = SetNodeVisibility(node.Nodes, search);
            var selfMatch  = node.Text.Contains(search, StringComparison.OrdinalIgnoreCase);
            node.ForeColor = selfMatch
                ? Color.Yellow
                : (childMatch ? Color.FromArgb(200, 230, 200) : Color.FromArgb(80, 80, 80));
            anyMatch |= selfMatch || childMatch;
        }
        return anyMatch;
    }

    private static int CountNodes(TreeNodeCollection nodes)
    {
        var count = 0;
        foreach (TreeNode n in nodes)
            count += 1 + CountNodes(n.Nodes);
        return count;
    }
}
