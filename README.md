# 🛡️ EasySave — ProSoft Backup Software

> Professional backup application written in C# / .NET 8.0 for ProSoft.
> EasySave 3.0 = a **WPF graphical application** and **`EasySave.exe`, its command-line interface** (same jobs, same settings).
> The console versions v1.0 / v1.1 remain available through their Git tags.

| Document | Audience |
|---|---|
| [User manual (one page)](docs/USER_MANUAL.md) | End users |
| [Technical support guide](docs/SUPPORT.md) | Customer support |
| [Release notes](RELEASE_NOTES.md) | Everyone |
| [UML diagrams](docs/UML_DIAGRAMS.md) | Developers |
| [v4.0 study](docs/DIRECTION_V4_STUDY.md) | Management |

---

## 📋 Table of contents

- [Overview](#-overview)
- [Version history](#-version-history)
- [Project architecture](#-project-architecture)
- [Prerequisites](#-prerequisites)
- [Build & run](#-build--run)
- [CryptoSoft](#-cryptosoft)
- [Centralized log server (Docker)](#-centralized-log-server-docker)
- [Tests](#-tests)
- [Technologies](#-technologies)

---

## 🎯 Overview

**EasySave** lets you:

- ✅ Create backup jobs (full or differential) on local, external or network drives
- ✅ Run jobs in parallel with Play / Pause / Stop per job or for all jobs
- ✅ Encrypt sensitive files with **CryptoSoft** (XOR, key ≥ 64 bits)
- ✅ Pause automatically while a business software is running
- ✅ Write daily logs (JSON or XML) locally, on a central Docker server, or both
- ✅ Follow every job in real time (`state.json` and progress bars)
- ✅ Use the interface in English or French (switch live), with dark / light themes

---

## 📈 Version history

| Feature | v1.0 | v1.1 | v2.0 | v3.0 |
|---|:---:|:---:|:---:|:---:|
| Interface | Console | Console | WPF | WPF |
| Languages | EN/FR | EN/FR | EN/FR | EN/FR |
| Max jobs | 5 | 5 | Unlimited | Unlimited |
| Daily log format | JSON | JSON/XML | JSON/XML + encryption time | JSON/XML + encryption time |
| CryptoSoft | ❌ | ❌ | ✅ | ✅ (mono-instance) |
| Business software | ❌ | ❌ | ✅ (job cannot start) | ✅ (automatic pause / resume) |
| Execution | Mono / sequential | Mono / sequential | Mono / sequential | **Parallel** |
| Play / Pause / Stop | ❌ | ❌ | ❌ | ✅ |
| Priority files | ❌ | ❌ | ❌ | ✅ |
| One large file at a time | ❌ | ❌ | ❌ | ✅ |
| Centralized logs (Docker) | ❌ | ❌ | ❌ | ✅ |
| Command line (`EasySave.exe 1-3`, `EasySave.exe 1;3`) | ✅ | ✅ | same as 1.0 | same as 1.0 (runs the v3.0 jobs with the v3.0 rules) |

Each version is tagged in Git:

```bash
git checkout v1.0    # Console, JSON logs
git checkout v1.1    # Console, JSON or XML logs
git checkout v2.0    # WPF graphical interface
git checkout main    # v3.0 (current version)
```

---

## 🏗️ Project architecture

```
EasySave_Joan/
├── EasySave.CLI/               # Command-line interface of v3.0 → EasySave.exe (same jobs as the WPF app)
├── EasySave.WPF/               # Graphical application v3.0 (MVVM: Views, ViewModels, Styles)
├── EasySave.Core/              # Business library shared by both applications
│   ├── Models/                 # BackupJob, JobState, JobStatus...
│   └── Services/
│       ├── BackupServiceBase.cs        # Job management shared by every version
│       ├── BackupService.cs            # v1.x rules (max 5 jobs)
│       ├── BackupServiceV2.cs          # v2.0 rules (encryption, business software blocks)
│       ├── BackupServiceV3.cs          # v3.0 rules (parallel, Play/Pause/Stop, auto-pause)
│       ├── BackupEngine.cs             # Copies, encrypts, logs and updates state.json
│       ├── JobCatalog.cs               # Job persistence and validation
│       ├── JobController.cs            # Play / Pause / Stop of one running job
│       ├── PriorityTransferGuard.cs    # Priority files rule (shared by all jobs)
│       ├── LargeFileTransferGuard.cs   # One file > n KB at a time
│       ├── CryptoSoftRunner.cs         # Calls CryptoSoft (serialized: mono-instance)
│       └── AppSettings.cs / AppPaths.cs / StateManager.cs / LanguageProvider.cs
├── EasyLog/                    # EasyLog.dll — reusable daily logging library
│   ├── FileLogger.cs           # Daily file, efficient append (JsonLogger, XmlLogger)
│   ├── SocketLogger.cs         # Sends entries to the log server over TCP
│   ├── CompositeLogger.cs      # Local + server
│   └── LoggerFactory.cs        # Builds the logger for a log mode
├── CryptoSoft/                 # XOR encryption tool (mono-instance)
├── EasySave.LogServer/         # Centralized log server (Docker)
├── EasySave.Tests*/            # Unit, WPF component and integration tests
└── docker-compose.yml
```

---

## ⚙️ Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 (WPF)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional, for the log server)

---

## 🚀 Build & run

```bash
git clone https://github.com/joanperreyprato/EasySave_Joan.git
cd EasySave_Joan
dotnet build EasySave.sln

dotnet run --project EasySave.WPF      # Graphical application (v3.0)
dotnet run --project EasySave.CLI      # Command-line interface (interactive menu)
```

Command line, as in version 1.0 (after build, in `EasySave.CLI/bin/Debug/net8.0/`). It runs the jobs created in the graphical
interface, in parallel, with every v3.0 rule (encryption, priority files, large files, business software, log destination):

```bash
EasySave.exe 1-3      # runs jobs 1 to 3
EasySave.exe "1;3"    # runs jobs 1 and 3 (quotes needed in PowerShell)
```

The progress is displayed until the jobs end; `Ctrl+C` stops them. Exit code: `0` if every job completed, `1` otherwise.

In VS Code, press `F5` and pick a configuration (WPF, CLI or CryptoSoft).

> ⚠️ EasySave is mono-instance: only one EasySave (WPF or CLI) can run at a time on a computer.

How to use the application: see the [user manual](docs/USER_MANUAL.md).

---

## 🔐 CryptoSoft

Stand-alone console tool that encrypts / decrypts a file with a bitwise **XOR** (the same command decrypts).

```bash
CryptoSoft.exe <source_file> <destination_file>
```

- Exit code: `> 0` = processing time in ms, `< 0` = error (`-1` usage, `-2` source not found, `-3` processing error, `-4` busy).
- The key (at least 64 bits) is read from `cryptosoft.config.json`, next to `CryptoSoft.exe`.
- **Mono-instance:** a system mutex (`Global\CryptoSoftMutex_EasySave`) prevents two instances from running at the same time; a second instance waits up to 30 s. EasySave launches it one file at a time and retries if it is busy.
- In EasySave, files whose extension is in the encryption list are copied, **then** encrypted in the target folder. The encryption time is written in the log (`0` = not encrypted, `< 0` = error).

---

## 🐳 Centralized log server (Docker)

The server receives log entries from every EasySave client over TCP and writes them to **one daily file shared by all users** (`server-logs/YYYY-MM-DD.json`). Each entry contains the `machineName` and `userName` of its sender.

```bash
docker compose up -d --build        # start (port 8080)
docker logs -f easysave-logserver   # live view
docker compose down                 # stop
```

In EasySave (Settings panel), choose the log destination:

| Mode | Logs written to |
|---|---|
| **Local** (default) | The user's computer only (`%ProgramData%\EasySave\Logs`) |
| **Server** | The Docker server only |
| **Local + Server** | Both |

and set the server address (`host:port`, default `127.0.0.1:8080`).

---

## 🧪 Tests

```bash
dotnet test EasySave.sln
```

| Project | Scope |
|---|---|
| `EasySave.Tests` | Unit tests: services, engine, priority rule, Play/Pause/Stop, loggers, CryptoSoft |
| `EasySave.Tests.WPF` | ViewModel tests without a window |
| `EasySave.Tests.Integration` | End-to-end backups with real files, logs and CryptoSoft |

Tests run on every push through GitHub Actions (`.github/workflows/tests.yml`).

---

## 🛠️ Technologies

C# / .NET 8.0 · WPF (MVVM) · xUnit + Moq · Docker · GitHub Actions · TCP sockets · XOR cipher

---

## 👥 Author

**Joan Perrey Prato** — PGE A3 FISA Computer Science — CESI (Software Engineering, 2025-2026)
