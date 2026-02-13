# EasySave 2.0 - Technical Documentation

Guide for developers and contributors

---

## Architecture

### Overview

```
???????????????????????????????????????????????
?           User Interface                    ?
???????????????????????????????????????????????
?   EasySave.WPF   ?      EasySave.UI         ?
?   (WPF GUI)      ?      (Console CLI)       ?
???????????????????????????????????????????????
         ?                   ?
         ?????????????????????
                    ?
         ?????????????????????????
         ?   EasySave v1.0       ?
         ?   (Core Library)      ?
         ?                       ?
         ?  - BackupManager      ?
         ?  - BackupJob          ?
         ?  - BackupExecutor     ?
         ?  - StateManager       ?
         ?  - LanguageProvider   ?
         ?????????????????????????
              ?          ?
     ???????????????  ??????????????
     ?   EasyLog   ?  ? CryptoSoft ?
     ?  (Logging)  ?  ?(Encryption)?
     ???????????????  ??????????????
```

---

## Main Components

### 1. EasySave.WPF (Graphical Interface)

**MainWindow.xaml.cs**
- Manages WPF interface
- `ObservableCollection<JobViewModel>` for binding
- Button event handling
- Encryption tab
- Dynamic language switching

**Key features**:
```csharp
// Add job
BtnAddJob.Click += (_, __) => AddJob();

// Execute with optional encryption
void StartBackupByIndex(int oneBasedIndex) {
    // Ask user
    // Execute backup
    // If encryption: encrypt files after copy
}
```

### 2. EasySave.UI (Console)

**Program.cs**
- Interactive mode (menu)
- Command mode (`create`, `execute`, `list`)
- CLI argument parsing
- Multilingual support

**Commands**:
```csharp
// Parse indices: "1-3" or "1,3,5"
int[] indices = CommandLineParser.ParseArguments(tokens);

// Execute
backupManager.ExecuteBackupJobs(indices);
```

### 3. EasySave v1.0 (Core Library)

#### BackupManager
- Job creation and validation
- Persistence in `backup_config.json`
- Execution orchestration

```csharp
public enum BackupCreationError {
    None, InvalidName, SourcePathInvalid, 
    SourcePathNotFound, TargetPathInvalid, 
    TargetPathNotFound, DuplicateName, MaxJobsExceeded
}

public bool CreateBackupJob(string name, string source, 
    string target, BackupType type, out BackupCreationError error)
```

#### BackupJob
- Represents a backup job
- `Execute()` method performs copy
- Updates state in real-time

```csharp
public void Execute(ILogger logger, StateManager stateManager) {
    // List files
    // Copy file by file
    // Update progress
    // Log actions
}
```

#### BackupExecutor
- Executes Full and Differential backups
- Progress calculation
- Silent error handling

#### StateManager
- Loads/saves `state.json`
- Updates state in real-time
- Provides current state for monitoring

```csharp
public class JobState {
    public string Name { get; set; }
    public DateTime Timestamp { get; set; }
    public StateStatus Status { get; set; }
    public int TotalFiles { get; set; }
    public long TotalSize { get; set; }
    public int RemainingFiles { get; set; }
    public long RemainingSize { get; set; }
    public double Progress { get; set; }
}
```

#### LanguageProvider
- Multilingual singleton
- FR and EN support
- Automatic fallback

```csharp
public static LanguageProvider GetInstance(string language = "en")
public string GetText(string key)
public void SetLanguage(string languageCode)
```

### 4. EasyLog (Logging System)

**ILogger Interface**
```csharp
public interface ILogger {
    void LogAction(LogEntry entry);
}
```

**JsonLogger / XmlLogger**
- Logs to `C:\ProgramData\EasySave\logs\`
- JSON or XML format
- Automatic timestamp

**LogEntry**
```csharp
public class LogEntry {
    public string JobName { get; set; }
    public string SourcePath { get; set; }
    public string TargetPath { get; set; }
    public long FileSize { get; set; }
    public long TransferTime { get; set; }
    public long EncryptionTime { get; set; } // -1 if error
    public EntryType Type { get; set; } // File or Summary
}
```

### 5. CryptoSoft (Encryption)

**CryptoService**
```csharp
public static class CryptoService {
    // Encrypt file with AES-256-CBC
    public static void EncryptFile(string input, string output, string key)
    
    // Decrypt file
    public static void DecryptFile(string input, string output, string key)
    
    // Encrypt stream, returns bytes
    public static byte[] Encrypt(Stream inputStream, string key)
    
    // Decrypt stream, returns bytes
    public static byte[] Decrypt(Stream inputStream, string key)
}
```

**Encrypted format**:
```
[1 byte: IV length][IV bytes][Ciphertext][32 bytes: HMAC-SHA256]
```

**Algorithm**:
- Derived key: `SHA256(key_text)`
- Encryption: AES-256-CBC
- Integrity: HMAC-SHA256

---

## Execution Flow

### Job Creation (WPF)

```
User clicks "Add job"
  ?
  ?
AddJobWindow dialog
  ?
  ?
User fills form ? OK
  ?
  ?
BackupManager.CreateBackupJob()
  ?
  ??? Validate name (alphanumeric)
  ??? Check source exists
  ??? Check target exists
  ??? Check no duplicate
  ??? Check limit (5 jobs)
  ?
  ?
Add to Jobs list
  ?
  ?
Save to backup_config.json
  ?
  ?
Update UI
```

### Execution with Encryption (WPF)

```
User clicks "Start selected"
  ?
  ?
