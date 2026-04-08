using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;

namespace DicomScuTestTool;

public partial class MainForm
{
    // ── C-ECHO ───────────────────────────────────────────────────

    private async void BtnEcho_Click(object? sender, EventArgs e)
    {
        _btnEcho.Enabled = false;
        _lblEchoStatus.Text = "Testing...";
        _lblEchoStatus.ForeColor = Color.Yellow;
        Log($"[ECHO] Sending C-ECHO to {_txtCalledAET.Text}@{_txtHost.Text}:{(int)_numPort.Value}...", Color.Cyan);

        try
        {
            var client = DicomClientFactory.Create(
                _txtHost.Text.Trim(), (int)_numPort.Value, false,
                _txtCallingAET.Text.Trim(), _txtCalledAET.Text.Trim());

            client.NegotiateAsyncOps();

            bool success = false;
            DicomStatus? echoStatus = null;
            var echo = new DicomCEchoRequest();
            echo.OnResponseReceived = (_, resp) =>
            {
                echoStatus = resp.Status;
                success = resp.Status == DicomStatus.Success;
            };

            await client.AddRequestAsync(echo);
            await client.SendAsync();

            if (success)
            {
                _lblEchoStatus.Text = "✔ Success";
                _lblEchoStatus.ForeColor = Color.LimeGreen;
                Log($"[ECHO] SUCCESS — {_txtCalledAET.Text}@{_txtHost.Text}:{(int)_numPort.Value} responded.", Color.LimeGreen);
            }
            else
            {
                _lblEchoStatus.Text = $"✘ {echoStatus}";
                _lblEchoStatus.ForeColor = Color.OrangeRed;
                Log($"[ECHO] FAILED — status: {echoStatus}", Color.OrangeRed);
            }
        }
        catch (Exception ex)
        {
            _lblEchoStatus.Text = "✘ Error";
            _lblEchoStatus.ForeColor = Color.OrangeRed;
            Log($"[ECHO] ERROR: {ex.Message}", Color.OrangeRed);
        }
        finally
        {
            _btnEcho.Enabled = true;
        }
    }

    // ── C-STORE SEND ─────────────────────────────────────────────

