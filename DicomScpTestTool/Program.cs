using System.Text;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== DICOM SCP Test Tool  v1.0  —  by George Hutchings ===");
Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine();

// Parse command line arguments
var port = 11112;
var aet = "TEST_SCP";
var storageFolder = Path.Combine(Environment.CurrentDirectory, "DicomStorage");
var verbose = false;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-v" || args[i] == "--verbose")
        verbose = true;
    else if (i == 0 && int.TryParse(args[i], out int customPort))
        port = customPort;
    else if (i == 1 && !args[i].StartsWith("-"))
        aet = args[i];
    else if (i == 2 && !args[i].StartsWith("-"))
        storageFolder = args[i];
}

CStoreProvider.VerboseLogging = verbose;
CStoreProvider.StorageFolderPath = storageFolder;

// Create storage directory
Directory.CreateDirectory(storageFolder);

Console.WriteLine($"Configuration:");
Console.WriteLine($"  AE Title:       {aet}");
Console.WriteLine($"  Port:           {port}");
Console.WriteLine($"  Storage Folder: {storageFolder}");
Console.WriteLine($"  Verbose:        {verbose}");
Console.WriteLine();
Console.WriteLine("DICOM Storage Commitment Support:");
Console.WriteLine("  This test tool supports the DICOM Storage Commitment Push Model:");
Console.WriteLine("  1. C-STORE: Receives and stores DICOM images, tracking SOP Instance UIDs");
Console.WriteLine("  2. N-ACTION: Accepts Storage Commitment requests, validates stored instances");
Console.WriteLine("  3. N-EVENT-REPORT: Sends confirmation back to SCU on port 11113");
Console.WriteLine();
Console.WriteLine("  Event Types:");
Console.WriteLine("    0x0001 = All instances successfully committed");
Console.WriteLine("    0x0002 = One or more instances failed commitment");
Console.WriteLine();
Console.WriteLine("  To test with Migration Tool:");
Console.WriteLine("    - Enable 'Storage Commitment' in destination settings");
Console.WriteLine("    - This SCP will validate each received instance");
Console.WriteLine("    - N-EVENT-REPORT sent to SCU at localhost:11113 after 1 second");
Console.WriteLine();
Console.WriteLine("Usage: DicomScpTestTool [port] [aet] [storage_folder] [-v|--verbose]");
Console.WriteLine();
Console.WriteLine("Press Ctrl+C to stop...");
Console.WriteLine(new string('=', 80));
Console.WriteLine();

// Create and start DICOM server
Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Starting DICOM SCP on port {port}...");
var server = DicomServerFactory.Create<CStoreProvider>(port);
Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] DICOM SCP is running and listening for connections");
Console.WriteLine();
Console.WriteLine("┌─────────────────────────────────────────────────────────────────────────────┐");
Console.WriteLine("│                           ✓ SERVER STATUS: RUNNING                         │");
Console.WriteLine("│                                                                             │");
Console.WriteLine($"│  Listening on:  {aet} @ 0.0.0.0:{port,-45}│");
Console.WriteLine("│  Ready to accept C-STORE, C-ECHO, and Storage Commitment requests          │");
Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────┘");
Console.WriteLine();

// Keep running
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\nShutting down...");
};

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (TaskCanceledException)
{
    // Expected when Ctrl+C is pressed
}

Console.WriteLine();
Console.WriteLine("┌─────────────────────────────────────────────────────────────────────────────┐");
Console.WriteLine("│                           ✗ SERVER STATUS: STOPPED                         │");
Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────┘");
Console.WriteLine();
Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Disposing server...");
server.Dispose();
Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Server disposed successfully");
Console.WriteLine("Goodbye!");

// DICOM C-STORE Provider Implementation with Storage Commitment support
public class CStoreProvider : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider, IDicomNServiceProvider
{
    public static string StorageFolderPath = Path.Combine(Environment.CurrentDirectory, "DicomStorage");
    public static bool VerboseLogging = false;
    private static int _fileCounter = 0;
    private static int _connectionCounter = 0;
    private int _connectionId;
    private DateTime _connectionTime;

    // Storage Commitment tracking
    private static readonly Dictionary<string, List<string>> _storedInstances = new();
    private static readonly Dictionary<string, string> _transactionCallingAE = new(); // Track which AE sent the commitment request
    private static readonly object _storageLock = new();

    public CStoreProvider(INetworkStream stream, Encoding fallbackEncoding, ILogger log, DicomServiceDependencies dependencies)
        : base(stream, fallbackEncoding, log, dependencies)
    {
        _connectionId = Interlocked.Increment(ref _connectionCounter);
        _connectionTime = DateTime.Now;
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] New connection established");
        
