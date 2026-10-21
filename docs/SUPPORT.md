# EasySave — Technical Support Guide

## Minimum configuration

| Component | Requirement |
|---|---|
| Operating system | Windows 10 (64-bit) or later, Windows Server 2019 or later |
| Runtime | .NET 8.0 Desktop Runtime (WPF) — .NET 8.0 Runtime for the console version |
| Memory / disk | 4 GB RAM, 200 MB free for the application (plus the backup volume) |
| Rights | Read access to the source folders, write access to the target folders and to `%ProgramData%\EasySave` |
| Log server (optional) | Any host running Docker, TCP port 8080 reachable from the clients |

## Default locations

| Item | Location |
|---|---|
| Application | `C:\Program Files\ProSoft\EasySave\` (recommended): `EasySave.WPF.exe`, `EasySave.exe` (console), `CryptoSoft.exe`, `EasyLog.dll` |
| CryptoSoft key | `cryptosoft.config.json`, next to `CryptoSoft.exe` (`{"key": "..."}`, at least 8 characters / 64 bits) |
| General settings | `%ProgramData%\EasySave\appsettings.json` |
| Jobs (console v1.x) | `%ProgramData%\EasySave\backup_config.json` |
| Jobs (graphical v3.0) | `%ProgramData%\EasySave\backup_config_v3.json` |
| Real-time state | `%ProgramData%\EasySave\state.json` |
| Interface language | `%ProgramData%\EasySave\language.json` |
| Daily logs | `%ProgramData%\EasySave\Logs\YYYY-MM-DD.json` or `.xml` |
| Central logs (server) | Volume `server-logs/YYYY-MM-DD.json` next to `docker-compose.yml` (`/logs` in the container) |

`%ProgramData%` is usually `C:\ProgramData`. All files are indented JSON (or XML) and can be read with Notepad.

## Settings file (`appsettings.json`)

| Key | Values | Default |
|---|---|---|
| `BusinessSoftwareName` | Process name, with or without `.exe` | empty (disabled) |
| `EncryptionExtensions` | e.g. `[".docx", ".xlsx"]` | empty |
| `PriorityExtensions` | e.g. `[".pdf"]` | empty |
| `LargeFileSizeKb` | Size in KB (0 = no limit) | 1024 |
| `LogFormat` | `Json` or `Xml` | `Json` |
| `LogMode` | `Local`, `Centralized` or `Both` | `Local` |
| `LogServerAddress` / `LogServerPort` | Log server address | `127.0.0.1` / `8080` |
| `Language` | `en` or `fr` | `en` |

## Reading the daily log

Each entry contains: `timestamp`, `backupName`, `action`, `machineName`, `userName`, `sourceFilePath` and `targetFilePath` (UNC format), `fileSize` (bytes), `transferDurationMs`, `encryptionDurationMs`.

| `action` | Meaning |
|---|---|
| `FileTransfer` | A file was copied. `transferDurationMs < 0` = copy error |
| `DirectoryCreated` | A folder was created in the target |
| `BusinessSoftwarePaused` / `BusinessSoftwareResumed` | Automatic pause / resume (v3.0) |
| `BusinessSoftwareBlocked` / `BusinessSoftwareInterrupted` | Job refused / stopped after the current file (v2.0) |
| `JobStopped` | Job stopped by the user |

| `encryptionDurationMs` | Meaning |
|---|---|
| `0` | File not encrypted |
| `> 0` | Encryption time in ms |
| `-1` to `-4` | CryptoSoft error: usage, source not found, processing error, CryptoSoft busy (30 s) |
| `-10` / `-11` / `-12` | `CryptoSoft.exe` not found next to EasySave / could not start / killed after 5 min |

## Common issues

| Symptom | Check |
|---|---|
| "EasySave is already running" | Another EasySave (WPF or console) is open on this computer: only one instance is allowed |
| Jobs stay paused | The business software set in the settings is running (status bar message) |
| Non-priority files do not start | Another job still has priority files to copy: this is the priority rule |
| No logs on the server | `LogMode` is `Centralized` or `Both`, the server address is correct, port 8080 is open, `docker logs easysave-logserver` shows the connection |
| `encryptionDurationMs = -10` | Copy `CryptoSoft.exe` and `cryptosoft.config.json` into the EasySave folder |
| Console command `1;3` does nothing in PowerShell | Quote the argument: `EasySave.exe "1;3"` |
