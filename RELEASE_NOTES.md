# EasySave — Release Notes

## v3.0.1 — Compliance update

### Fixed
- **Priority files:** each job now copies its priority files first, and every job registers its priority files before any job starts. Non-priority files of every job wait until no priority file is left. A stopped or failed job releases its remaining priority files, so other jobs can no longer be blocked forever.
- **Business software:** jobs launched while the business software is already running now start paused (they used to run). Every automatic pause and resume is written to the daily log. A job paused by the user is no longer resumed when the business software closes.
- **Stop** is now immediate: the file being copied is cancelled and the partial copy is deleted.
- **state.json** shows `Paused` only during a real pause, and statuses are written as text (`Active`, `Paused`, `Stopped`, `Completed`...).
- **CryptoSoft:** EasySave calls CryptoSoft one file at a time, retries when it is busy and no longer leaves `.tmp` files. CryptoSoft recovers its mutex when a previous instance crashed.
- **v2.0 rule:** when the business software starts during a sequential run, the current file is finished, the stop is logged and the remaining files are skipped.
- **Command line (v3.0):** `EasySave.exe 1-3` / `EasySave.exe 1;3` now runs the v3.0 jobs (the ones of the graphical interface) in parallel with every v3.0 rule, shows the progress, stops on Ctrl+C and returns an exit code. The text menu, the log format, the log destination and the language are shared with the graphical interface. The v1.1 console remains available through the `v1.1.1` tag.
- **Job creation:** quotes around the paths are removed; relative paths are refused (a target such as `sd` used to be created in the folder EasySave was started from); a target equal to the source or inside it is refused; the console asks the backup type again until 1 or 2 is typed.

### Added
- **Centralized logs:** the Docker server now writes one daily file shared by all users (`server-logs/YYYY-MM-DD.json`). Each entry identifies its machine and user.
- **Log destination** setting: Local, Server or Local + Server, with a configurable server address.
- New log fields: `action` (file transfer, directory created, pause...), `machineName` and `userName`. Existing fields are unchanged, so EasyLog stays compatible with v1.0.
- Folder creation is logged.
- Documentation in English: one-page user manual, support guide, release notes.

### Changed
- One backup engine shared by v1.x, v2.0 and v3.0: the duplicated copy, log and state code was removed.
- Daily log files are appended in place instead of being rewritten for each entry.
- Every message of the console and of the graphical interface is translated (EN / FR).

## v3.0
Parallel backups, Play / Pause / Stop per job or for all jobs, priority files, one large file at a time, automatic pause while the business software runs, mono-instance CryptoSoft, Docker log server.

## v2.0.1 — Maintenance of the v2.0 branch
- **Fixed:** the progress bar stayed at 0 %. The bars are now refreshed on every tick and keep their final value when the run is over.
- Available on the `maintenance/v2.0` branch, tag `v2.0.1` (for customers who stay on v2.0).

## v2.0
WPF graphical interface (MVVM), unlimited jobs, CryptoSoft encryption by extension, encryption time in the log, business software detection (a job cannot start).

## v1.1.1 — Maintenance of the v1.1 branch
- **Fixed:** job path validation (same fix as v3.0.1: quotes removed, relative paths and a target inside the source refused, backup type asked again). Branch `maintenance/v1.1`, tag `v1.1.1`.

## v1.1
Choice of the daily log format: JSON or XML.

## v1.0.1 — Maintenance of the v1.0 branch
- **Fixed:** job path validation (same fix as v1.1.1). Branch `maintenance/v1.0`, tag `v1.0.1`.

## v1.0
Console application: up to 5 full or differential jobs, sequential execution, command line (`EasySave.exe 1-3`, `EasySave.exe 1;3`), JSON daily log (EasyLog.dll), real-time state file, English and French.