        if (VerboseLogging)
        {
            Console.WriteLine($"  Encoding: {fallbackEncoding?.EncodingName ?? "default"}");
        }
    }

    public async Task OnReceiveAssociationRequestAsync(DicomAssociation association)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] === ASSOCIATION REQUEST ===");
        Console.WriteLine($"  Calling AE Title: {association.CallingAE}");
        Console.WriteLine($"  Called AE Title:  {association.CalledAE}");
        Console.WriteLine($"  Max PDU Length:   {association.MaximumPDULength}");
        Console.WriteLine($"  Implementation:   {association.RemoteImplementationClassUID}");
        Console.WriteLine($"  Version:          {association.RemoteImplementationVersion}");
        
        if (VerboseLogging)
        {
            Console.WriteLine($"  Async Ops:        Window={association.MaxAsyncOpsInvoked}, Performed={association.MaxAsyncOpsPerformed}");
        }

        Console.WriteLine($"  Presentation Contexts ({association.PresentationContexts.Count}):");
        
        int acceptedCount = 0;
        foreach (var pc in association.PresentationContexts)
        {
            var abstractSyntax = pc.AbstractSyntax;
            var abstractSyntaxName = abstractSyntax.Name;

            if (VerboseLogging)
            {
                Console.WriteLine($"    [{pc.ID}] {abstractSyntaxName}");
                Console.WriteLine($"        UID: {abstractSyntax.UID}");
                Console.WriteLine($"        Transfer Syntaxes: {pc.GetTransferSyntaxes().Count()}");
                foreach (var ts in pc.GetTransferSyntaxes())
                {
                    Console.WriteLine($"          - {ts.UID.Name}");
                }
            }
            else
            {
                Console.WriteLine($"    [{pc.ID}] {abstractSyntaxName}");
            }

            // Accept whatever transfer syntax the SCU proposes (no transcoding)
            var proposedTransferSyntaxes = pc.GetTransferSyntaxes().ToArray();
            pc.AcceptTransferSyntaxes(proposedTransferSyntaxes);
            
            acceptedCount++;
        }

        await SendAssociationAcceptAsync(association);
        Console.WriteLine($"  Status: ACCEPTED ({acceptedCount} contexts)");
        Console.WriteLine();
    }

    public async Task OnReceiveAssociationReleaseRequestAsync()
    {
        var duration = DateTime.Now - _connectionTime;
        await SendAssociationReleaseResponseAsync();
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] Association released normally");
        Console.WriteLine($"  Duration: {duration.TotalSeconds:F2} seconds");
        Console.WriteLine();
    }

    public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
    {
        var duration = DateTime.Now - _connectionTime;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] Association ABORTED");
        Console.WriteLine($"  Source: {source}");
        Console.WriteLine($"  Reason: {reason}");
        Console.WriteLine($"  Duration: {duration.TotalSeconds:F2} seconds");
        Console.WriteLine();
    }

    public void OnConnectionClosed(Exception exception)
    {
        if (exception != null)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] Connection closed with ERROR");
            Console.WriteLine($"  Error: {exception.GetType().Name}");
            Console.WriteLine($"  Message: {exception.Message}");
            if (VerboseLogging && exception.StackTrace != null)
                Console.WriteLine($"  Stack: {exception.StackTrace.Split('\n')[0]}");
            Console.WriteLine();
        }
        else if (VerboseLogging)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] Connection closed normally");
            Console.WriteLine();
        }
    }

    public async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
    {
        var startTime = DateTime.Now;
        var dataset = request.Dataset;
        
        // Extract all relevant DICOM tags
        var sopInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, "unknown");
        var sopClassUID = dataset.GetSingleValueOrDefault(DicomTag.SOPClassUID, "unknown");
        var studyUID = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, "unknown");
        var seriesUID = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, "unknown");
        var patientID = dataset.GetSingleValueOrDefault(DicomTag.PatientID, "unknown");
        var patientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, "unknown");
        var modality = dataset.GetSingleValueOrDefault(DicomTag.Modality, "unknown");
        var studyDate = dataset.GetSingleValueOrDefault(DicomTag.StudyDate, "unknown");
        var instanceNumber = dataset.GetSingleValueOrDefault(DicomTag.InstanceNumber, "unknown");
        var seriesNumber = dataset.GetSingleValueOrDefault(DicomTag.SeriesNumber, "unknown");

        var fileNumber = Interlocked.Increment(ref _fileCounter);

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] >>> C-STORE REQUEST #{fileNumber} <<<");
        Console.WriteLine($"  Patient:");
        Console.WriteLine($"    ID:             {patientID}");
        Console.WriteLine($"    Name:           {patientName}");
        Console.WriteLine($"  Study:");
        Console.WriteLine($"    UID:            {studyUID}");
        Console.WriteLine($"    Date:           {studyDate}");
        Console.WriteLine($"  Series:");
        Console.WriteLine($"    UID:            {seriesUID}");
        Console.WriteLine($"    Number:         {seriesNumber}");
        Console.WriteLine($"    Modality:       {modality}");
        Console.WriteLine($"  Instance:");
        Console.WriteLine($"    SOP UID:        {sopInstanceUID}");
        Console.WriteLine($"    Number:         {instanceNumber}");
        
        if (VerboseLogging)
        {
            Console.WriteLine($"    SOP Class UID:  {sopClassUID}");
            Console.WriteLine($"    Transfer Syn:   {request.TransferSyntax}");
            Console.WriteLine($"    Message ID:     {request.MessageID}");
            Console.WriteLine($"    Priority:       {request.Priority}");
            
            // Show some additional tags if present
            if (dataset.Contains(DicomTag.AccessionNumber))
                Console.WriteLine($"    Accession:      {dataset.GetSingleValue<string>(DicomTag.AccessionNumber)}");
            if (dataset.Contains(DicomTag.StudyDescription))
                Console.WriteLine($"    Study Desc:     {dataset.GetSingleValue<string>(DicomTag.StudyDescription)}");
            if (dataset.Contains(DicomTag.SeriesDescription))
                Console.WriteLine($"    Series Desc:    {dataset.GetSingleValue<string>(DicomTag.SeriesDescription)}");
        }

        try
        {
            Directory.CreateDirectory(StorageFolderPath);

            // Save all files flat into the storage folder
            var fileName = $"{SanitizeFileName(sopInstanceUID)}.dcm";
            var filePath = Path.Combine(StorageFolderPath, fileName);

            if (VerboseLogging)
                Console.WriteLine($"  Saving file: {filePath}");

            await request.File.SaveAsync(filePath);

            var fileInfo = new FileInfo(filePath);
            var duration = DateTime.Now - startTime;

            // Track stored instance for Storage Commitment
            lock (_storageLock)
            {
                if (!_storedInstances.ContainsKey(studyUID))
                {
                    _storedInstances[studyUID] = new List<string>();
                }
                if (!_storedInstances[studyUID].Contains(sopInstanceUID))
                {
                    _storedInstances[studyUID].Add(sopInstanceUID);
                }
            }

            Console.WriteLine($"  Result: SUCCESS");
            Console.WriteLine($"  File Size: {fileInfo.Length:N0} bytes");
            Console.WriteLine($"  Save Time: {duration.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  Path: {filePath}");
            Console.WriteLine();

            return new DicomCStoreResponse(request, DicomStatus.Success);
        }
        catch (Exception ex)
        {
            var duration = DateTime.Now - startTime;
            Console.WriteLine($"  Result: FAILURE");
            Console.WriteLine($"  Error Type: {ex.GetType().Name}");
            Console.WriteLine($"  Error Message: {ex.Message}");
            Console.WriteLine($"  Duration: {duration.TotalMilliseconds:F2} ms");
            
            if (VerboseLogging && ex.StackTrace != null)
            {
                Console.WriteLine($"  Stack Trace:");
                foreach (var line in ex.StackTrace.Split('\n').Take(5))
                    Console.WriteLine($"    {line.Trim()}");
            }
            
            Console.WriteLine();
            return new DicomCStoreResponse(request, DicomStatus.ProcessingFailure);
        }
    }

    public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] C-STORE EXCEPTION");
        Console.WriteLine($"  Temp File: {tempFileName}");
        Console.WriteLine($"  Exception: {e.GetType().Name}");
        Console.WriteLine($"  Message: {e.Message}");
        
        if (VerboseLogging && e.StackTrace != null)
        {
            Console.WriteLine($"  Stack Trace:");
            foreach (var line in e.StackTrace.Split('\n').Take(5))
                Console.WriteLine($"    {line.Trim()}");
        }
        
        Console.WriteLine();
        return Task.CompletedTask;
    }

    public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] C-ECHO REQUEST");

        if (VerboseLogging)
        {
            Console.WriteLine($"  Message ID: {request.MessageID}");
        }

        Console.WriteLine($"  Result: SUCCESS");
        Console.WriteLine();

        return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
    }

    // Storage Commitment N-ACTION handler
    public async Task<DicomNActionResponse> OnNActionRequestAsync(DicomNActionRequest request)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] >>> N-ACTION REQUEST (Storage Commitment) <<<");

        var dataset = request.Dataset;
        var transactionUID = dataset.GetSingleValueOrDefault(DicomTag.TransactionUID, "unknown");
        
        // Capture the calling AE for sending N-EVENT-REPORT response
        var callingAE = Association?.CallingAE ?? "UNKNOWN";
        lock (_storageLock)
        {
            _transactionCallingAE[transactionUID] = callingAE;
        }

        Console.WriteLine($"  Transaction UID: {transactionUID}");
        Console.WriteLine($"  Action Type ID: {request.ActionTypeID}");
        Console.WriteLine($"  Calling AE: {callingAE}");

        // Get list of referenced SOP instances to commit
        if (dataset.Contains(DicomTag.ReferencedSOPSequence))
        {
            var sopSequence = dataset.GetSequence(DicomTag.ReferencedSOPSequence);
            Console.WriteLine($"  Requested SOP Instances: {sopSequence.Items.Count}");

            var successInstances = new List<string>();
            var failedInstances = new List<string>();

            lock (_storageLock)
            {
                foreach (var item in sopSequence.Items)
                {
                    var sopClassUID = item.GetSingleValueOrDefault(DicomTag.ReferencedSOPClassUID, "unknown");
                    var sopInstanceUID = item.GetSingleValueOrDefault(DicomTag.ReferencedSOPInstanceUID, "unknown");

                    if (VerboseLogging)
                    {
                        Console.WriteLine($"    Checking: {sopInstanceUID}");
                    }

                    // Check if we actually stored this instance
                    bool found = false;
                    foreach (var storedList in _storedInstances.Values)
                    {
                        if (storedList.Contains(sopInstanceUID))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        successInstances.Add(sopInstanceUID);
                        if (VerboseLogging)
                            Console.WriteLine($"      ✓ Found");
                    }
                    else
                    {
                        failedInstances.Add(sopInstanceUID);
                        Console.WriteLine($"      ✗ NOT FOUND");
                    }
                }

                // Store the commitment request for this transaction
                if (successInstances.Count > 0)
                {
                    _storedInstances[transactionUID] = successInstances;
                }
            }

            Console.WriteLine($"  Success: {successInstances.Count}, Failed: {failedInstances.Count}");
        }

        Console.WriteLine($"  Result: ACCEPTED (will send N-EVENT-REPORT)");
        Console.WriteLine();

        // Accept the commitment request
        var response = new DicomNActionResponse(request, DicomStatus.Success);

        // Send N-EVENT-REPORT asynchronously to confirm storage commitment
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000); // Small delay to simulate processing
            await SendStorageCommitmentEventReportAsync(transactionUID, dataset);
        });

        await Task.CompletedTask; // Suppress async warning
        return response;
    }

    // Send N-EVENT-REPORT to confirm storage commitment
    private async Task SendStorageCommitmentEventReportAsync(string transactionUID, DicomDataset requestDataset)
    {
        try
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] >>> Sending N-EVENT-REPORT <<<");
            Console.WriteLine($"  Transaction UID: {transactionUID}");

            // Get the calling AE that sent the N-ACTION request
            string callingAE = "MIGRATION_TOOL"; // Default fallback
            lock (_storageLock)
            {
                if (_transactionCallingAE.TryGetValue(transactionUID, out var ae))
                {
                    callingAE = ae;
                }
            }

            // Build event report dataset
            var eventDataset = new DicomDataset
            {
                { DicomTag.TransactionUID, transactionUID }
            };

            // Get the original requested instances
            var originalSequence = requestDataset.GetSequence(DicomTag.ReferencedSOPSequence);
            var successSequence = new DicomSequence(DicomTag.ReferencedSOPSequence);
            var failedSequence = new DicomSequence(DicomTag.FailedSOPSequence);

            int successCount = 0;
            int failedCount = 0;

            lock (_storageLock)
            {
                foreach (var item in originalSequence.Items)
                {
                    var itemSopClassUID = item.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID);
                    var itemSopInstanceUID = item.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);

                    // Check if this instance is in our stored list
                    bool found = _storedInstances.TryGetValue(transactionUID, out var storedList) &&
                                storedList.Contains(itemSopInstanceUID);

                    if (found)
                    {
                        var successItem = new DicomDataset
                        {
                            { DicomTag.ReferencedSOPClassUID, itemSopClassUID },
                            { DicomTag.ReferencedSOPInstanceUID, itemSopInstanceUID }
                        };
                        successSequence.Items.Add(successItem);
                        successCount++;
                    }
                    else
                    {
                        var failedItem = new DicomDataset
                        {
                            { DicomTag.ReferencedSOPClassUID, itemSopClassUID },
                            { DicomTag.ReferencedSOPInstanceUID, itemSopInstanceUID },
                            { DicomTag.FailureReason, (ushort)0x0110 } // Processing failure
                        };
                        failedSequence.Items.Add(failedItem);
                        failedCount++;
                    }
                }
            }

            // Add appropriate sequence based on results
            if (successCount > 0)
            {
                eventDataset.Add(successSequence);
            }
            if (failedCount > 0)
            {
                eventDataset.Add(failedSequence);
            }

            Console.WriteLine($"  Success: {successCount}, Failed: {failedCount}");
            Console.WriteLine($"  Event Type: {(failedCount > 0 ? "0x0002 (Failures exist)" : "0x0001 (All success)")}");

            // Determine event type ID
            ushort eventTypeID = failedCount > 0 ? (ushort)2 : (ushort)1;

            // ACTUALLY SEND the N-EVENT-REPORT to the calling AE
            // Assume the calling AE is listening on localhost:11113 (the Storage Commitment SCP port)
            var client = DicomClientFactory.Create("localhost", 11113, false, "TEST_SCP", callingAE);
            
            // Storage Commitment Push Model SOP Class
            var sopClassUID = new DicomUID("1.2.840.10008.1.20.1", "Storage Commitment Push Model SOP Class", DicomUidType.SOPClass);
            var sopInstanceUID = new DicomUID("1.2.840.10008.1.20.1.1", "Storage Commitment Push Model SOP Instance", DicomUidType.SOPInstance);
            
            var nEventReportRequest = new DicomNEventReportRequest(sopClassUID, sopInstanceUID, eventTypeID)
            {
                Dataset = eventDataset
            };
            
            await client.AddRequestAsync(nEventReportRequest);
            await client.SendAsync();
            
            Console.WriteLine($"  Result: N-EVENT-REPORT sent successfully to {callingAE}@localhost:11113");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error sending N-EVENT-REPORT: {ex.Message}");
            Console.WriteLine();
        }
    }

    // Storage Commitment N-EVENT-REPORT handler (incoming from SCU)
    public Task<DicomNEventReportResponse> OnNEventReportRequestAsync(DicomNEventReportRequest request)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] N-EVENT-REPORT RECEIVED");

        var dataset = request.Dataset;
        var transactionUID = dataset.GetSingleValueOrDefault(DicomTag.TransactionUID, "unknown");

        Console.WriteLine($"  Transaction UID: {transactionUID}");
        Console.WriteLine($"  Event Type ID: {request.EventTypeID}");

        if (VerboseLogging)
        {
            Console.WriteLine($"  Message ID: {request.MessageID}");
        }

        Console.WriteLine($"  Result: ACKNOWLEDGED");
        Console.WriteLine();

        return Task.FromResult(new DicomNEventReportResponse(request, DicomStatus.Success));
    }

    // N-CREATE handler (not used for Storage Commitment, but required by interface)
    public Task<DicomNCreateResponse> OnNCreateRequestAsync(DicomNCreateRequest request)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] N-CREATE REQUEST (Not Supported)");
        Console.WriteLine();
        return Task.FromResult(new DicomNCreateResponse(request, DicomStatus.SOPClassNotSupported));
    }

    // N-DELETE handler (not used for Storage Commitment, but required by interface)
    public Task<DicomNDeleteResponse> OnNDeleteRequestAsync(DicomNDeleteRequest request)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] N-DELETE REQUEST (Not Supported)");
        Console.WriteLine();
        return Task.FromResult(new DicomNDeleteResponse(request, DicomStatus.SOPClassNotSupported));
    }

    // N-GET handler (not used for Storage Commitment, but required by interface)
    public Task<DicomNGetResponse> OnNGetRequestAsync(DicomNGetRequest request)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] N-GET REQUEST (Not Supported)");
        Console.WriteLine();
        return Task.FromResult(new DicomNGetResponse(request, DicomStatus.SOPClassNotSupported));
    }

    // N-SET handler (not used for Storage Commitment, but required by interface)
    public Task<DicomNSetResponse> OnNSetRequestAsync(DicomNSetRequest request)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Conn #{_connectionId}] N-SET REQUEST (Not Supported)");
        Console.WriteLine();
        return Task.FromResult(new DicomNSetResponse(request, DicomStatus.SOPClassNotSupported));
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
