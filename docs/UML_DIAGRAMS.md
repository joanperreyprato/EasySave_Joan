# 📐 UML Models — EasySave

This document gathers the UML diagrams required for each deliverable (v1.0 to v3.0).
Diagrams are written in **Mermaid** (rendered directly by GitHub and VS Code).

---

## 1. Use case diagram

```mermaid
flowchart LR
    User(["👤 User"])
    OS(["💻 Operating system"])

    subgraph EasySave ["EasySave (v1.x → v3.0)"]
        UC_Create["Create / delete a backup job (v1: 5 max, v2+: unlimited)"]
        UC_Run["Run backups (v1/v2: sequential, v3: parallel)"]
        UC_Cli["Run jobs from the command line (EasySave.exe 1-3)"]
        UC_Control["Play / Pause / Stop a job or all jobs (v3)"]
        UC_Settings["Set general settings (log format, log destination, extensions, n KB...)"]
        UC_Lang["Switch language (EN / FR)"]
        UC_Crypto["Encrypt files with CryptoSoft (v2+)"]
        UC_Log["Write daily log and real-time state"]
        UC_Central["Send logs to the central Docker server (v3)"]
        UC_Detect["Detect business software (v2: block, v3: pause / resume)"]
    end

    User --> UC_Create
    User --> UC_Run
    User --> UC_Cli
    User --> UC_Control
    User --> UC_Settings
    User --> UC_Lang
    UC_Run -.->|include| UC_Log
    UC_Run -.->|extend: encrypted extension| UC_Crypto
    UC_Log -.->|extend: Server or Local + Server mode| UC_Central
    OS --> UC_Detect
    UC_Detect -.->|pauses| UC_Run
```

---

## 2. Class diagram

Design patterns:
- **MVVM** for the WPF application
- **Template / inheritance**: `BackupServiceBase` holds the behavior shared by the three versions; each version only adds its own rules
- **Strategy-like options**: one `BackupEngine` for every version, features enabled by `BackupRunOptions`
- **Factory** (`LoggerFactory`) and **Composite** (`CompositeLogger`) for logging
- **Template method** (`FileLogger` → `JsonLogger`, `XmlLogger`)
- **Singleton** (`LanguageProvider`, application-wide `PriorityTransferGuard` and `LargeFileTransferGuard`)

