# EasySave 3.0 — User Manual

**1. Create a backup job** — Click **＋ New Job**, enter a name, a source folder and a target folder (local, external or network drive, e.g. `\\server\share`), choose **Full** (all files) or **Differential** (only new or modified files), then click **✓ Create Job**.

**2. Run backups** — Tick the jobs and click **▶ Run Selected**, or click **▶▶ Run All**. Jobs run **in parallel**; the grid shows each job's status, progress (%) and remaining files.

**3. Control running jobs**

| Button | Selected job | All jobs |
|---|---|---|
| ▶ Play | Starts the job, or resumes it after a pause | Resumes every paused job |
| ⏸ Pause | Pauses after the file being copied | Pauses every job |
| ⏹ Stop | Stops immediately (the file being copied is cancelled) | Stops every job |

**4. Business software** — While the business software set in the settings is running (e.g. `CalculatorApp` for the Windows calculator), all backups pause automatically and resume as soon as it is closed.

**5. Settings** (bottom panel, then **Save**)

| Setting | Meaning |
|---|---|
| Business software | Process name that pauses backups |
| Encrypt extensions | Files encrypted by CryptoSoft after the copy (e.g. `.docx, .xlsx`) |
| Priority extensions | Files copied before any other file of any job (e.g. `.pdf`) |
| Large file (KB) | Two files above this size are never copied at the same time |
| Log format | JSON or XML |
| Log destination | Local, Server, or Local + Server; server address as `host:port` |

**6. Language and theme** — The **EN / FR** button switches the language instantly; the **🌙 / ☀** button switches the theme.

**7. Command line** — `EasySave.exe 1-3` / `EasySave.exe "1;3"` runs jobs 1 to 3 / jobs 1 and 3 of the list above, with the same rules as the interface (Ctrl+C stops them). `EasySave.exe` alone opens a text menu.

**Logs and state** — Daily logs: `%ProgramData%\EasySave\Logs\YYYY-MM-DD.json` (or `.xml`). Real-time state: `%ProgramData%\EasySave\state.json`. Both open in Notepad.
