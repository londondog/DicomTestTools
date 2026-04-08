# DICOM SCP Test Tool

A simple DICOM C-STORE SCP (Service Class Provider) for testing and troubleshooting DICOM transfers.

## Features

- **Accepts C-STORE requests** from any DICOM SCU
- **Extensive logging** with millisecond timestamps
- **Verbose mode** for detailed debugging
- **Automatic file organization** by Patient/Study/Series
- **Connection tracking** with unique IDs
- **Performance metrics** (file sizes, save times, durations)

## Usage

### Basic (default settings)
```bash
dotnet run
```
Default: Port 11112, AE Title "TEST_SCP", storage in `.\DicomStorage`

### Custom port
```bash
dotnet run 11113
```

### Custom port and AE Title
```bash
dotnet run 11113 MY_SCP
```

### Custom storage folder
```bash
dotnet run 11113 MY_SCP "C:\MyDicomFiles"
```

### Enable verbose logging
```bash
dotnet run -v
dotnet run 11113 MY_SCP -v
dotnet run 11113 MY_SCP "C:\MyDicomFiles" --verbose
```

## What Gets Logged

### Standard Mode
- Connection establishment with unique ID
- Association details (Calling/Called AE, Max PDU, Implementation)
- Presentation contexts list
- Each C-STORE request with:
  - Patient ID and Name
  - Study UID and Date
  - Series UID, Number, and Modality
  - Instance SOP UID and Number
  - File size and save time
  - Full file path
- Association release/abort
- Connection duration
- Errors with type and message

### Verbose Mode (add `-v` or `--verbose`)
Additionally logs:
- Encoding information
- Async operations window settings
- Full presentation context details with UIDs
- All transfer syntaxes for each context
- SOP Class UID
- Transfer syntax used
- Message ID and Priority
- Accession Number (if present)
- Study Description (if present)
- Series Description (if present)
- Directory creation details
- Full exception stack traces

## File Organization

Files are automatically saved in this structure:
```
DicomStorage/
  ├── [PatientID]/
  │   ├── [StudyInstanceUID]/
  │   │   ├── [SeriesInstanceUID]/
  │   │   │   ├── [SOPInstanceUID].dcm
  │   │   │   ├── [SOPInstanceUID].dcm
  │   │   │   └── ...
```

## Example Output

```
=== DICOM SCP Test Tool ===
Started at: 2026-01-12 14:30:00

Configuration:
  AE Title:       TEST_SCP
  Port:           11112
  Storage Folder: C:\...\DicomStorage
  Verbose:        true

Usage: DicomScpTestTool [port] [aet] [storage_folder] [-v|--verbose]

Press Ctrl+C to stop...
================================================================================

[14:30:05.123] Starting DICOM SCP on port 11112...
[14:30:05.234] DICOM SCP is running and listening for connections

[14:30:15.456] [Conn #1] New connection established
[14:30:15.467] [Conn #1] === ASSOCIATION REQUEST ===
  Calling AE Title: MIGRATION_TOOL
  Called AE Title:  TEST_SCP
  Max PDU Length:   262144
  Implementation:   1.2.826.0.1.3680043.8.498.1
  Version:          fo-dicom 5.2.2
  Presentation Contexts (3):
    [1] CT Image Storage
    [3] MR Image Storage
    [5] Secondary Capture Image Storage
  Status: ACCEPTED (3 contexts)

[14:30:15.489] [Conn #1] >>> C-STORE REQUEST #1 <<<
  Patient:
    ID:             12345
    Name:           DOE^JOHN
  Study:
    UID:            1.2.840.113619.2.55.3.123456789
    Date:           20260112
  Series:
    UID:            1.2.840.113619.2.55.3.123456789.1
    Number:         1
    Modality:       CT
  Instance:
    SOP UID:        1.2.840.113619.2.55.3.123456789.1.1
    Number:         1
  Result: SUCCESS
  File Size: 524,288 bytes
  Save Time: 12.34 ms
  Path: C:\...\DicomStorage\12345\1.2.840...\1.2.840...\1.2.840....dcm

[14:30:16.501] [Conn #1] Association released normally
  Duration: 1.05 seconds
```

## Storage Commitment Support

This SCP now supports DICOM Storage Commitment verification:

### How It Works

1. **C-STORE Phase**: When files are received via C-STORE, their SOP Instance UIDs are tracked internally
2. **N-ACTION Request**: When a Storage Commitment N-ACTION request is received:
   - Validates that all requested SOP instances were actually stored
   - Immediately accepts the request with success status
3. **N-EVENT-REPORT Response**: After a 1-second processing delay, sends N-EVENT-REPORT:
   - **Event Type 0x0001**: All instances successfully stored
   - **Event Type 0x0002**: One or more instances failed (with failure reasons)

### Storage Commitment Log Output

When a Storage Commitment request is received, you'll see:

```
[14:23:45.123] [Conn #2] >>> N-ACTION REQUEST (Storage Commitment) <<<
  Transaction UID: 1.2.840.113619.2.55.3.12345678.789
  Action Type ID: 1
  Requested SOP Instances: 135
  Success: 135, Failed: 0
  Result: ACCEPTED (will send N-EVENT-REPORT)

[14:23:46.234] [Conn #2] >>> Sending N-EVENT-REPORT <<<
  Transaction UID: 1.2.840.113619.2.55.3.12345678.789
  Success: 135, Failed: 0
  Event Type: 0x0001 (All success)
  Result: N-EVENT-REPORT sent successfully
```

### Testing Storage Commitment

To test Storage Commitment with the Migration Tool:

1. Start this SCP: `dotnet run 11112 TEST_SCP -v`
2. Configure a C-STORE destination in Migration Tool:
   - AE Title: TEST_SCP
   - Host: localhost
   - Port: 11112
   - **Enable Storage Commitment**: Yes
   - Storage Commitment Timeout: 60 seconds
3. Send a study - watch for:
   - C-STORE requests (individual images)
   - N-ACTION request (commitment request)
   - N-EVENT-REPORT (commitment confirmation)

### Verbose Mode for Storage Commitment

Enable verbose mode (`-v`) to see individual SOP Instance UIDs being validated:

```
[14:23:45.123] [Conn #2] >>> N-ACTION REQUEST (Storage Commitment) <<<
  Transaction UID: 1.2.840.113619.2.55.3.12345678.789
  Action Type ID: 1
  Requested SOP Instances: 3
    Checking: 1.2.840.113619.2.55.3.1234567.1
      ✓ Found
    Checking: 1.2.840.113619.2.55.3.1234567.2
      ✓ Found
    Checking: 1.2.840.113619.2.55.3.1234567.3
      ✗ NOT FOUND
  Success: 2, Failed: 1
```

## Troubleshooting Tips

1. **No connections?** Check firewall settings
2. **Connection immediately closes?** Enable verbose mode to see association details
3. **Files not saving?** Check write permissions on storage folder
4. **Want to see everything?** Use verbose mode: `dotnet run -v`
5. **Storage Commitment not working?** Ensure presentation contexts include Storage Commitment Push Model SOP Class

## Testing with Migration Tool

1. Start this SCP: `dotnet run 11112 TEST_SCP -v`
2. In Migration Tool, add destination server:
   - AE Title: TEST_SCP
   - Host: localhost
   - Port: 11112
3. Send a study - watch the detailed logs here!

## Stop the Server

Press `Ctrl+C` to gracefully shutdown.