```mermaid
classDiagram
    %% ── Models ──
    class BackupJob {
        +string Name
        +string SourceDirectory
        +string TargetDirectory
        +BackupType Type
    }
    class BackupType {
        <<enumeration>>
        Full
        Differential
    }
    class JobState {
        +string Name
        +DateTime LastActionTime
        +JobStatus Status
        +int TotalFiles
        +long TotalSize
        +int RemainingFiles
        +long RemainingSize
        +double Progress
        +string CurrentSourceFile
        +string CurrentTargetFile
    }
    class JobStatus {
        <<enumeration>>
        Inactive
        Active
        Paused
        Completed
        Error
        Stopped
    }
    BackupJob --> BackupType
    JobState --> JobStatus

    %% ── Services ──
    class IBackupService {
        <<interface>>
        +CreateJob(name, source, target, type) BackupCreationResult
        +DeleteJob(name)
        +GetAllJobs() List~BackupJob~
        +ExecuteJob(name, ILogger)
        +ExecuteJobs(names, ILogger)
    }
    class BackupServiceBase {
        <<abstract>>
        #StateManager StateManager
        #BackupEngine Engine
        +CreateJob(...) BackupCreationResult
        +DeleteJob(name)
        +GetAllJobs() List~BackupJob~
        +GetJob(name) BackupJob
        #RunSequentially(job, logger, options) BackupOutcome
    }
    class BackupService {
        +MaxJobs = 5
        +ExecuteJob(name, ILogger)
        +ExecuteJobs(names, ILogger)
    }
    class BackupServiceV2 {
        +ExecuteJob(name, ILogger)
        +ExecuteJobs(names, ILogger)
    }
    class BackupServiceV3 {
        -Dictionary~string, RunningJob~ _running
        +ExecuteJobsParallelAsync(names, ILogger) Task
        +PauseJob(name) / ResumeJob(name) / StopJob(name)
        +PauseAll() / ResumeAll() / StopAll()
        +HasRunningJobs bool
        -PollBusinessSoftware()
    }
    class JobCatalog {
        +Add(...) BackupCreationResult
        +Remove(name) bool
        +GetAll() List~BackupJob~
        +Find(name) BackupJob
    }
    class BackupEngine {
        +Prepare(job, priorityExtensions) BackupPlan
        +RunAsync(plan, ILogger, BackupRunOptions) Task~BackupOutcome~
        +LogEvent(logger, jobName, action)$
    }
    class BackupRunOptions {
        +EncryptionExtensions
        +PriorityExtensions
        +LargeFileThresholdKb
        +JobController Controller
        +Func~bool~ InterruptRequested
    }
    class BackupPlan {
        +BackupJob Job
        +List~PlannedFile~ Files
        +int PriorityFileCount
    }
    class JobController {
        +Pause() / Resume()
        +SetBusinessSoftwarePause(bool)
        +Stop()
        +WaitWhilePaused()
        +bool IsPaused
        +CancellationToken Token
    }
    class PriorityTransferGuard {
        <<shared>>
        +Register(count)
        +Release(count)
        +WaitUntilNoPriorityPendingAsync(ct)
    }
    class LargeFileTransferGuard {
        <<shared>>
        +AcquireAsync(size, thresholdKb, ct) IDisposable
    }
    class CryptoSoftRunner {
        <<static>>
        +EncryptAsync(file) Task~long~
    }
    class BusinessSoftwareMonitor {
        +string ProcessName
        +IsRunning() bool
    }
    class StateManager {
        +UpdateJobState(JobState)
        +GetAllStates() List~JobState~
    }
    class AppSettings {
        +LogFormat / LogMode
        +LogServerAddress / LogServerPort
        +EncryptionExtensions / PriorityExtensions
        +LargeFileSizeKb
        +BusinessSoftwareName
        +CreateLogger() ILogger
    }

    IBackupService <|.. BackupService
    IBackupService <|.. BackupServiceV2
    BackupServiceBase <|-- BackupService
    BackupServiceBase <|-- BackupServiceV2
    BackupServiceBase <|-- BackupServiceV3
    BackupServiceBase *-- JobCatalog
    BackupServiceBase *-- BackupEngine
    BackupServiceV2 --> BusinessSoftwareMonitor
    BackupServiceV3 --> BusinessSoftwareMonitor
    BackupServiceV3 o-- JobController
    BackupEngine ..> BackupRunOptions
    BackupEngine ..> BackupPlan
    BackupEngine --> PriorityTransferGuard
    BackupEngine --> LargeFileTransferGuard
    BackupEngine ..> CryptoSoftRunner
    BackupEngine --> StateManager
    BackupRunOptions --> JobController

    %% ── EasyLog.dll ──
    class ILogger {
        <<interface>>
        +Log(LogEntry entry)
    }
    class LogEntry {
        +DateTime Timestamp
        +string BackupName
        +string Action
        +string MachineName
        +string UserName
        +string SourceFilePath
        +string TargetFilePath
        +long FileSize
        +long TransferDurationMs
        +long EncryptionDurationMs
    }
    class FileLogger {
        <<abstract>>
        +Log(LogEntry entry)
        #Serialize(LogEntry)* string
    }
    class JsonLogger
    class XmlLogger
    class SocketLogger {
        +Log(LogEntry entry)
    }
    class CompositeLogger {
        +Log(LogEntry entry)
    }
    class LoggerFactory {
        <<static>>
        +Create(LogFormat, dir) ILogger
        +Create(LogMode, LogFormat, dir, host, port) ILogger
    }
    class LogMode {
        <<enumeration>>
        Local
        Centralized
        Both
    }
    ILogger <|.. FileLogger
    FileLogger <|-- JsonLogger
    FileLogger <|-- XmlLogger
    ILogger <|.. SocketLogger
    ILogger <|.. CompositeLogger
    CompositeLogger o-- ILogger
    LoggerFactory ..> ILogger : creates
    LoggerFactory ..> LogMode
    ILogger ..> LogEntry
    AppSettings ..> LoggerFactory

    %% ── MVVM ──
    class MainViewModel {
        +ObservableCollection~JobViewModel~ Jobs
        +ICommand RunSelectedJobsCommand / RunAllJobsCommand
        +ICommand PlayJobCommand / PauseJobCommand / StopJobCommand
        +ICommand PlayAllCommand / PauseAllCommand / StopAllCommand
        +string SelectedLogFormat
        +string SelectedLogMode
        +string this[key]
    }
    class JobViewModel {
        +string Name
        +JobStatus Status
        +double Progress
        +string StatusText
    }
    MainViewModel o-- JobViewModel
    MainViewModel --> BackupServiceV3
    MainViewModel --> AppSettings
```

