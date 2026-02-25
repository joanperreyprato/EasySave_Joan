# EasySave v3.0 - Documentation (English)

**Version:** 3.0  
**Framework:** .NET 8  
**Language:** C#  
**Last Updated:** 25/02/2026

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture](#2-architecture)
3. [Features](#3-features)
4. [Installation & Setup](#4-installation--setup)
5. [User Interfaces](#5-user-interfaces)
6. [Configuration](#6-configuration)
7. [Advanced Features](#7-advanced-features)
8. [Distributed Logging](#8-distributed-logging)
9. [Troubleshooting](#9-troubleshooting)
10. [Development](#10-development)

---

## 1. Overview

### What is EasySave?

EasySave is a professional backup application designed for small and medium-sized businesses. It provides reliable, fast, and secure backup solutions with advanced features including:

- **Full and Differential Backup**: Choose between complete copies or incremental changes
- **File Encryption**: Secure your sensitive data with AES-256 encryption
- **Priority File Processing**: Ensure critical files are backed up first
- **Parallel Execution**: Run multiple backup jobs simultaneously
- **Real-time Monitoring**: Pause, resume, and stop backups on the fly
- **Business Software Detection**: Prevents conflicts with running business applications
- **Distributed Logging**: Centralized log collection from multiple clients
- **Multi-Interface**: Command-line (CLI) and graphical (WPF) interfaces

### Key Benefits

- ✅ **Single Instance Protection**: Only one instance can run at a time
- ✅ **Maximum 5 Backup Jobs**: Optimized for SMB needs
- ✅ **Multi-Language Support**: English and French
- ✅ **Extensible Architecture**: Modular design for easy customization
- ✅ **Professional Logging**: JSON and XML formats with daily rotation
- ✅ **Docker Support**: Containerized log server for easy deployment

---

## 2. Architecture

### Project Structure

EasySave is organized into multiple projects for separation of concerns:

```
EasySave/
├── EasySave.Core/           # Core business logic
│   ├── BackupManager.cs     # Job management
│   ├── BackupExecutor.cs    # Backup execution engine
│   ├── JobController.cs     # Pause/Resume/Stop control
│   ├── StateManager.cs      # Job state persistence
│   ├── LanguageProvider.cs  # Internationalization
│   └── BusinessSoftwareMonitor.cs
│
├── EasySave.CLI/            # Command-line interface
│   ├── Program.cs           # CLI entry point
│   └── ConsoleUI.cs         # Console menus
│
├── EasySave.WPF/            # Graphical interface
│   ├── MainWindow.xaml      # Main UI
│   ├── AddJobWindow.xaml    # Job creation dialog
│   └── JobViewModel.cs      # MVVM view model
│
├── EasyLog/                 # Logging library
│   ├── JsonLogger.cs        # JSON log writer
│   ├── XmlLogger.cs         # XML log writer
│   ├── SocketLogger.cs      # Remote logging
│   └── CompositeLogger.cs   # Multi-target logging
│
├── CryptoSoft/              # Encryption library
│   └── CryptoService.cs     # AES-256 encryption
│
└── EasySave.LogServer/      # Centralized log server
    ├── Program.cs           # Server entry point
    ├── LogServerService.cs  # TCP log receiver
    └── Dockerfile           # Container image
```

### Core Components

#### BackupManager
- Manages backup job creation, deletion, and execution
- Validates job configuration (name, paths, types)
- Enforces business rules (max 5 jobs, unique names)
- Coordinates with StateManager for persistence

#### BackupExecutor
- Executes full and differential backups
- Handles large file synchronization (semaphore-based)
- Processes priority file extensions first
- Integrates with CryptoSoft for encryption
- Supports pause, resume, and cancel operations

#### JobController
- Provides real-time control over running jobs
- Manages cancellation tokens and pause events
- Thread-safe concurrent job management

#### StateManager
- Persists job states to JSON files
- Tracks progress, status, and statistics
- Provides real-time state updates

#### LanguageProvider
- Singleton pattern for language management
- Supports English and French
- Loads translations from `languages.json`
- Fallback to built-in translations

---

## 3. Features

### 3.1 Backup Types

#### Full Backup
Copies all files from source to destination, overwriting existing files.

```csharp
BackupType.Full
```

**Use Case**: Initial backup, monthly archives, disaster recovery snapshots.

#### Differential Backup
Copies only files that are new or modified since the last full backup.

```csharp
BackupType.Differential
```

**Use Case**: Daily backups, incremental updates, bandwidth-sensitive scenarios.

### 3.2 File Encryption

EasySave integrates with the CryptoSoft library to encrypt files during backup.

**Algorithm**: AES-256-CBC with HMAC-SHA256 integrity check  
**Key Derivation**: SHA-256 of user-provided password  
**Format**: `[IV Length][IV][Ciphertext][HMAC]`

**Configuration**:
- Set extensions to encrypt in `backup_config.json`
- Provide encryption key via UI or command-line
- Files are encrypted on-the-fly during backup

**Example Configuration**:
```json
{
  "encryptExtensions": [".docx", ".xlsx", ".pdf", ".txt"],
  "businessSoftwareNames": ["excel", "word", "outlook"]
}
```

### 3.3 Priority File Extensions

Files with priority extensions are backed up first, ensuring critical data is saved before less important files.

**Configuration**:
```json
{
  "priorityExtensions": [".docx", ".xlsx", ".pdf"]
}
```

**Behavior**:
1. Priority files from all jobs are processed first
2. Non-priority files wait until all priority files are complete
3. Global coordination prevents priority inversion

### 3.4 Large File Management

Large files are transferred sequentially to avoid overwhelming system resources.

**Configuration**:
- Set `largeFileThresholdKb` in BackupManager constructor
- Set to `0` to disable (parallel transfer of all files)
- Recommended: `10240` (10 MB)

**Behavior**:
- Files larger than threshold acquire a global semaphore
- Only one large file transfers at a time across all jobs
- Smaller files continue in parallel

### 3.5 Business Software Monitoring

Prevents backup conflicts by detecting running business applications.

**Configuration**:
```json
{
  "businessSoftwareNames": ["excel", "word", "outlook", "quickbooks"]
}
```

**Behavior**:
- Monitors running processes before starting backup
- Blocks backup if monitored process is detected
- Logs error with process names

### 3.6 Real-Time Job Control

Control running backups without restarting the application.

**Operations**:
- **Pause**: Suspend backup after current file completes
- **Resume**: Continue a paused backup
- **Stop**: Immediately cancel backup (sets status to Inactive)

**API**:
```csharp
jobController.PauseJob("jobName");
jobController.ResumeJob("jobName");
jobController.StopJob("jobName");
```

### 3.7 Parallel Execution

Run multiple backup jobs simultaneously to maximize throughput.

**WPF Interface**:
- "Start All" button runs all jobs in parallel
- Each job runs in a separate Task
- Progress updates in real-time

**CLI Interface**:
- Command-line: `easysave execute 1 2 3`
- Sequential execution by default

---

## 4. Installation & Setup

### 4.1 System Requirements

- **Operating System**: Windows 10/11, Windows Server 2019+
- **.NET Runtime**: .NET 8.0 or higher
- **Memory**: 512 MB minimum, 2 GB recommended
- **Disk Space**: 100 MB for application, variable for logs

### 4.2 Building from Source

#### Prerequisites
```bash
# Install .NET 8 SDK
winget install Microsoft.DotNet.SDK.8

# Clone repository
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
```

#### Build All Projects
```bash
dotnet build EasySave.sln --configuration Release
```

#### Build Specific Projects
```bash
# CLI version
dotnet build EasySave.CLI/EasySave.CLI.csproj -c Release

# WPF version
dotnet build EasySave.WPF/EasySave.WPF.csproj -c Release

# Log Server
dotnet build EasySave.LogServer/EasySave.LogServer.csproj -c Release
```

### 4.3 Installation

#### CLI Installation
```bash
# Publish self-contained executable
dotnet publish EasySave.CLI/EasySave.CLI.csproj -c Release -r win-x64 --self-contained true

# Copy to installation directory
xcopy /E /I EasySave.CLI\bin\Release\net8.0\win-x64\publish C:\EasySave
```

#### WPF Installation
```bash
# Publish self-contained executable
dotnet publish EasySave.WPF/EasySave.WPF.csproj -c Release -r win-x64 --self-contained true

# Copy to installation directory
xcopy /E /I EasySave.WPF\bin\Release\net8.0-windows\win-x64\publish C:\EasySave
```

### 4.4 First Run Setup

1. **Launch the application** (CLI or WPF)
2. **Configuration files** are created automatically in:
   - `%ProgramData%\EasySave\backup_config.json`
   - `%ProgramData%\EasySave\language.json`
   - `%ProgramData%\EasySave\state.json`
3. **Log directory** is created at:
   - `%ProgramData%\EasySave\logs\`

---

## 5. User Interfaces

### 5.1 Command-Line Interface (CLI)

#### Interactive Mode

Launch without arguments:
```bash
EasySave.CLI.exe
```

**Main Menu**:
```
=== EasySave ===
1. Create backup job
2. Execute backup
3. List backup jobs
4. Edit backup job
5. Change language
6. Exit
```

#### Command-Line Mode

**List all jobs**:
```bash
EasySave.CLI.exe list
```

**Create a job**:
```bash
EasySave.CLI.exe create "MyBackup" "C:\Source" "D:\Backup" full
EasySave.CLI.exe create "MyBackup" "C:\Source" "D:\Backup" diff
```

**Execute jobs**:
```bash
# Execute job 1
EasySave.CLI.exe execute 1

# Execute jobs 1, 2, and 3
EasySave.CLI.exe execute 1 2 3

# Alternative: direct execution
EasySave.CLI.exe 1-3
EasySave.CLI.exe 1,2,3
```

**Specify log directory**:
```bash
EasySave.CLI.exe --logdir "C:\MyLogs" list
EasySave.CLI.exe -l=C:\MyLogs execute 1
```

### 5.2 WPF Graphical Interface

#### Main Window Features

**Job List**:
- View all configured backup jobs
- Real-time progress bars
- Status indicators (Stopped, Active, Paused, Completed, Error)

**Control Buttons**:
- **Add Job**: Create new backup job
- **Start All**: Run all jobs in parallel
- **Stop All**: Cancel all running jobs
- **Pause All**: Pause all active jobs
- **Start Selected**: Run selected job
- **Stop Selected**: Cancel selected job
- **Pause Selected**: Pause selected job
- **Resume Selected**: Resume paused job
- **Delete Selected**: Remove job from configuration

**Settings**:
- **Language**: English / Français
- **Log Format**: JSON / XML
- **Large File Threshold**: KB value (0 = disabled)
- **Encryption Key**: Password for file encryption

#### Adding a Backup Job

1. Click "**Add Job**"
2. Enter job details:
   - **Name**: Alphanumeric, max 50 characters
   - **Source**: Existing directory path
   - **Destination**: Existing directory path
   - **Type**: Full or Differential
3. Click "**Create**"

#### Running a Backup

1. Select job(s) from the list
2. Click "**Start Selected**" or "**Start All**"
3. Monitor progress in real-time
4. Use control buttons to pause/resume/stop as needed

---

## 6. Configuration

### 6.1 Backup Configuration File

**Location**: `%ProgramData%\EasySave\backup_config.json`

**Example**:
```json
{
  "backupJobs": [
    {
      "name": "DocumentsBackup",
      "sourcePath": "C:\\Users\\User\\Documents",
      "targetPath": "D:\\Backups\\Documents",
      "type": "Full",
      "logFormat": "Json"
    }
  ],
  "encryptExtensions": [".docx", ".xlsx", ".pdf", ".txt"],
  "businessSoftwareNames": ["excel", "word", "outlook"],
  "priorityExtensions": [".docx", ".xlsx"]
}
```

**Fields**:
- `backupJobs`: Array of backup job definitions
- `encryptExtensions`: File extensions to encrypt during backup
- `businessSoftwareNames`: Process names to monitor (without .exe)
- `priorityExtensions`: Extensions to process first

### 6.2 Application Settings

**Location**: `EasySave.WPF\appsettings.json` or `EasySave.CLI\appsettings.json`

**Example**:
```json
{
  "RemoteLogging": {
    "Enabled": false,
    "ServerHost": "192.168.1.100",
    "ServerPort": 5555,
    "EnableLocalLogging": true
  },
  "Logging": {
    "LogDirectory": "./logs",
    "LogFormat": "Json"
  }
}
```

**Fields**:
- `RemoteLogging.Enabled`: Enable centralized log server
- `RemoteLogging.ServerHost`: IP or hostname of log server
- `RemoteLogging.ServerPort`: TCP port (default: 5555)
- `RemoteLogging.EnableLocalLogging`: Keep local logs when remote is enabled
- `Logging.LogDirectory`: Local log directory path
- `Logging.LogFormat`: "Json" or "Xml"

### 6.3 Language Configuration

**Location**: `%ProgramData%\EasySave\language.json`

**Content**:
```json
{
  "currentLanguage": "en"
}
```

Change to `"fr"` for French.

### 6.4 State File

**Location**: `%ProgramData%\EasySave\state.json`

Contains runtime state for all backup jobs. Automatically managed by StateManager.

**Example**:
```json
{
  "DocumentsBackup": {
    "name": "DocumentsBackup",
    "status": "Completed",
    "totalFiles": 150,
    "totalSize": 52428800,
    "remainingFiles": 0,
    "remainingSize": 0,
    "progress": 100.0,
    "currentSourceFile": "report.docx",
    "currentTargetFile": "D:\\Backups\\Documents\\report.docx",
    "timestamp": "2024-01-15T10:30:45Z"
  }
}
```

---

## 7. Advanced Features

### 7.1 File Encryption

#### Enabling Encryption

1. Add extensions to `encryptExtensions` in `backup_config.json`
2. Set encryption key in UI (WPF) or via prompt (CLI)
3. Run backup - matching files are encrypted automatically

#### Encryption Details

- **Algorithm**: AES-256-CBC
- **Integrity**: HMAC-SHA256
- **Key Derivation**: SHA-256(password)
- **IV**: Random, stored with ciphertext

#### Decrypting Files

Use the CryptoSoft library or WPF decrypt feature:

```csharp
CryptoService.DecryptFile(inputPath, outputPath, password);
```

**WPF Decrypt Dialog**:
1. Click "**Decrypt File**" (if available)
2. Select encrypted file
3. Enter password
4. Choose output location

### 7.2 Differential Backup

Differential backups compare file modification times:

```csharp
// File is eligible for differential backup if:
// - File doesn't exist in target, OR
// - Source file is newer than target file

DateTime sourceTime = File.GetLastWriteTimeUtc(sourceFile);
DateTime targetTime = File.GetLastWriteTimeUtc(targetFile);

if (!File.Exists(targetFile) || sourceTime > targetTime)
{
    // Copy file
}
```

**Advantages**:
- Faster than full backup
- Less bandwidth usage
- Smaller backup sets

**Considerations**:
- Requires full backup as baseline
- Doesn't detect deletions
- Multiple differentials can accumulate

### 7.3 Priority File Processing

Priority files are processed before non-priority files globally across all jobs.

**Implementation**:
```csharp
// Global counter tracks pending priority files
private static int PendingPriorityFilesCount = 0;

// Priority files increment counter
lock (PriorityLock)
{
    PendingPriorityFilesCount += priorityFiles.Count;
}

// Non-priority files wait for counter to reach zero
while (PendingPriorityFilesCount > 0)
{
    Thread.Sleep(100);
}
```

**Use Cases**:
- Ensure critical documents backup first
- Database files before log files
- Configuration files before media files

### 7.4 Large File Synchronization

Prevents multiple large files from transferring simultaneously.

**Configuration**:
```csharp
BackupManager manager = new BackupManager(
    stateManager, 
    logger, 
    configPath: null, 
    largeFileThresholdKb: 10240 // 10 MB
);
```

**Behavior**:
- Files > threshold acquire `LargeFileSemaphore`
- Only one large file transfers globally
- Small files continue in parallel

### 7.5 Business Software Monitoring

Prevents backup conflicts with open files.

**Configuration**:
```json
{
  "businessSoftwareNames": ["excel", "word", "outlook"]
}
```

**Monitoring Logic**:
```csharp
if (businessMonitor.IsBusinessSoftwareRunning())
{
    Console.WriteLine("Backup blocked: Business software running");
    state.Status = StateStatus.Error;
    return;
}
```

**Process Detection**:
- Uses `Process.GetProcesses()` to enumerate
- Compares process names (case-insensitive)
- Removes ".exe" suffix for comparison

---

## 8. Distributed Logging

### 8.1 Overview

EasySave supports centralized logging via TCP socket to a dedicated log server.

**Architecture**:
```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│ EasySave CLI │────────▶│  Log Server  │◀────────│ EasySave WPF │
│  Client 1    │  TCP    │   (Docker)   │  TCP    │  Client 2    │
└──────────────┘ :5555   └──────────────┘ :5555   └──────────────┘
       │                         │
       │                         │
       ▼                         ▼
  Local Logs             Centralized Logs
```

### 8.2 Log Server Setup

#### Docker Deployment (Recommended)

**Prerequisites**:
- Docker Desktop installed

**Using Docker Compose**:
```bash
# At repository root
docker-compose up -d
```

**Manual Docker Build**:
```bash
# Build image
docker build -f EasySave.LogServer/Dockerfile -t easysave-logserver:latest .

# Run container
docker run -d \
  --name easysave-logserver \
  -p 5555:5555 \
  -v $(pwd)/logs:/app/logs \
  -e LOG_SERVER_PORT=5555 \
  -e LOG_SERVER_DIRECTORY=/app/logs \
  easysave-logserver:latest
```

#### Native Deployment

```bash
# Build server
dotnet build EasySave.LogServer/EasySave.LogServer.csproj -c Release

# Run server
cd EasySave.LogServer/bin/Release/net8.0
EasySave.LogServer.exe
```

**Environment Variables**:
- `LOG_SERVER_PORT`: TCP port (default: 5555)
- `LOG_SERVER_DIRECTORY`: Log storage path (default: `/app/logs`)

### 8.3 Client Configuration

**Edit `appsettings.json`**:
```json
{
  "RemoteLogging": {
    "Enabled": true,
    "ServerHost": "192.168.1.100",
    "ServerPort": 5555,
    "EnableLocalLogging": true
  }
}
```

**Fields**:
- `Enabled`: Set to `true` to enable remote logging
- `ServerHost`: IP address or hostname of log server
- `ServerPort`: TCP port (must match server)
- `EnableLocalLogging`: If `true`, logs are written locally AND remotely

### 8.4 Log Format

**JSON Log Entry**:
```json
{
  "timestamp": "2024-01-15T10:30:45.123Z",
  "backupName": "DocumentsBackup",
  "sourceFilePath": "\\\\MACHINE1\\C$\\Users\\User\\Documents\\report.docx",
  "targetFilePath": "\\\\BACKUP_SERVER\\Backups\\Documents\\report.docx",
  "fileSize": 1048576,
  "transferTime": 1250,
  "encryptionTime": 0,
  "type": "Full",
  "machineName": "WORKSTATION-01"
}
```

### 8.5 Log Organization

Logs are organized by client machine:

```
logs/
├── WORKSTATION-01/
│   ├── 2024-01-15.json
│   ├── 2024-01-16.json
│   └── 2024-01-17.json
├── WORKSTATION-02/
│   ├── 2024-01-15.json
│   └── 2024-01-16.json
└── 192.168.1.50/
    └── 2024-01-15.json
```

**Naming Convention**:
- Directory: `MachineName` or IP address
- File: `YYYY-MM-DD.json`
- Daily rotation: New file created automatically each day

### 8.6 Server Management

**View real-time logs**:
```bash
docker logs -f easysave-logserver
```

**Stop server**:
```bash
docker stop easysave-logserver
```

**Restart server**:
```bash
docker restart easysave-logserver
```

**Remove server**:
```bash
docker stop easysave-logserver
docker rm easysave-logserver
```

**Backup server logs**:
```bash
tar -czf logs-backup-$(date +%Y%m%d).tar.gz logs/
```

---

## 9. Troubleshooting

### 9.1 Common Issues

#### "Another instance is already running"

**Cause**: Mutex prevents multiple instances  
**Solution**: Close other EasySave windows or kill process

```bash
# Find process
tasklist | findstr EasySave

# Kill process
taskkill /F /IM EasySave.CLI.exe
taskkill /F /IM EasySave.WPF.exe
```

#### "Source path not found"

**Cause**: Directory doesn't exist or incorrect path  
**Solution**: Verify path and ensure directory exists

```bash
# Check if path exists
dir "C:\Source\Path"

# Create directory if needed
mkdir "C:\Source\Path"
```

#### "Cannot create backup job - max jobs exceeded"

**Cause**: Limit of 5 jobs enforced  
**Solution**: Delete unused jobs before creating new ones

#### "Backup blocked: Business software running"

**Cause**: Monitored process detected  
**Solution**: Close business applications or remove from `businessSoftwareNames`

#### Remote logging not working

**Causes**:
- Server not running
- Firewall blocking port 5555
- Incorrect server IP/port in config

**Solutions**:
```bash
# Check server is running
docker ps | grep logserver

# Test connectivity
telnet 192.168.1.100 5555

# Check firewall (Windows)
netsh advfirewall firewall add rule name="EasySave LogServer" dir=in action=allow protocol=TCP localport=5555
```

### 9.2 Log Analysis

**View backup logs**:
```bash
# JSON logs
type %ProgramData%\EasySave\logs\2024-01-15.json

# Filter specific backup
findstr "DocumentsBackup" %ProgramData%\EasySave\logs\2024-01-15.json
```

**Count backed up files**:
```powershell
(Get-Content "$env:ProgramData\EasySave\logs\2024-01-15.json" | ConvertFrom-Json).Count
```

**Calculate total transferred size**:
```powershell
(Get-Content "$env:ProgramData\EasySave\logs\2024-01-15.json" | ConvertFrom-Json | 
    Measure-Object -Property fileSize -Sum).Sum / 1MB
```

### 9.3 Performance Optimization

#### Slow backup performance

**Solutions**:
1. **Increase large file threshold**: Fewer synchronization points
2. **Reduce priority extensions**: Less coordination overhead
3. **Disable encryption**: Significant performance gain
4. **Use SSD for backup destination**: Faster write speeds
5. **Adjust network settings**: For network backup destinations

#### High memory usage

**Solutions**:
1. **Lower large file threshold**: Reduces parallel transfers
2. **Disable remote logging**: Reduces buffering
3. **Close other applications**: Free up system resources

#### Network congestion

**Solutions**:
1. **Schedule backups during off-hours**
2. **Use differential backups**: Less data transferred
3. **Enable large file synchronization**: Prevents bandwidth saturation
4. **Compress files before backup**: Reduce transfer size

---

## 10. Development

### 10.1 Project Structure

**Solution**: `EasySave.sln`

**Projects**:
1. **EasySave.Core** (Class Library)
   - Target: .NET 8
   - Purpose: Core business logic
   - Dependencies: None

2. **EasySave.CLI** (Console Application)
   - Target: .NET 8
   - Purpose: Command-line interface
   - Dependencies: EasySave.Core, EasyLog, CryptoSoft

3. **EasySave.WPF** (WPF Application)
   - Target: .NET 8 (Windows)
   - Purpose: Graphical interface
   - Dependencies: EasySave.Core, EasyLog, CryptoSoft

4. **EasyLog** (Class Library)
   - Target: .NET 8
   - Purpose: Logging infrastructure
   - Dependencies: None

5. **CryptoSoft** (Class Library)
   - Target: .NET 8
   - Purpose: File encryption
   - Dependencies: None

6. **EasySave.LogServer** (Console Application)
   - Target: .NET 8
   - Purpose: Centralized log server
   - Dependencies: None

7. **EasySave.Tests** (Unit Test Project)
   - Target: .NET 8
   - Purpose: Automated tests
   - Dependencies: All projects

### 10.2 Building

#### Debug Build
```bash
dotnet build EasySave.sln --configuration Debug
```

#### Release Build
```bash
dotnet build EasySave.sln --configuration Release
```

#### Single Project
```bash
dotnet build EasySave.Core/EasySave.Core.csproj -c Release
```

### 10.3 Testing

```bash
# Run all tests
dotnet test EasySave.Tests/EasySave.Tests.csproj

# Run with detailed output
dotnet test --verbosity detailed

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

### 10.4 Publishing

#### Self-Contained Executable
```bash
dotnet publish EasySave.CLI/EasySave.CLI.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o ./publish/cli
```

#### Framework-Dependent Deployment
```bash
dotnet publish EasySave.WPF/EasySave.WPF.csproj \
  -c Release \
  -r win-x64 \
  --self-contained false \
  -o ./publish/wpf
```

### 10.5 Extending EasySave

#### Adding a New Backup Type

1. Define enum value in `BackupType.cs`:
```csharp
public enum BackupType
{
    Full,
    Differential,
    Incremental  // New type
}
```

2. Implement execution logic in `BackupExecutor.cs`:
```csharp
public void ExecuteIncrementalBackup(BackupJob job)
{
    // Implementation
}
```

3. Update `BackupManager.ExecuteBackupJob()`:
```csharp
if (job.GetBackupType() == BackupType.Incremental)
    executor.ExecuteIncrementalBackup(job);
```

#### Adding a New Log Format

1. Create logger class in `EasyLog`:
```csharp
public class CsvLogger : ILogger
{
    public LogFormat Format => LogFormat.Csv;
    
    public void LogAction(LogEntry entry)
    {
        // CSV formatting logic
    }
}
```

2. Register in `LoggerFactory.cs`:
```csharp
ILogger localLogger = logFormat.ToLower() switch
{
    "xml" => new XmlLogger(logDirectory),
    "csv" => new CsvLogger(logDirectory),
    _ => new JsonLogger(logDirectory)
};
```

#### Adding New Language

1. Add translations to `languages.json`:
```json
{
  "en": { ... },
  "fr": { ... },
  "es": {
    "menu.create": "Crear trabajo de copia de seguridad",
    "menu.execute": "Ejecutar copia de seguridad",
    ...
  }
}
```

2. Language is automatically loaded by LanguageProvider

### 10.6 Code Standards

- **C# Version**: 12.0
- **Target Framework**: .NET 8
- **Nullable**: Enabled
- **Code Analysis**: Enabled (CA rules)
- **Naming**: PascalCase for public, camelCase for private
- **Documentation**: XML comments for public APIs

---

## Support & Contributions

**Version:** 3.0  
**Date:** 2024  
**Maintainer:** Joan Perreyprato  
**Repository:** https://github.com/joanperreyprato/EasySave  
**License:** Proprietary - SMB Backup Solution

For issues, questions, or feature requests, please visit the GitHub repository.
