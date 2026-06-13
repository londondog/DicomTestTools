using System.Drawing;

namespace DicomScuTestTool;

internal sealed class ProductionWarningDialog : Form
{
    private const string ProductionPassword = "Warning123!";

    private TextBox _txtPassword = null!;
    private Label _lblError = null!;

    internal ProductionWarningDialog()
    {
        Text = "Production Environment Warning";
        Size = new Size(460, 310);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        BuildUI();
    }

    private void BuildUI()
    {
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Color.FromArgb(180, 0, 0),
            Padding = new Padding(16, 0, 16, 0)
        };

        var lblTitle = new Label
        {
            Text = "⚠  PRODUCTION SYSTEM",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        headerPanel.Controls.Add(lblTitle);

        var bodyPanel = new Panel { Dock = DockStyle.Fill };

        var lblWarning = new Label
        {
            Text = "You are connected to a PRODUCTION system.\n" +
                   "Changes made here will affect live patient data.\n\n" +
                   "Enter the password to continue.",
            Location = new Point(16, 16),
            Size = new Size(420, 72),
            ForeColor = Color.FromArgb(180, 60, 0),
            Font = new Font("Segoe UI", 9.5f)
        };

        var lblPassword = new Label
        {
            Text = "Password:",
            Location = new Point(16, 96),
            AutoSize = true
        };

        _txtPassword = new TextBox
        {
            Location = new Point(16, 115),
            Size = new Size(420, 24),
            UseSystemPasswordChar = true
        };

        _lblError = new Label
        {
            Text = "Incorrect password. Try again.",
            ForeColor = Color.Red,
            Location = new Point(16, 143),
            AutoSize = true,
            Visible = false
        };

        var btnContinue = new Button
        {
            Text = "Continue",
            Location = new Point(240, 178),
            Size = new Size(90, 28)
        };

        var btnExit = new Button
        {
            Text = "Exit",
            Location = new Point(346, 178),
            Size = new Size(90, 28),
            DialogResult = DialogResult.Cancel
        };

        btnContinue.Click += BtnContinue_Click;
        _txtPassword.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) BtnContinue_Click(null, EventArgs.Empty);
        };

        bodyPanel.Controls.AddRange([lblWarning, lblPassword, _txtPassword, _lblError, btnContinue, btnExit]);
        Controls.AddRange([headerPanel, bodyPanel]);

        AcceptButton = btnContinue;
        CancelButton = btnExit;
        ActiveControl = _txtPassword;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _txtPassword.Focus();
    }

    private void BtnContinue_Click(object? sender, EventArgs e)
    {
        if (_txtPassword.Text == ProductionPassword)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            _lblError.Visible = true;
            _txtPassword.Clear();
            _txtPassword.Focus();
        }
    }
}