For each selected job:
  ?
  ??? Ask user: "Encrypt?"
  ?    ?
  ?    ??? No ? Execute backup normally
  ?    ?
  ?    ??? Yes
  ?         ?
  ?         ??? Check encryption key exists
  ?         ?
  ?         ??? Execute backup (copy files)
  ?         ?
  ?         ??? For each copied file:
  ?              ?
  ?              ??? Check extension in filter list
  ?              ?
  ?              ??? If match:
  ?              ?    ?
  ?              ?    ??? CryptoService.EncryptFile()
  ?              ?    ??? Measure encryption time
  ?              ?    ??? Replace original with encrypted
  ?              ?    ??? Log encryption time
  ?              ?
  ?              ??? Continue with next file
  ?
  ?
Update state to "Completed"
```

### Differential Backup

```
BackupExecutor.ExecuteDifferentialBackup()
  ?
  ??? List all files in source
  ?
  ??? For each file:
  ?    ?
  ?    ??? Check if exists in target
  ?    ?
  ?    ??? If not exists ? Include
  ?    ?
  ?    ??? If exists:
  ?         ?
  ?         ??? Compare modification date
  ?         ??? Compare size
  ?         ?
  ?         ??? If different ? Include
  ?
  ??? Copy only included files
  ?
  ??? Log results
```

---

## Data Validation

### Job Name
```csharp
Regex.IsMatch(name, @"^[a-zA-Z0-9\s\-_]+$")
name.Length <= 50
```

### Paths
```csharp
Path.GetFullPath(path) // Validate format
Directory.Exists(path)  // Check existence
```

---

## Error Handling

### Strategy
- **Early validation**: Before persistence
- **Silent errors**: Continue despite individual file error
- **Logs**: `transferTime: -1` or `encryptionTime: -1` indicate error
- **Localized messages**: According to active language

### Creation Error Codes
```csharp
InvalidName             ? "Invalid name"
SourcePathNotFound      ? "Source not found"
TargetPathNotFound      ? "Destination not found"
DuplicateName           ? "Name already used"
MaxJobsExceeded         ? "Limit reached (5 max)"
```

---

## Design Patterns

### 1. Singleton
```csharp
// LanguageProvider
public static LanguageProvider GetInstance(string language = "en")
```

### 2. Dependency Injection
```csharp
public ConsoleUI(BackupManager manager, ILanguageProvider language)
public BackupManager(StateManager stateManager, ILogger logger = null)
```

### 3. Observer (via WPF)
```csharp
public ObservableCollection<JobViewModel> Jobs { get; }
```

### 4. Strategy
```csharp
ILogger logger = useXml ? new XmlLogger(logDir) : new JsonLogger(logDir);
```

---

## Extensibility

### Add a New Backup Type

1. Add to enum:
```csharp
public enum BackupType {
    Full = 0,
    Differential = 1,
    Incremental = 2  // New
}
```

2. Implement in BackupExecutor:
```csharp
public void ExecuteIncrementalBackup(BackupJob job) {
    // Implementation
}
```

3. Route in BackupManager:
```csharp
if (type == BackupType.Incremental)
    executor.ExecuteIncrementalBackup(job);
```

### Add a New Language

1. Modify LanguageProvider:
```csharp
translations["es"] = new Dictionary<string, string> {
    ["window.title"] = "EasySave 2.0",
    // ... other keys
};
```

2. Add UI option (WPF or CLI)

---

## Testing

### Recommended Unit Tests

```csharp
// Validation
[Test] void IsValidName_ValidName_ReturnsTrue()
[Test] void IsValidName_InvalidChars_ReturnsFalse()
[Test] void IsValidPath_InvalidPath_ReturnsFalse()

// Creation
[Test] void CreateBackupJob_ValidParams_Succeeds()
[Test] void CreateBackupJob_DuplicateName_Fails()

// Execution
[Test] void ExecuteFullBackup_CopiesAllFiles()
[Test] void ExecuteDifferentialBackup_OnlyModifiedFiles()

// Encryption
[Test] void EncryptFile_ValidKey_Succeeds()
[Test] void DecryptFile_CorrectKey_RestoresOriginal()
[Test] void DecryptFile_WrongKey_ThrowsException()
```

---

## Performance

### Current Optimizations
- Native `File.Copy()` (OS optimized)
- Buffered logs
- State saved only after each file

### Limitations
- Single-threaded (files copied sequentially)
- No compression
- Encryption adds ~30-50% time

### Future Improvements
- Multi-threading for parallel copy
- Optional compression before encryption
- File metadata caching for Differential
- Real-time UI progress (polling state.json)

---

## Build Configuration

### Projects
```
EasySave.WPF         ? .NET 8.0, WPF, Windows
EasySave.UI          ? .NET 8.0, Console, Cross-platform
EasySave v1.0        ? .NET 8.0, Class Library
EasyLog              ? .NET 8.0, Class Library
CryptoSoft           ? .NET 8.0, Class Library
EasySave.Tests       ? xUnit, .NET 8.0
```

### Dependencies
```
EasySave.WPF     ? References EasySave v1.0, EasyLog, CryptoSoft
EasySave.UI      ? References EasySave v1.0, EasyLog
EasySave v1.0    ? References EasyLog
```

---

## Contribution

### Branch Structure
```
main             ? Stable production
develop          ? Feature integration
feature/*        ? New features
bugfix/*         ? Bug fixes
```

### Pull Request
1. Fork project
2. Create feature branch
3. Commit changes
4. Test (dotnet test)
5. Submit PR to develop

---

## Support

- **GitHub Issues**: [github.com/joanperreyprato/EasySave/issues](https://github.com/joanperreyprato/EasySave/issues)
- **User documentation**: [DOCUMENTATION.md](DOCUMENTATION.md)
- **FAQ**: [FAQ.md](FAQ.md)

---

**Version**: 2.0  
**Last updated**: January 2025

? Complete technical documentation for developers
