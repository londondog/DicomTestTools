using System.Drawing;
using FellowOakDicom;

namespace DicomScuTestTool;

public partial class MainForm
{
    private void AddCustomTagRow()
    {
        var row = new DataGridViewRow();
        row.CreateCells(_dgvCustomTags, "", "", "");
        _dgvCustomTags.Rows.Add(row);
        _dgvCustomTags.CurrentCell = _dgvCustomTags.Rows[^1].Cells[0];
        _dgvCustomTags.BeginEdit(true);
    }

    private void RemoveSelectedCustomTagRow()
    {
        if (_dgvCustomTags.CurrentRow != null)
            _dgvCustomTags.Rows.Remove(_dgvCustomTags.CurrentRow);
    }

    private List<CustomTagOverride> GetCustomTagOverrides()
    {
        var list = new List<CustomTagOverride>();
        foreach (DataGridViewRow row in _dgvCustomTags.Rows)
        {
            var tag = row.Cells["Tag"].Value?.ToString()?.Trim() ?? "";
            var vr = row.Cells["VR"].Value?.ToString()?.Trim() ?? "";
            var value = row.Cells["Value"].Value?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(tag))
                list.Add(new CustomTagOverride { Tag = tag, VR = vr, Value = value });
        }
        return list;
    }

    private void LoadCustomTagOverrides(List<CustomTagOverride> overrides)
    {
        _dgvCustomTags.Rows.Clear();
        foreach (var o in overrides)
        {
            var row = new DataGridViewRow();
            row.CreateCells(_dgvCustomTags, o.Tag, o.VR, o.Value);
            _dgvCustomTags.Rows.Add(row);
        }
    }

    private static void ApplyCustomTags(DicomDataset ds, List<CustomTagOverride> overrides)
    {
        foreach (var o in overrides)
        {
            try
            {
                var tag = ParseTag(o.Tag);
                if (tag == null) continue;

                DicomVR vr;
                if (!string.IsNullOrWhiteSpace(o.VR))
                {
                    vr = DicomVR.Parse(o.VR) ?? ResolveVR(tag);
                }
                else
                {
                    vr = ResolveVR(tag);
                }

                ds.AddOrUpdate(vr, tag, o.Value);
            }
            catch
            {
                // Skip tags that can't be applied
            }
        }
    }

    private static DicomTag? ParseTag(string tagStr)
    {
        tagStr = tagStr.Trim().Replace("(", "").Replace(")", "").Replace(" ", "");
        if (tagStr.Contains(','))
        {
            var parts = tagStr.Split(',');
            if (parts.Length == 2
                && ushort.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out var group)
                && ushort.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out var element))
            {
                return new DicomTag(group, element);
            }
        }
        return null;
    }

    private static DicomVR ResolveVR(DicomTag tag)
    {
        var entry = DicomDictionary.Default[tag];
        return entry?.ValueRepresentations?.FirstOrDefault() ?? DicomVR.LO;
    }
}