    private async Task SendAsync(bool sendAll)
    {
        List<DicomFileEntry> targets;
        if (sendAll)
        {
            targets = _files.ToList();
        }
        else
        {
            var selected = _lvFiles.SelectedItems.Cast<ListViewItem>().ToHashSet();
            targets = _files.Where(f => f.ListViewItem != null && selected.Contains(f.ListViewItem)).ToList();
        }

        if (targets.Count == 0)
        {
            MessageBox.Show("No files to send.", "Nothing to send", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _sending = true;
        _cts = new CancellationTokenSource();
        UpdateButtonStates();
        _progressBar.Value = 0;
        _progressBar.Maximum = targets.Count;
        _lblProgress.Text = $"0 / {targets.Count}";

        string host = _txtHost.Text.Trim();
        int port = (int)_numPort.Value;
        string callingAET = _txtCallingAET.Text.Trim();
        string calledAET = _txtCalledAET.Text.Trim();

        var demo = _chkOverride.Checked ? new DemographicsSnapshot
        {
            PatientName = _txtPatientName.Text.Trim(),
            PatientID = _txtPatientID.Text.Trim(),
            DOB = _txtDOB.Text.Trim(),
            Sex = _cmbSex.SelectedItem?.ToString() ?? "",
            AccessionNumber = _txtAccession.Text.Trim(),
            StudyDate = _txtStudyDate.Text.Trim(),
            StudyTime = _txtStudyTime.Text.Trim(),
            StudyDescription = _txtStudyDesc.Text.Trim()
        } : null;

        var proc = _chkOverrideProcedure.Checked ? new ProcedureSnapshot
        {
            Modality = _cmbModality.SelectedItem?.ToString() ?? "",
            ProcedureDescription = _txtProcedureDesc.Text.Trim(),
            StudyUID = _txtStudyUID.Text.Trim(),
            SeriesUID = _txtSeriesUID.Text.Trim(),
            GenerateNewStudyUID = _chkNewStudyUID.Checked,
            GenerateNewSeriesUID = _chkNewSeriesUID.Checked,
            GenerateNewSOPUID = _chkNewSOPUID.Checked
        } : null;

        Log($"[SEND] Starting: {targets.Count} file(s) → {calledAET}@{host}:{port}", Color.Cyan);

        // UID remapping maps — consistent within a batch
        var studyUidMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var seriesUidMap = new Dictionary<string, string>(StringComparer.Ordinal);

        int sent = 0, failed = 0;

        try
        {
            var client = DicomClientFactory.Create(host, port, false, callingAET, calledAET);
            client.NegotiateAsyncOps();

            var responseTasks = new List<(DicomFileEntry entry, TaskCompletionSource<bool> tcs)>();

            foreach (var entry in targets)
            {
                if (_cts.Token.IsCancellationRequested) break;

                SetFileStatus(entry, "Preparing", Color.Yellow);

                DicomFile dcm;
                try { dcm = DicomFile.Open(entry.FilePath, FileReadOption.ReadAll); }
                catch (Exception ex)
                {
                    SetFileStatus(entry, "Read Error", Color.OrangeRed);
                    Log($"[FAIL] {Path.GetFileName(entry.FilePath)}: {ex.Message}", Color.OrangeRed);
                    failed++;
                    continue;
                }

                // Debug: log what was loaded
                var originalFileSize = new FileInfo(entry.FilePath).Length;
                var hasPixelData = dcm.Dataset.Contains(DicomTag.PixelData);
                var transferSyntax = dcm.Dataset.InternalTransferSyntax?.UID?.Name ?? "unknown";
                var sopClass = dcm.Dataset.GetSingleValueOrDefault(DicomTag.SOPClassUID, "unknown");
                Log($"[DBG]  File: {Path.GetFileName(entry.FilePath)}", Color.Gray);
                Log($"[DBG]  Original size: {originalFileSize:N0} bytes", Color.Gray);
                Log($"[DBG]  Has pixel data: {hasPixelData}", hasPixelData ? Color.Gray : Color.OrangeRed);
                Log($"[DBG]  Transfer syntax: {transferSyntax}", Color.Gray);
                Log($"[DBG]  SOP Class: {sopClass}", Color.Gray);

                if (hasPixelData)
                {
                    var pixelDataItem = dcm.Dataset.GetDicomItem<DicomItem>(DicomTag.PixelData);
                    Log($"[DBG]  Pixel data type: {pixelDataItem?.GetType().Name ?? "null"}", Color.Gray);
                }

                // Modify in-place — pixel data and transfer syntax fully loaded into memory
                ApplyDemographics(dcm.Dataset, demo);
                ApplyProcedure(dcm.Dataset, entry, proc, studyUidMap, seriesUidMap);

                var tcs = new TaskCompletionSource<bool>();
                var capturedEntry = entry;
                var capturedTcs = tcs;

                // Pass DicomFile directly — no temp file, no re-read from disk
                var request = new DicomCStoreRequest(dcm);
                request.OnResponseReceived = (_, resp) =>
                {
                    bool ok = resp.Status == DicomStatus.Success;
                    Invoke(() => SetFileStatus(capturedEntry, ok ? "Sent" : $"Failed ({resp.Status.Code:X4})",
                        ok ? Color.LimeGreen : Color.OrangeRed));
                    Invoke(() => Log(ok
                        ? $"[OK]   {Path.GetFileName(capturedEntry.FilePath)}"
                        : $"[FAIL] {Path.GetFileName(capturedEntry.FilePath)}: {resp.Status}",
                        ok ? Color.LimeGreen : Color.OrangeRed));
                    capturedTcs.SetResult(ok);
                };

                responseTasks.Add((entry, tcs));
                await client.AddRequestAsync(request);
            }

            if (responseTasks.Count > 0)
            {
                Log($"[SEND] Sending {responseTasks.Count} file(s)...", Color.Cyan);

                _ = Task.Run(async () =>
                {
                    foreach (var (_, tcs) in responseTasks)
                    {
                        bool ok = await tcs.Task;
                        Invoke(() =>
                        {
                            if (ok) sent++; else failed++;
                            _progressBar.Value = Math.Min(sent + failed, _progressBar.Maximum);
                            _lblProgress.Text = $"{sent + failed} / {targets.Count}";
                        });
                    }
                });

                await client.SendAsync(_cts.Token);
                await Task.WhenAll(responseTasks.Select(r => r.tcs.Task));
            }
        }
        catch (OperationCanceledException)
        {
            Log("[SEND] Cancelled by user.", Color.Orange);
        }
        catch (Exception ex)
        {
            Log($"[SEND] Connection error: {ex.Message}", Color.OrangeRed);
            MessageBox.Show($"Send failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _sending = false;
            _cts?.Dispose();
            _cts = null;
            UpdateButtonStates();
            _lblProgress.Text = $"Done: {sent} sent, {failed} failed";
            Log($"[DONE] {sent} sent, {failed} failed.", sent > 0 && failed == 0 ? Color.LimeGreen : Color.Orange);
        }
    }

    private static void ApplyDemographics(DicomDataset ds, DemographicsSnapshot? demo)
    {
        if (demo == null) return;
        if (!string.IsNullOrWhiteSpace(demo.PatientName))    ds.AddOrUpdate(DicomTag.PatientName, demo.PatientName);
        if (!string.IsNullOrWhiteSpace(demo.PatientID))      ds.AddOrUpdate(DicomTag.PatientID, demo.PatientID);
        if (!string.IsNullOrWhiteSpace(demo.DOB))            ds.AddOrUpdate(DicomTag.PatientBirthDate, demo.DOB);
        if (!string.IsNullOrWhiteSpace(demo.Sex))            ds.AddOrUpdate(DicomTag.PatientSex, demo.Sex);
        if (!string.IsNullOrWhiteSpace(demo.AccessionNumber)) ds.AddOrUpdate(DicomTag.AccessionNumber, demo.AccessionNumber);
        var normalizedStudyDate = NormalizeStudyDate(demo.StudyDate);
        if (!string.IsNullOrWhiteSpace(normalizedStudyDate))
        {
            ds.AddOrUpdate(DicomTag.StudyDate, normalizedStudyDate);
            ds.AddOrUpdate(DicomTag.SeriesDate, normalizedStudyDate);
        }

        var normalizedStudyTime = NormalizeStudyTime(demo.StudyTime);
        if (!string.IsNullOrWhiteSpace(normalizedStudyTime))
        {
            ds.AddOrUpdate(DicomTag.StudyTime, normalizedStudyTime);
            ds.AddOrUpdate(DicomTag.SeriesTime, normalizedStudyTime);
        }

        if (!string.IsNullOrWhiteSpace(demo.StudyDescription)) ds.AddOrUpdate(DicomTag.StudyDescription, demo.StudyDescription);
    }

    private static string NormalizeStudyTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        var digits = Regex.Replace(value, "[^0-9]", "");
        if (digits.Length < 4) return "";

        // Prefer HHMMSS for broader viewer compatibility while accepting HHMM input.
        if (digits.Length >= 6)
            return digits[..6];

        return digits[..4] + "00";
    }

    private static string NormalizeStudyDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        var digits = Regex.Replace(value, "[^0-9]", "");
        if (digits.Length >= 8 && DateTime.TryParseExact(
                digits[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedExact))
        {
            return parsedExact.ToString("yyyyMMdd");
        }

        var acceptedFormats = new[]
        {
            "d-MMMM-yyyy", "d-MMM-yyyy", "d-M-yyyy",
            "dd-MMMM-yyyy", "dd-MMM-yyyy", "dd-MM-yyyy",
            "yyyy-MM-dd", "yyyy/M/d", "yyyy/MM/dd",
            "M/d/yyyy", "MM/dd/yyyy", "d/M/yyyy", "dd/MM/yyyy"
        };

        if (DateTime.TryParseExact(value.Trim(), acceptedFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            return parsed.ToString("yyyyMMdd");
        }

        if (DateTime.TryParse(value.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out parsed))
        {
            return parsed.ToString("yyyyMMdd");
        }

        return "";
    }

    private static void ApplyProcedure(
        DicomDataset ds, DicomFileEntry entry, ProcedureSnapshot? proc,
        Dictionary<string, string> studyMap, Dictionary<string, string> seriesMap)
    {
        if (proc == null) return;

        if (!string.IsNullOrWhiteSpace(proc.Modality))
            ds.AddOrUpdate(DicomTag.Modality, proc.Modality);

        if (!string.IsNullOrWhiteSpace(proc.ProcedureDescription))
            ds.AddOrUpdate(DicomTag.PerformedProcedureStepDescription, proc.ProcedureDescription);

        // Study UID: explicit value takes priority, then generate-new flag
        if (!string.IsNullOrWhiteSpace(proc.StudyUID))
        {
            ds.AddOrUpdate(DicomTag.StudyInstanceUID, proc.StudyUID);
        }
        else if (proc.GenerateNewStudyUID)
        {
            if (!studyMap.TryGetValue(entry.StudyInstanceUID, out var newStudy))
                studyMap[entry.StudyInstanceUID] = newStudy = DicomUIDGenerator.GenerateNew();
            ds.AddOrUpdate(DicomTag.StudyInstanceUID, newStudy);
        }

        // Series UID: explicit value takes priority, then generate-new flag
        if (!string.IsNullOrWhiteSpace(proc.SeriesUID))
        {
            ds.AddOrUpdate(DicomTag.SeriesInstanceUID, proc.SeriesUID);
        }
        else if (proc.GenerateNewSeriesUID)
        {
            if (!seriesMap.TryGetValue(entry.SeriesInstanceUID, out var newSeries))
                seriesMap[entry.SeriesInstanceUID] = newSeries = DicomUIDGenerator.GenerateNew();
            ds.AddOrUpdate(DicomTag.SeriesInstanceUID, newSeries);
        }

        // SOP Instance UID: always unique per file
        if (proc.GenerateNewSOPUID)
            ds.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateNew());
    }

    // ── SETTINGS ─────────────────────────────────────────────────

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsFile)) return;
            var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsFile));
            if (s == null) return;
            _txtHost.Text = s.Host;
            _numPort.Value = Math.Clamp(s.Port, 1, 65535);
            _txtCallingAET.Text = s.CallingAET;
            _txtCalledAET.Text = s.CalledAET;
            _chkOverride.Checked = s.OverrideDemographics;
            _chkOverrideProcedure.Checked = s.OverrideProcedure;
            _txtLookupConnectionString.Text = s.LookupConnectionString;
            _numLookupDays.Value = Math.Clamp(s.LookupRangeDays, (int)_numLookupDays.Minimum, (int)_numLookupDays.Maximum);
            _chkLookupTrustServerCert.Checked = s.LookupTrustServerCertificate;
            UpdateLookupTrustServerCertificateInConnectionString();
        }
        catch { /* ignore corrupt settings */ }
    }

    private void SaveSettings()
    {
        try
        {
            var s = new AppSettings
            {
                Host = _txtHost.Text.Trim(),
                Port = (int)_numPort.Value,
                CallingAET = _txtCallingAET.Text.Trim(),
                CalledAET = _txtCalledAET.Text.Trim(),
                OverrideDemographics = _chkOverride.Checked,
                OverrideProcedure = _chkOverrideProcedure.Checked,
                LookupConnectionString = _txtLookupConnectionString.Text.Trim(),
                LookupRangeDays = (int)_numLookupDays.Value,
                LookupTrustServerCertificate = _chkLookupTrustServerCert.Checked
            };
            File.WriteAllText(_settingsFile, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* best effort */ }
    }
}
