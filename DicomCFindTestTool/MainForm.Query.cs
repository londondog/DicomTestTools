using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;

namespace DicomCFindTestTool;

public partial class MainForm
{
    // ── C-ECHO ────────────────────────────────────────────────────

    private async Task RunEchoAsync()
    {
        _lblEchoStatus.Text = "Testing...";
        _lblEchoStatus.ForeColor = Color.Gray;
        SetQuerying(true);
        try
        {
            var client = DicomClientFactory.Create(
                _txtHost.Text.Trim(),
                (int)_numPort.Value,
                false,
                _txtCallingAET.Text.Trim(),
                _txtCalledAET.Text.Trim());
            bool success = false;
            string statusText = "No response";
            var echo = new DicomCEchoRequest();
            echo.OnResponseReceived += (_, resp) =>
            {
                success = resp.Status == DicomStatus.Success;
                statusText = resp.Status.ToString();
            };
            await client.AddRequestAsync(echo);
            await client.SendAsync();

            if (success)
            {
                _lblEchoStatus.Text = "✔ Echo OK";
                _lblEchoStatus.ForeColor = Color.LimeGreen;
                Log($"C-ECHO success → {_txtHost.Text.Trim()}:{(int)_numPort.Value}", Color.LimeGreen);
            }
            else
            {
                _lblEchoStatus.Text = $"✘ {statusText}";
                _lblEchoStatus.ForeColor = Color.OrangeRed;
                Log($"C-ECHO failed: {statusText}", Color.OrangeRed);
            }
        }
        catch (Exception ex)
        {
            _lblEchoStatus.Text = "✘ Error";
            _lblEchoStatus.ForeColor = Color.OrangeRed;
            Log($"C-ECHO error: {ex.Message}", Color.OrangeRed);
        }
        finally
        {
            SetQuerying(false);
        }
    }

    // ── Study / Patient C-FIND ────────────────────────────────────

