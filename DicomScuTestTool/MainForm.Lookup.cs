using System.Drawing;
using Microsoft.Data.SqlClient;

namespace DicomScuTestTool;

public partial class MainForm
{
    private void UpdateLookupTrustServerCertificateInConnectionString()
    {
        try
        {
            var current = _txtLookupConnectionString.Text.Trim();
            var builder = new SqlConnectionStringBuilder();

            if (!string.IsNullOrWhiteSpace(current))
            {
                builder.ConnectionString = current;
            }
            else
            {
                builder.DataSource = "localhost";
                builder.InitialCatalog = "Medcon";
                builder.IntegratedSecurity = true;
            }

            builder.TrustServerCertificate = _chkLookupTrustServerCert.Checked;
            _txtLookupConnectionString.Text = builder.ConnectionString;
        }
        catch
        {
            // If user typed an invalid string, normalize to a known-good local default.
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = "localhost",
                InitialCatalog = "Medcon",
                IntegratedSecurity = true,
                TrustServerCertificate = _chkLookupTrustServerCert.Checked
            };
            _txtLookupConnectionString.Text = builder.ConnectionString;
        }
    }

    private async Task TestLookupConnectionAsync()
    {
        var connectionString = _txtLookupConnectionString.Text.Trim();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show("Enter a SQL Server connection string first.", "Lookup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _btnLookupTestConnection.Enabled = false;
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            Log("[LOOKUP] Database connection successful.", Color.LightGreen);
            MessageBox.Show("Connection successful.", "Lookup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"[LOOKUP] Connection failed: {ex.Message}", Color.OrangeRed);
            MessageBox.Show($"Connection failed:\n{ex.Message}", "Lookup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnLookupTestConnection.Enabled = true;
        }
    }

    private async Task SearchPatientsAsync()
    {
        const string query = @"
            SELECT TOP (@MaxResults)
                unique_id_str,
                patient_name,
                first_name,
                last_name,
                date_of_birth,
                patient_sex,
                DATEDIFF(YEAR, date_of_birth, GETDATE()) AS patient_age
            FROM files
            WHERE unique_id_str LIKE @PatientIdLike
              AND patient_name LIKE @NameLike
            ORDER BY unique_id_str ASC";

        var connectionString = _txtLookupConnectionString.Text.Trim();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show("Enter a SQL Server connection string first.", "Lookup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _btnLookupPatients.Enabled = false;

        try
        {
            var patientIdSearch = _txtLookupPatientId.Text.Trim();
            var patientNameSearch = _txtLookupPatientName.Text.Trim();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@MaxResults", 100);
            command.Parameters.AddWithValue("@PatientIdLike", $"%{patientIdSearch}%");
            command.Parameters.AddWithValue("@NameLike", $"%{patientNameSearch}%");

            var results = new List<PatientLookupResult>();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var sex = reader["patient_sex"]?.ToString() ?? string.Empty;
                results.Add(new PatientLookupResult
                {
                    PatientId = reader["unique_id_str"]?.ToString() ?? string.Empty,
                    FullName = reader["patient_name"]?.ToString() ?? string.Empty,
                    FirstName = reader["first_name"]?.ToString() ?? string.Empty,
                    LastName = reader["last_name"]?.ToString() ?? string.Empty,
                    DateOfBirth = reader["date_of_birth"] as DateTime?,
                    Sex = sex,
                    Age = reader["patient_age"] as int? ?? 0
                });
            }

            _dgvLookupPatients.DataSource = results;
            Log($"[LOOKUP] Found {results.Count} patient(s).", Color.Cyan);
        }
        catch (Exception ex)
        {
            Log($"[LOOKUP] Patient search failed: {ex.Message}", Color.OrangeRed);
            MessageBox.Show($"Patient search failed:\n{ex.Message}", "Lookup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnLookupPatients.Enabled = true;
        }
    }

    private async Task SearchOrdersAsync()
    {
        const string query = @"
            SELECT TOP (@MaxResults)
                f.unique_id_str AS PatientId,
                f.last_name AS LastName,
                f.first_name AS FirstName,
                G.gender_name AS Gender,
                T.scheduled_Procedure_step_id AS ScheduledProcedureStepId,
                PT.code AS ProcedureCode,
                T.accession_number AS AccessionNumber,
                T.visit_id AS VisitId,
                A.start_time AS StartTime,
                W.workflow_status_name AS WorkflowStatus
            FROM T_TCS_PROCEDURE T
                JOIN T_SCHED_ALLOCATION A ON A.allocation_id = T.allocation_id
                JOIN files f ON f.file_id = T.patient_id
                JOIN T_TCS_WORKFLOW_STATUS W ON W.workflow_status_id = T.workflow_status_id
                JOIN T_TCS_GENDER G ON f.gender_id = G.gender_id
                LEFT JOIN T_TCS_PROCEDURE_TYPE PT ON PT.procedure_type_id = T.procedure_type_id
            WHERE T.workflow_status_id IN ('0', '7')
              AND f.unique_id_str LIKE @PatientIdLike
              AND f.patient_name LIKE @NameLike
              AND CAST(A.start_time AS DATE) BETWEEN @DateStart AND @DateEnd
            ORDER BY A.start_time DESC";

        var connectionString = _txtLookupConnectionString.Text.Trim();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show("Enter a SQL Server connection string first.", "Lookup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _btnLookupOrders.Enabled = false;

        try
        {
            var patientIdSearch = _txtLookupPatientId.Text.Trim();
            var patientNameSearch = _txtLookupPatientName.Text.Trim();
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-(int)_numLookupDays.Value);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@MaxResults", 200);
            command.Parameters.AddWithValue("@PatientIdLike", $"%{patientIdSearch}%");
            command.Parameters.AddWithValue("@NameLike", $"%{patientNameSearch}%");
            command.Parameters.AddWithValue("@DateStart", startDate.Date);
            command.Parameters.AddWithValue("@DateEnd", endDate.Date);

            var results = new List<OrderLookupResult>();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var firstName = reader["FirstName"]?.ToString() ?? string.Empty;
                var lastName = reader["LastName"]?.ToString() ?? string.Empty;

                results.Add(new OrderLookupResult
                {
                    PatientId = reader["PatientId"]?.ToString() ?? string.Empty,
                    PatientName = $"{lastName}, {firstName}".Trim(' ', ','),
                    FirstName = firstName,
                    LastName = lastName,
                    Gender = reader["Gender"]?.ToString() ?? string.Empty,
                    ScheduledProcedureStepId = reader["ScheduledProcedureStepId"]?.ToString() ?? string.Empty,
                    ProcedureCode = reader["ProcedureCode"]?.ToString() ?? string.Empty,
                    AccessionNumber = reader["AccessionNumber"]?.ToString() ?? string.Empty,
                    VisitId = reader["VisitId"] as int? ?? 0,
                    StartTime = reader["StartTime"] as DateTime?,
                    WorkflowStatus = reader["WorkflowStatus"]?.ToString() ?? string.Empty
                });
            }

            _dgvLookupOrders.DataSource = results;
            Log($"[LOOKUP] Found {results.Count} order(s) from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}.", Color.Cyan);
        }
        catch (Exception ex)
        {
            Log($"[LOOKUP] Order search failed: {ex.Message}", Color.OrangeRed);
            MessageBox.Show($"Order search failed:\n{ex.Message}", "Lookup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnLookupOrders.Enabled = true;
        }
    }

    private void ApplySelectedPatientToDemographics()
    {
        if (_dgvLookupPatients.CurrentRow?.DataBoundItem is not PatientLookupResult patient)
        {
            MessageBox.Show("Select a patient row first.", "Lookup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _chkOverride.Checked = true;
        _txtPatientID.Text = patient.PatientId;

        var patientName = BuildDicomPatientName(patient);
        if (!string.IsNullOrWhiteSpace(patientName))
        {
            _txtPatientName.Text = patientName;
        }

        if (patient.DateOfBirth.HasValue)
        {
            _txtDOB.Text = patient.DateOfBirth.Value.ToString("yyyyMMdd");
        }

        var dicomSex = ToDicomSex(patient.Sex);
        if (!string.IsNullOrWhiteSpace(dicomSex))
        {
            _cmbSex.SelectedItem = dicomSex;
        }

        Log($"[LOOKUP] Applied patient {patient.PatientId} to demographics.", Color.LightGreen);
    }

    private void ApplySelectedOrderToDemographics()
    {
        if (_dgvLookupOrders.CurrentRow?.DataBoundItem is not OrderLookupResult order)
        {
            MessageBox.Show("Select an order row first.", "Lookup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _chkOverride.Checked = true;
        _chkOverrideProcedure.Checked = true;

        if (!string.IsNullOrWhiteSpace(order.PatientId)) _txtPatientID.Text = order.PatientId;
        if (!string.IsNullOrWhiteSpace(order.AccessionNumber)) _txtAccession.Text = order.AccessionNumber;

        var patientName = BuildDicomPatientName(order.LastName, order.FirstName);
        if (!string.IsNullOrWhiteSpace(patientName)) _txtPatientName.Text = patientName;

        var dicomSex = ToDicomSex(order.Gender);
        if (!string.IsNullOrWhiteSpace(dicomSex)) _cmbSex.SelectedItem = dicomSex;

        if (order.StartTime.HasValue)
        {
            _txtStudyDate.Text = order.StartTime.Value.ToString("yyyyMMdd");
            _txtStudyTime.Text = order.StartTime.Value.ToString("HHmm");
        }

        var procText = !string.IsNullOrWhiteSpace(order.ProcedureCode)
            ? order.ProcedureCode
            : order.ScheduledProcedureStepId;

        if (!string.IsNullOrWhiteSpace(procText))
        {
            _txtStudyDesc.Text = procText;
            _txtProcedureDesc.Text = procText;
        }

        var mappedModality = GuessModality(order.ProcedureCode, order.ScheduledProcedureStepId);
        if (!string.IsNullOrWhiteSpace(mappedModality) && _cmbModality.Items.Contains(mappedModality))
        {
            _cmbModality.SelectedItem = mappedModality;
        }

        Log($"[LOOKUP] Applied order {order.AccessionNumber} to override fields.", Color.LightGreen);
    }

    private static string ToDicomSex(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.StartsWith("M")) return "M";
        if (normalized.StartsWith("F")) return "F";
        if (normalized.StartsWith("O")) return "O";
        return string.Empty;
    }

    private static string BuildDicomPatientName(PatientLookupResult patient)
    {
        return BuildDicomPatientName(patient.LastName, patient.FirstName, patient.FullName);
    }

    private static string BuildDicomPatientName(string lastName, string firstName, string fullName = "")
    {
        if (!string.IsNullOrWhiteSpace(lastName) || !string.IsNullOrWhiteSpace(firstName))
        {
            return $"{lastName}^{firstName}".Trim('^');
        }

        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;

        var parts = fullName.Split(',', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2) return $"{parts[0]}^{parts[1]}";

        var bySpace = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (bySpace.Length >= 2) return $"{bySpace[^1]}^{string.Join(" ", bySpace.Take(bySpace.Length - 1))}";

        return fullName;
    }

    private static string GuessModality(string procedureCode, string scheduledStepId)
    {
        var code = (procedureCode ?? string.Empty).Trim().ToUpperInvariant();
        var step = (scheduledStepId ?? string.Empty).Trim().ToUpperInvariant();
        var token = string.IsNullOrWhiteSpace(code) ? step : code;

        if (token.StartsWith("ECH") || token.Contains("ECHO")) return "US";
        if (token.StartsWith("CATH") || token.Contains("ANGIO")) return "XA";
        if (token.StartsWith("EP")) return "EP";
        if (token.StartsWith("ECG") || token.Contains("HOLTER") || token.Contains("STRESS ECG")) return "ECG";
        if (token.StartsWith("NM") || token.Contains("NUCLEAR")) return "NM";
        if (token.StartsWith("CT") || token.Contains("CARDIAC CT")) return "CT";
        if (token.StartsWith("MR") || token.Contains("CARDIAC MR")) return "MR";

        return string.Empty;
    }
}
