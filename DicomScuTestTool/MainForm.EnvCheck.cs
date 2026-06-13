using System.Drawing;
using Microsoft.Data.SqlClient;

namespace DicomScuTestTool;

public partial class MainForm
{
    private async Task CheckProductionEnvironmentAsync()
    {
        var index = await GetEnvironmentIndexAsync();

        if (index == 8)
        {
            Log("[ENV] Production environment detected — prompting for authorisation.", Color.OrangeRed);
            using var dlg = new ProductionWarningDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                Application.Exit();
                return;
            }
            Log("[ENV] Production access authorised.", Color.OrangeRed);
        }
        else if (index == 7)
        {
            Log("[ENV] Test environment confirmed.", Color.LightGreen);
        }
        else
        {
            Log("[ENV] Environment could not be confirmed — prompting for authorisation.", Color.OrangeRed);
            using var dlg = new ProductionWarningDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                Application.Exit();
                return;
            }
            Log("[ENV] Unconfirmed environment access authorised.", Color.OrangeRed);
        }
    }

    private async Task<int?> GetEnvironmentIndexAsync()
    {
        var connectionString = _txtLookupConnectionString.Text.Trim();
        if (string.IsNullOrWhiteSpace(connectionString)) return null;

        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(
                "SELECT field_value FROM T_SITE_CONFIGURATION WHERE field_name = 'Site.Config.EnvironmentIndex'",
                conn);
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return null;
            return int.TryParse(result.ToString(), out var v) ? v : null;
        }
        catch (Exception ex)
        {
            Log($"[ENV] Could not check environment index: {ex.Message}", Color.Yellow);
            return null;
        }
    }
}
