using System.Runtime.InteropServices;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Render;

namespace Dicom.Viewer;

public class DicomImageViewerForm : Form
{
    private readonly string _filePath;
    private DicomDataset? _dataset;
    private DicomPixelData? _pixelData;
    private int _frameCount;
    private int _currentFrame;
    private readonly System.Windows.Forms.Timer _timer = new();

    private PictureBox _pictureBox = null!;
    private TrackBar _frameSlider = null!;
    private Label _frameLabel = null!;
    private Label _infoLabel = null!;
    private Button _playPauseBtn = null!;
    private NumericUpDown _fpsSpinner = null!;

    public DicomImageViewerForm(string filePath)
    {
        _filePath = filePath;
        InitializeComponent();
        LoadFile();
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        Text = $"DICOM Image — {Path.GetFileName(_filePath)}";
        Size = new Size(900, 780);
        MinimumSize = new Size(600, 500);
        BackColor = Color.FromArgb(20, 20, 20);

        var infoBar = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.FromArgb(37, 37, 38) };
        var tagsBtn = new Button
        {
            Text = "DICOM Tags...",
            Dock = DockStyle.Right,
            Width = 110,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(60, 60, 60),
            Font = new Font("Segoe UI", 8.5f)
        };
        tagsBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        tagsBtn.Click += (_, _) => new DicomTagsForm(_filePath).ShowDialog(this);
        _infoLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.LightGray,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            Padding = new Padding(8, 0, 0, 0)
        };
        infoBar.Controls.Add(_infoLabel);
        infoBar.Controls.Add(tagsBtn);

        _pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };

        var controlBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(37, 37, 38)
        };

        var prevBtn = MakeBtn("◀", 8);
        prevBtn.Click += (_, _) => { Stop(); ShowFrame(_currentFrame - 1); };

        _playPauseBtn = MakeBtn("▶  Play", 50);
        _playPauseBtn.Width = 80;
        _playPauseBtn.Click += TogglePlay;

        var nextBtn = MakeBtn("▶", 138);
        nextBtn.Click += (_, _) => { Stop(); ShowFrame(_currentFrame + 1); };

        _frameLabel = new Label
        {
            Location = new Point(186, 10),
            Size = new Size(100, 20),
            ForeColor = Color.LightGray,
            Font = new Font("Consolas", 9f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var fpsLabel = new Label
        {
            Location = new Point(294, 10),
            Size = new Size(36, 20),
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleRight,
            Text = "FPS:"
        };

        _fpsSpinner = new NumericUpDown
        {
            Location = new Point(334, 8),
            Size = new Size(54, 22),
            Minimum = 1,
            Maximum = 120,
            Value = 25,
            BackColor = Color.FromArgb(55, 55, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _fpsSpinner.ValueChanged += (_, _) => { if (_timer.Enabled) UpdateTimerInterval(); };

        _frameSlider = new TrackBar
        {
            Location = new Point(8, 32),
            Size = new Size(860, 22),
            Minimum = 0,
            Maximum = 0,
            TickStyle = TickStyle.None,
            BackColor = Color.FromArgb(37, 37, 38)
        };
        _frameSlider.ValueChanged += OnSliderChanged;

        controlBar.Controls.AddRange(new Control[]
            { prevBtn, _playPauseBtn, nextBtn, _frameLabel, fpsLabel, _fpsSpinner, _frameSlider });

        _timer.Tick += (_, _) => ShowFrame((_currentFrame + 1) % _frameCount);
        Resize += (_, _) => _frameSlider.Width = controlBar.Width - 16;
        FormClosing += (_, _) => { _timer.Stop(); _pictureBox.Image?.Dispose(); };

        Controls.Add(_pictureBox);
        Controls.Add(controlBar);
        Controls.Add(infoBar);
        ResumeLayout();
    }

    private static Button MakeBtn(string text, int x)
    {
        var b = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(60, 60, 60),
            Size = new Size(36, 22),
            Location = new Point(x, 6)
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        return b;
    }

    private void LoadFile()
    {
        try
        {
            var file = DicomFile.Open(_filePath);
            _dataset = file.Dataset;
            Log($"Opened {_filePath}  TransferSyntax={file.Dataset.InternalTransferSyntax?.UID?.Name}");
            _pixelData = DicomPixelData.Create(_dataset);
            _frameCount = _pixelData.NumberOfFrames;

            var patient  = _dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
            var modality = _dataset.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);
            var date     = _dataset.GetSingleValueOrDefault(DicomTag.StudyDate, string.Empty);
            var desc     = _dataset.GetSingleValueOrDefault(DicomTag.SeriesDescription,
                           _dataset.GetSingleValueOrDefault(DicomTag.StudyDescription, string.Empty));

            _infoLabel.Text = string.Join("   ",
                new[] { patient, modality, date, desc,
                    $"{_pixelData.Width}×{_pixelData.Height}",
                    $"{_pixelData.BitsStored}-bit",
                    $"{_pixelData.PhotometricInterpretation}",
                    $"{_frameCount} frame(s)" }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            var frameTimeMs = _dataset.GetSingleValueOrDefault(DicomTag.FrameTime, 0.0);
            if (frameTimeMs > 0)
                _fpsSpinner.Value = Math.Clamp((int)Math.Round(1000.0 / frameTimeMs), 1, 120);

            _frameSlider.Maximum = Math.Max(0, _frameCount - 1);
            ShowFrame(0);
            Text = $"DICOM Image — {Path.GetFileName(_filePath)}  [{modality}]";
        }
        catch (Exception ex)
        {
            var msg = $"{ex.GetType().Name}: {ex.Message}";
            _infoLabel.Text = $"Error: {msg}";
            Log(ex.ToString());
            MessageBox.Show($"Failed to open DICOM image:\n\n{msg}\n\nSee viewer.log for details.",
                "DICOM Image Viewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void Log(string text)
    {
        try
        {
            var log = Path.Combine(AppContext.BaseDirectory, "viewer.log");
            File.AppendAllText(log, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}\n\n");
        }
        catch { }
    }

    private void ShowFrame(int frameIndex)
    {
        if (_dataset is null || _pixelData is null) return;
        frameIndex = Math.Clamp(frameIndex, 0, Math.Max(0, _frameCount - 1));

        try
        {
            var bmp = RenderFrame(frameIndex);
            var old = _pictureBox.Image;
            _pictureBox.Image = bmp;
            old?.Dispose();

            _currentFrame = frameIndex;
            _frameLabel.Text = $"Frame {frameIndex + 1} / {_frameCount}";

            if (_frameSlider.Value != frameIndex)
            {
                _frameSlider.ValueChanged -= OnSliderChanged;
                _frameSlider.Value = frameIndex;
                _frameSlider.ValueChanged += OnSliderChanged;
            }
        }
        catch (Exception ex)
        {
            _infoLabel.Text = $"Render error frame {frameIndex}: {ex.Message}";
            Log($"RenderFrame {frameIndex} failed: {ex}");
        }
    }

    private Bitmap RenderFrame(int frameIndex)
    {
        var dicomImage = new DicomImage(_dataset!);
        var rendered = (RawImage)dicomImage.RenderImage(frameIndex);
        int w = dicomImage.Width, h = dicomImage.Height;

        var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var bd  = bmp.LockBits(new Rectangle(0, 0, w, h),
            System.Drawing.Imaging.ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        Marshal.Copy(rendered.Pixels.Data, 0, bd.Scan0, w * h);
        bmp.UnlockBits(bd);
        return bmp;
    }

    private void TogglePlay(object? sender, EventArgs e)
    {
        if (_timer.Enabled) Stop();
        else if (_frameCount > 1) { UpdateTimerInterval(); _timer.Start(); _playPauseBtn.Text = "⏸  Pause"; }
    }

    private void Stop()
    {
        _timer.Stop();
        _playPauseBtn.Text = "▶  Play";
    }

    private void OnSliderChanged(object? sender, EventArgs e)
    {
        if (_frameSlider.Value != _currentFrame)
            ShowFrame(_frameSlider.Value);
    }

    private void UpdateTimerInterval() =>
        _timer.Interval = Math.Max(1, (int)(1000.0 / (double)_fpsSpinner.Value));
}
