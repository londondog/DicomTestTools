using System.Drawing;
using FellowOakDicom;

namespace DicomScuTestTool;

public partial class MainForm
{
    private void BtnAddFiles_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select DICOM files",
            Filter = "DICOM files (*.dcm;*.vid;*.img)|*.dcm;*.vid;*.img|All files (*.*)|*.*",
            Multiselect = true
        };
        if (dlg.ShowDialog() == DialogResult.OK)
            LoadFiles(dlg.FileNames);
    }

    private void BtnAddFolder_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { Description = "Select folder containing DICOM files" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var files = Directory.GetFiles(dlg.SelectedPath, "*.dcm", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            // Fall back to all files, excluding common non-DICOM extensions
            files = Directory.GetFiles(dlg.SelectedPath, "*", SearchOption.AllDirectories)
                .Where(f => !Path.GetExtension(f).Equals(".json", StringComparison.OrdinalIgnoreCase)
                            && !Path.GetExtension(f).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
        LoadFiles(files);
    }

    private void LoadFiles(IEnumerable<string> paths)
    {
        int added = 0, skipped = 0;
        foreach (var path in paths)
        {
            if (_files.Any(f => f.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
            { skipped++; continue; }

            try
            {
                var dcm = DicomFile.Open(path);
                var ds = dcm.Dataset;
                var entry = new DicomFileEntry
                {
                    FilePath = path,
                    PatientName = ds.GetSingleValueOrDefault(DicomTag.PatientName, ""),
                    PatientID = ds.GetSingleValueOrDefault(DicomTag.PatientID, ""),
                    AccessionNumber = ds.GetSingleValueOrDefault(DicomTag.AccessionNumber, ""),
                    StudyDate = ds.GetSingleValueOrDefault(DicomTag.StudyDate, ""),
                    StudyTime = ds.GetSingleValueOrDefault(DicomTag.StudyTime, ""),
                    Modality = ds.GetSingleValueOrDefault(DicomTag.Modality, ""),
                    StudyInstanceUID = ds.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, ""),
                    SeriesInstanceUID = ds.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, ""),
                    SOPInstanceUID = ds.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, ""),
                    DOB = ds.GetSingleValueOrDefault(DicomTag.PatientBirthDate, ""),
                    Sex = ds.GetSingleValueOrDefault(DicomTag.PatientSex, ""),
                    StudyDescription = ds.GetSingleValueOrDefault(DicomTag.StudyDescription, "")
                };

                var lvi = new ListViewItem((_files.Count + 1).ToString());
                lvi.SubItems.Add(Path.GetFileName(path));
                lvi.SubItems.Add(Path.GetExtension(path));
                lvi.SubItems.Add(entry.PatientName);
                lvi.SubItems.Add(entry.PatientID);
                lvi.SubItems.Add(entry.AccessionNumber);
                lvi.SubItems.Add(entry.StudyDate);
                lvi.SubItems.Add(entry.Modality);
                lvi.SubItems.Add("Pending");
                entry.ListViewItem = lvi;

                _files.Add(entry);
                _lvFiles.Items.Add(lvi);
                added++;
            }
            catch (Exception ex)
            {
                Log($"[WARN] Could not read {Path.GetFileName(path)}: {ex.Message}", Color.Orange);
                skipped++;
            }
        }

        RenumberList();
        UpdateFileCount();
        Log($"[INFO] Loaded {added} file(s){(skipped > 0 ? $", skipped {skipped}" : "")}.", Color.Cyan);

        // Pre-populate demographics from first file if fields are empty
        if (added > 0 && string.IsNullOrWhiteSpace(_txtPatientName.Text))
            PopulateDemographicsFromEntry(_files[0]);
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        var selected = _lvFiles.SelectedItems.Cast<ListViewItem>().ToList();
        foreach (var lvi in selected)
        {
            var entry = _files.FirstOrDefault(f => f.ListViewItem == lvi);
            if (entry != null) _files.Remove(entry);
            _lvFiles.Items.Remove(lvi);
        }
        RenumberList();
        UpdateFileCount();
    }

    private void ClearFiles()
    {
        _files.Clear();
        _lvFiles.Items.Clear();
        UpdateFileCount();
        Log("[INFO] File list cleared.", Color.Gray);
    }

    private void RenumberList()
    {
        for (int i = 0; i < _lvFiles.Items.Count; i++)
            _lvFiles.Items[i].Text = (i + 1).ToString();
    }

    private void UpdateFileCount()
    {
        _lblFileCount.Text = $"{_files.Count} file(s)";
        UpdateButtonStates();
    }

    private static readonly string[] _lvColumnNames =
        { "#", "Filename", "Ext", "Patient Name", "Patient ID", "Accession", "Study Date", "Modality", "Status" };

    internal void LvFiles_ColumnClick(object? sender, ColumnClickEventArgs e)
    {
        if (_sortColumn == e.Column)
            _sortDescending = !_sortDescending;
        else
        {
            _sortColumn = e.Column;
            _sortDescending = false;
        }

        for (int i = 0; i < _lvFiles.Columns.Count; i++)
            _lvFiles.Columns[i].Text = _lvColumnNames[i] + (i == _sortColumn ? (_sortDescending ? " ▼" : " ▲") : "");

        _lvFiles.ListViewItemSorter = new ListViewSorter(_sortColumn, _sortDescending);
    }

    private sealed class ListViewSorter : System.Collections.IComparer
    {
        private readonly int _col;
        private readonly bool _desc;

        public ListViewSorter(int col, bool desc) { _col = col; _desc = desc; }

        public int Compare(object? x, object? y)
        {
            var a = (ListViewItem)x!;
            var b = (ListViewItem)y!;
            string sa = _col < a.SubItems.Count ? a.SubItems[_col].Text : "";
            string sb = _col < b.SubItems.Count ? b.SubItems[_col].Text : "";

            int result = _col == 0 && int.TryParse(sa, out int ia) && int.TryParse(sb, out int ib)
                ? ia.CompareTo(ib)
                : string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);

            return _desc ? -result : result;
        }
    }
}