    private async Task RunStudyQueryAsync()
    {
        SetQuerying(true);
        _dgvStudy.Rows.Clear();
        _lblStudyCount.Text = "Querying...";
        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            DicomCFindRequest request;
            if (_radPatientRoot.Checked)
            {
                request = new DicomCFindRequest(DicomUID.PatientRootQueryRetrieveInformationModelFind);
                request.Dataset.AddOrUpdate(DicomTag.QueryRetrieveLevel, "STUDY");
            }
            else
            {
                request = new DicomCFindRequest(DicomQueryRetrieveLevel.Study);
            }

            // Patient
            SetQueryParam(request.Dataset, DicomTag.PatientID, _txtStudyPatientID.Text.Trim());
            SetQueryParam(request.Dataset, DicomTag.PatientName, _txtStudyPatientName.Text.Trim());
            SetQueryParam(request.Dataset, DicomTag.PatientBirthDate, _txtStudyDOB.Text.Trim());
            request.Dataset.AddOrUpdate(DicomTag.PatientSex, _cmbStudySex.SelectedItem?.ToString() ?? "");

            // Study
            SetQueryParam(request.Dataset, DicomTag.StudyInstanceUID, _txtStudyUID.Text.Trim());
            SetQueryParam(request.Dataset, DicomTag.AccessionNumber, _txtStudyAccession.Text.Trim());
            SetQueryParam(request.Dataset, DicomTag.StudyDescription, _txtStudyDesc.Text.Trim());
            SetQueryParam(request.Dataset, DicomTag.ReferringPhysicianName, _txtStudyReferringPhysician.Text.Trim());
            request.Dataset.AddOrUpdate(DicomTag.StudyDate, BuildDateRange(_chkStudyDateFrom, _dtpStudyDateFrom, _chkStudyDateTo, _dtpStudyDateTo));
            request.Dataset.AddOrUpdate(DicomTag.StudyTime, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyID, "");

            // Modality filter
            request.Dataset.AddOrUpdate(DicomTag.ModalitiesInStudy, _cmbStudyModality.SelectedItem?.ToString() ?? "");

            // Return-only tags
            request.Dataset.AddOrUpdate(DicomTag.NumberOfStudyRelatedSeries, "");
            request.Dataset.AddOrUpdate(DicomTag.NumberOfStudyRelatedInstances, "");

            var results = new List<StudyResult>();
            request.OnResponseReceived += (_, response) =>
            {
                if (response.HasDataset)
                    results.Add(ParseStudyResult(response.Dataset));
            };

            Log($"Study C-FIND → {ConnectionString()}  [{(_radPatientRoot.Checked ? "Patient Root" : "Study Root")}]", Color.Cyan);

            var client = MakeClient();
            await client.AddRequestAsync(request);
            await client.SendAsync(ct);

            PopulateStudyGrid(results);
            Log($"Study query complete: {results.Count} result(s)", Color.LightGreen);
        }
        catch (OperationCanceledException)
        {
            Log("Query cancelled", Color.Yellow);
            _lblStudyCount.Text = "Cancelled";
        }
        catch (Exception ex)
        {
            Log($"Study query error: {ex.Message}", Color.OrangeRed);
            _lblStudyCount.Text = "Error";
        }
        finally
        {
            SetQuerying(false);
        }
    }

    private void PopulateStudyGrid(List<StudyResult> results)
    {
        if (InvokeRequired) { Invoke(() => PopulateStudyGrid(results)); return; }
        _dgvStudy.Rows.Clear();
        foreach (var r in results)
        {
            _dgvStudy.Rows.Add(r.PatientName, r.PatientID, r.PatientDOB, r.PatientSex,
                r.StudyDate, r.AccessionNumber, r.Modality, r.StudyDescription,
                r.NumberOfSeries, r.NumberOfInstances, r.StudyInstanceUID);
        }
        _lblStudyCount.Text = $"{results.Count} result(s)";
    }

    // ── Series C-FIND ─────────────────────────────────────────────

    private async Task RunSeriesQueryAsync()
    {
        var studyUID = _txtSeriesStudyUID.Text.Trim();
        if (string.IsNullOrEmpty(studyUID))
        {
            Log("Series query requires a Study UID", Color.OrangeRed);
            MessageBox.Show("Please enter a Study Instance UID.", "Study UID Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetQuerying(true);
        _dgvSeries.Rows.Clear();
        _lblSeriesCount.Text = "Querying...";
        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Series);
            request.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyUID);

            SetQueryParam(request.Dataset, DicomTag.SeriesInstanceUID, _txtSeriesSeriesUID.Text.Trim());
            request.Dataset.AddOrUpdate(DicomTag.Modality, _cmbSeriesModality.SelectedItem?.ToString() ?? "");
            SetQueryParam(request.Dataset, DicomTag.SeriesDescription, _txtSeriesDesc.Text.Trim());
            request.Dataset.AddOrUpdate(DicomTag.SeriesNumber, "");
            request.Dataset.AddOrUpdate(DicomTag.SeriesDate, "");
            request.Dataset.AddOrUpdate(DicomTag.SeriesTime, "");
            request.Dataset.AddOrUpdate(DicomTag.BodyPartExamined, "");
            request.Dataset.AddOrUpdate(DicomTag.NumberOfSeriesRelatedInstances, "");

            var results = new List<SeriesResult>();
            request.OnResponseReceived += (_, response) =>
            {
                if (response.HasDataset)
                    results.Add(ParseSeriesResult(response.Dataset, studyUID));
            };

            Log($"Series C-FIND → {ConnectionString()}  [StudyUID={studyUID}]", Color.Cyan);

            var client = MakeClient();
            await client.AddRequestAsync(request);
            await client.SendAsync(ct);

            PopulateSeriesGrid(results);
            Log($"Series query complete: {results.Count} result(s)", Color.LightGreen);
        }
        catch (OperationCanceledException)
        {
            Log("Query cancelled", Color.Yellow);
            _lblSeriesCount.Text = "Cancelled";
        }
        catch (Exception ex)
        {
            Log($"Series query error: {ex.Message}", Color.OrangeRed);
            _lblSeriesCount.Text = "Error";
        }
        finally
        {
            SetQuerying(false);
        }
    }

    private void PopulateSeriesGrid(List<SeriesResult> results)
    {
        if (InvokeRequired) { Invoke(() => PopulateSeriesGrid(results)); return; }
        _dgvSeries.Rows.Clear();
        foreach (var r in results)
        {
            _dgvSeries.Rows.Add(r.SeriesNumber, r.Modality, r.SeriesDescription,
                r.SeriesDate, r.BodyPartExamined, r.NumberOfInstances,
                r.StudyInstanceUID, r.SeriesInstanceUID);
        }
        _lblSeriesCount.Text = $"{results.Count} result(s)";
    }

    // ── Instance C-FIND ───────────────────────────────────────────

    private async Task RunInstanceQueryAsync()
    {
        var studyUID = _txtInstStudyUID.Text.Trim();
        if (string.IsNullOrEmpty(studyUID))
        {
            Log("Instance query requires a Study UID", Color.OrangeRed);
            MessageBox.Show("Please enter a Study Instance UID.", "Study UID Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetQuerying(true);
        _dgvInst.Rows.Clear();
        _lblInstCount.Text = "Querying...";
        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Image);
            request.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyUID);

            var seriesUID = _txtInstSeriesUID.Text.Trim();
            request.Dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, seriesUID);

            request.Dataset.AddOrUpdate(DicomTag.SOPInstanceUID, "");
            request.Dataset.AddOrUpdate(DicomTag.SOPClassUID, "");
            request.Dataset.AddOrUpdate(DicomTag.InstanceNumber, "");
            request.Dataset.AddOrUpdate(DicomTag.Rows, "");
            request.Dataset.AddOrUpdate(DicomTag.Columns, "");
            request.Dataset.AddOrUpdate(DicomTag.ContentDate, "");

            var results = new List<InstanceResult>();
            request.OnResponseReceived += (_, response) =>
            {
                if (response.HasDataset)
                    results.Add(ParseInstanceResult(response.Dataset, studyUID, seriesUID));
            };

            var label = string.IsNullOrEmpty(seriesUID)
                ? $"StudyUID={studyUID}"
                : $"StudyUID={studyUID}  SeriesUID={seriesUID}";
            Log($"Instance C-FIND → {ConnectionString()}  [{label}]", Color.Cyan);

            var client = MakeClient();
            await client.AddRequestAsync(request);
            await client.SendAsync(ct);

            PopulateInstanceGrid(results);
            Log($"Instance query complete: {results.Count} result(s)", Color.LightGreen);
        }
        catch (OperationCanceledException)
        {
            Log("Query cancelled", Color.Yellow);
            _lblInstCount.Text = "Cancelled";
        }
        catch (Exception ex)
        {
            Log($"Instance query error: {ex.Message}", Color.OrangeRed);
            _lblInstCount.Text = "Error";
        }
        finally
        {
            SetQuerying(false);
        }
    }

    private void PopulateInstanceGrid(List<InstanceResult> results)
    {
        if (InvokeRequired) { Invoke(() => PopulateInstanceGrid(results)); return; }
        _dgvInst.Rows.Clear();
        foreach (var r in results)
        {
            _dgvInst.Rows.Add(r.InstanceNumber, r.SOPClassName, r.Rows, r.Columns,
                r.ContentDate, r.SOPInstanceUID, r.SOPClassUID);
        }
        _lblInstCount.Text = $"{results.Count} result(s)";
    }

    // ── Worklist (MWL) C-FIND ─────────────────────────────────────

    private async Task RunWorklistQueryAsync()
    {
        SetQuerying(true);
        _dgvWorklist.Rows.Clear();
        _lblWlCount.Text = "Querying...";
        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            // MWL uses a different SOP class — no QueryRetrieveLevel
            var request = new DicomCFindRequest(DicomUID.ModalityWorklistInformationModelFind);

            // Patient tags
            SetQueryParam(request.Dataset, DicomTag.PatientID, _txtWlPatientID.Text.Trim());
            SetQueryParam(request.Dataset, DicomTag.PatientName, _txtWlPatientName.Text.Trim());
            request.Dataset.AddOrUpdate(DicomTag.PatientBirthDate, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientSex, "");

            // Study tags
            SetQueryParam(request.Dataset, DicomTag.AccessionNumber, _txtWlAccession.Text.Trim());
            request.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, "");
            request.Dataset.AddOrUpdate(DicomTag.RequestedProcedureDescription, "");
            SetQueryParam(request.Dataset, DicomTag.RequestedProcedureID, _txtWlRequestedProcID.Text.Trim());
            request.Dataset.AddOrUpdate(DicomTag.AdmissionID, "");
            request.Dataset.AddOrUpdate(DicomTag.SpecialNeeds, "");

            // Scheduled Procedure Step Sequence (required for MWL)
            var spsItem = new DicomDataset();
            spsItem.AddOrUpdate(DicomTag.Modality, _cmbWlModality.SelectedItem?.ToString() ?? "");
            SetQueryParam(spsItem, DicomTag.ScheduledStationAETitle, _txtWlStationAE.Text.Trim());
            spsItem.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDate,
                BuildDateRange(_chkWlDateFrom, _dtpWlDateFrom, _chkWlDateTo, _dtpWlDateTo));
            spsItem.AddOrUpdate(DicomTag.ScheduledProcedureStepStartTime, "");
            SetQueryParam(spsItem, DicomTag.ScheduledPerformingPhysicianName, _txtWlPhysician.Text.Trim());
            SetQueryParam(spsItem, DicomTag.ScheduledProcedureStepDescription, _cmbWlProcedureType.Text.Trim());
            spsItem.AddOrUpdate(DicomTag.ScheduledProcedureStepID, "");
            spsItem.AddOrUpdate(DicomTag.ScheduledStationName, "");

            // Protocol code sequence — filter by code value if provided, otherwise request return
            var protocolCode = _txtWlProtocolCode.Text.Trim();
            var codeItem = new DicomDataset();
            codeItem.AddOrUpdate(DicomTag.CodeValue, protocolCode);
            codeItem.AddOrUpdate(DicomTag.CodingSchemeDesignator, "");
            codeItem.AddOrUpdate(DicomTag.CodeMeaning, "");
            spsItem.Add(new DicomSequence(DicomTag.ScheduledProtocolCodeSequence, codeItem));

            request.Dataset.Add(new DicomSequence(DicomTag.ScheduledProcedureStepSequence, spsItem));

            var results = new List<WorklistResult>();
            request.OnResponseReceived += (_, response) =>
            {
                if (response.HasDataset)
                    results.Add(ParseWorklistResult(response.Dataset));
            };

            Log($"MWL C-FIND → {ConnectionString()}  [SOP: ModalityWorklistInformationModelFind]", Color.Cyan);

            var client = MakeClient();
            await client.AddRequestAsync(request);
            await client.SendAsync(ct);

            PopulateWorklistGrid(results);
            Log($"Worklist query complete: {results.Count} result(s)", Color.LightGreen);
        }
        catch (OperationCanceledException)
        {
            Log("Query cancelled", Color.Yellow);
            _lblWlCount.Text = "Cancelled";
        }
        catch (Exception ex)
        {
            Log($"Worklist query error: {ex.Message}", Color.OrangeRed);
            _lblWlCount.Text = "Error";
        }
        finally
        {
            SetQuerying(false);
        }
    }

    private void PopulateWorklistGrid(List<WorklistResult> results)
    {
        if (InvokeRequired) { Invoke(() => PopulateWorklistGrid(results)); return; }
        _dgvWorklist.Rows.Clear();
        foreach (var r in results)
        {
            _dgvWorklist.Rows.Add(r.PatientName, r.PatientID, r.PatientDOB, r.PatientSex,
                r.AccessionNumber, r.ScheduledDate, r.ScheduledTime, r.Modality,
                r.StationAETitle, r.StationName, r.PerformingPhysician,
                r.RequestedProcedureDescription, r.ProcedureStepDescription,
                r.ProcedureStepID, r.ProtocolCode, r.ProtocolCodeMeaning,
                r.StudyInstanceUID);
        }
        _lblWlCount.Text = $"{results.Count} result(s)";
    }

    // ── Parse helpers ─────────────────────────────────────────────

    private static StudyResult ParseStudyResult(DicomDataset ds)
    {
        var modality = "";
        try
        {
            if (ds.Contains(DicomTag.ModalitiesInStudy))
            {
                var vals = ds.GetValues<string>(DicomTag.ModalitiesInStudy);
                modality = vals?.Length > 0 ? string.Join("\\", vals) : "";
            }
        }
        catch
        {
            modality = GetTag(ds, DicomTag.ModalitiesInStudy);
        }

        return new StudyResult
        {
            PatientName = GetTag(ds, DicomTag.PatientName),
            PatientID = GetTag(ds, DicomTag.PatientID),
            PatientDOB = GetTag(ds, DicomTag.PatientBirthDate),
            PatientSex = GetTag(ds, DicomTag.PatientSex),
            StudyDate = GetTag(ds, DicomTag.StudyDate),
            StudyTime = GetTag(ds, DicomTag.StudyTime),
            AccessionNumber = GetTag(ds, DicomTag.AccessionNumber),
            Modality = modality,
            StudyDescription = GetTag(ds, DicomTag.StudyDescription),
            ReferringPhysician = GetTag(ds, DicomTag.ReferringPhysicianName),
            StudyID = GetTag(ds, DicomTag.StudyID),
            StudyInstanceUID = GetTag(ds, DicomTag.StudyInstanceUID),
            NumberOfSeries = GetTag(ds, DicomTag.NumberOfStudyRelatedSeries),
            NumberOfInstances = GetTag(ds, DicomTag.NumberOfStudyRelatedInstances)
        };
    }

    private static SeriesResult ParseSeriesResult(DicomDataset ds, string studyUID)
    {
        return new SeriesResult
        {
            StudyInstanceUID = studyUID,
            SeriesNumber = GetTag(ds, DicomTag.SeriesNumber),
            Modality = GetTag(ds, DicomTag.Modality),
            SeriesDescription = GetTag(ds, DicomTag.SeriesDescription),
            SeriesDate = GetTag(ds, DicomTag.SeriesDate),
            SeriesTime = GetTag(ds, DicomTag.SeriesTime),
            BodyPartExamined = GetTag(ds, DicomTag.BodyPartExamined),
            NumberOfInstances = GetTag(ds, DicomTag.NumberOfSeriesRelatedInstances),
            SeriesInstanceUID = GetTag(ds, DicomTag.SeriesInstanceUID)
        };
    }

    private static InstanceResult ParseInstanceResult(DicomDataset ds, string studyUID, string seriesUID)
    {
        var sopClassUID = GetTag(ds, DicomTag.SOPClassUID);
        var sopClassName = string.IsNullOrEmpty(sopClassUID) ? "" : TryGetSopClassName(sopClassUID);
        return new InstanceResult
        {
            StudyInstanceUID = studyUID,
            SeriesInstanceUID = seriesUID,
            InstanceNumber = GetTag(ds, DicomTag.InstanceNumber),
            SOPClassUID = sopClassUID,
            SOPClassName = sopClassName,
            SOPInstanceUID = GetTag(ds, DicomTag.SOPInstanceUID),
            Rows = GetTag(ds, DicomTag.Rows),
            Columns = GetTag(ds, DicomTag.Columns),
            ContentDate = GetTag(ds, DicomTag.ContentDate)
        };
    }

    private static WorklistResult ParseWorklistResult(DicomDataset ds)
    {
        var result = new WorklistResult
        {
            PatientName = GetTag(ds, DicomTag.PatientName),
            PatientID = GetTag(ds, DicomTag.PatientID),
            PatientDOB = GetTag(ds, DicomTag.PatientBirthDate),
            PatientSex = GetTag(ds, DicomTag.PatientSex),
            AccessionNumber = GetTag(ds, DicomTag.AccessionNumber),
            StudyInstanceUID = GetTag(ds, DicomTag.StudyInstanceUID),
            RequestedProcedureDescription = GetTag(ds, DicomTag.RequestedProcedureDescription),
            RequestedProcedureID = GetTag(ds, DicomTag.RequestedProcedureID)
        };

        // Extract scheduled procedure step from sequence
        try
        {
            var seq = ds.GetSequence(DicomTag.ScheduledProcedureStepSequence);
            if (seq?.Items.Count > 0)
            {
                var item = seq.Items[0];
                result.ScheduledDate = GetTag(item, DicomTag.ScheduledProcedureStepStartDate);
                result.ScheduledTime = GetTag(item, DicomTag.ScheduledProcedureStepStartTime);
                result.Modality = GetTag(item, DicomTag.Modality);
                result.StationAETitle = GetTag(item, DicomTag.ScheduledStationAETitle);
                result.StationName = GetTag(item, DicomTag.ScheduledStationName);
                result.PerformingPhysician = GetTag(item, DicomTag.ScheduledPerformingPhysicianName);
                result.ProcedureStepID = GetTag(item, DicomTag.ScheduledProcedureStepID);
                result.ProcedureStepDescription = GetTag(item, DicomTag.ScheduledProcedureStepDescription);

                // Protocol code sequence
                try
                {
                    var codeSeq = item.GetSequence(DicomTag.ScheduledProtocolCodeSequence);
                    if (codeSeq?.Items.Count > 0)
                    {
                        result.ProtocolCode = GetTag(codeSeq.Items[0], DicomTag.CodeValue);
                        result.ProtocolCodeMeaning = GetTag(codeSeq.Items[0], DicomTag.CodeMeaning);
                    }
                }
                catch { /* not present */ }
            }
        }
        catch { /* sequence not present or malformed */ }

        return result;
    }

    // ── Shared helpers ────────────────────────────────────────────

    private IDicomClient MakeClient()
    {
        var client = DicomClientFactory.Create(
            _txtHost.Text.Trim(),
            (int)_numPort.Value,
            false,
            _txtCallingAET.Text.Trim(),
            _txtCalledAET.Text.Trim());
        client.NegotiateAsyncOps();
        return client;
    }

    private string ConnectionString() =>
        $"{_txtCalledAET.Text.Trim()}@{_txtHost.Text.Trim()}:{(int)_numPort.Value}";

    private static void SetQueryParam(DicomDataset ds, DicomTag tag, string value)
    {
        // Empty string = "return this field" with no filter
        // Non-empty = filter by this value (wildcard supported by PACS)
        ds.AddOrUpdate(tag, value);
    }

    private static string BuildDateRange(CheckBox chkFrom, DateTimePicker dtpFrom, CheckBox chkTo, DateTimePicker dtpTo)
    {
        bool hasFrom = chkFrom.Checked;
        bool hasTo = chkTo.Checked;
        if (!hasFrom && !hasTo) return "";
        if (hasFrom && !hasTo) return dtpFrom.Value.ToString("yyyyMMdd") + "-";
        if (!hasFrom && hasTo) return "-" + dtpTo.Value.ToString("yyyyMMdd");
        return dtpFrom.Value.ToString("yyyyMMdd") + "-" + dtpTo.Value.ToString("yyyyMMdd");
    }

    private static string GetTag(DicomDataset ds, DicomTag tag)
    {
        try { return ds.GetSingleValueOrDefault(tag, ""); }
        catch { return ""; }
    }

    private static string TryGetSopClassName(string uid)
    {
        try
        {
            var dicomUID = DicomUID.Parse(uid);
            return dicomUID?.Name ?? uid;
        }
        catch
        {
            return uid;
        }
    }
}
