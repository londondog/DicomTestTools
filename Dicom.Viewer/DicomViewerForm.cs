namespace Dicom.Viewer;

public class DicomViewerForm : Form
{
    private string? _loadedFilePath;
    private Button _dicomTagsBtn = null!;
    private static string? _lastOpenDir;

    public DicomViewerForm(string? initialFile = null)
    {
        InitializeComponent();
        if (initialFile is not null)
            OpenFile(initialFile);
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        Text = "DICOM Viewer";
        Size = new Size(700, 500);
        MinimumSize = new Size(500, 350);
        BackColor = Color.FromArgb(45, 45, 48);
        Icon = SystemIcons.Application;

        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.FromArgb(37, 37, 38),
            Padding = new Padding(8, 4, 8, 4)
        };

        var openBtn = MakeToolbarButton("Open File...", Color.FromArgb(0, 120, 212), 8, 100);
        openBtn.Click += OnOpenFile;

        _dicomTagsBtn = MakeToolbarButton("DICOM Tags...", Color.FromArgb(80, 80, 85), 116, 110);
        _dicomTagsBtn.Enabled = false;
        _dicomTagsBtn.Click += (_, _) => new DicomTagsForm(_loadedFilePath!).ShowDialog(this);

        toolbar.Controls.Add(openBtn);
        toolbar.Controls.Add(_dicomTagsBtn);

        var placeholder = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(100, 100, 100),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12f),
            Text = "Open a DICOM file to view it"
        };

        Controls.Add(placeholder);
        Controls.Add(toolbar);
        ResumeLayout();
    }

    private static Button MakeToolbarButton(string text, Color back, int x, int width)
    {
        var btn = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = back,
            Size = new Size(width, 26),
            Location = new Point(x, 5)
        };
        btn.FlatAppearance.BorderColor = ControlPaint.Dark(back);
        return btn;
    }

    private void OnOpenFile(object? sender, EventArgs e)
    {
        var initialDir = _lastOpenDir;
        if (initialDir is null || !Directory.Exists(initialDir))
            initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        using var dlg = new OpenFileDialog
        {
            Title = "Open DICOM File",
            Filter = "DICOM files (*.dcm)|*.dcm|All files (*.*)|*.*",
            InitialDirectory = initialDir
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _lastOpenDir = Path.GetDirectoryName(dlg.FileName);
        OpenFile(dlg.FileName);
    }

    private void OpenFile(string filePath)
    {
        _loadedFilePath = filePath;
        _dicomTagsBtn.Enabled = true;
        new DicomImageViewerForm(filePath).Show();
    }
}