---

## 3. Sequence diagram — one v3.0 job

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant VM as MainViewModel
    participant Svc as BackupServiceV3
    participant Eng as BackupEngine
    participant PG as PriorityTransferGuard
    participant LG as LargeFileTransferGuard
    participant Ctl as JobController
    participant Crypto as CryptoSoft.exe
    participant Log as ILogger (local / server / both)

    User->>VM: Run selected
    VM->>VM: logger = settings.CreateLogger()
    VM->>Svc: ExecuteJobsParallelAsync(jobs, logger)
    Svc->>Ctl: new JobController (paused if business software is running)
    Svc->>Eng: Prepare(job) for every job
    Eng->>PG: Register(priority file count)
    Note over Svc,Eng: All jobs are registered before any job starts

    par Each job in parallel
        loop Each file (priority files first)
            Eng->>Ctl: WaitWhilePaused() (state "Paused" only if really paused)
            alt Non-priority file
                Eng->>PG: WaitUntilNoPriorityPendingAsync()
            end
            Eng->>LG: AcquireAsync(size, n KB) (one large file at a time)
            Eng->>Eng: Cancellable copy (Stop = immediate)
            opt Extension to encrypt
                Eng->>Crypto: CryptoSoft.exe target target.tmp (one at a time)
                Crypto-->>Eng: exit code (> 0 = ms, < 0 = error)
            end
            Eng->>Log: Log(FileTransfer entry)
            Eng->>PG: Release() if priority file
            Eng->>Eng: Update state.json
        end
        Eng->>PG: Release(remaining) (completed, stopped or failed)
    end

    loop Every 500 ms
        Svc->>Svc: business software running?
        Svc->>Ctl: SetBusinessSoftwarePause(true / false)
        Svc->>Log: BusinessSoftwarePaused / Resumed (on change)
    end

    Svc-->>VM: all jobs finished
    VM-->>User: "All backups completed"
```

---

## 4. Deployment diagram (v3.0)

```mermaid
flowchart TB
    subgraph Client ["Each user's computer (Windows)"]
        subgraph App ["EasySave process (mono-instance mutex)"]
            WPF["EasySave.WPF.exe (GUI) / EasySave.exe (command line)"]
            Core["EasySave.Core.dll"]
            EasyLog["EasyLog.dll"]
        end
        Crypto["CryptoSoft.exe (mono-instance mutex)"]
        Data[("%ProgramData%\\EasySave\nappsettings.json, state.json,\nbackup_config*.json, Logs\\")]

        WPF --> Core
        Core --> EasyLog
        Core -->|starts process| Crypto
        EasyLog -->|Local / Both| Data
        Core --> Data
    end

    subgraph Docker ["Docker host"]
        subgraph Container ["Container: easysave-logserver"]
            Server["EasySave.LogServer (TCP 8080)\nuses EasyLog.dll"]
        end
        Volume[("Volume server-logs/\nYYYY-MM-DD.json\none file for all users")]
        Server --> Volume
    end

    EasyLog -->|Centralized / Both: JSON lines over TCP| Server
```
