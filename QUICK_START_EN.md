# EasySave - Quick Start Guide (English)

Get started with EasySave in 5 minutes! 

---

## ? Quick Installation

### Windows
```bash
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
dotnet build
dotnet run
```

### Linux / macOS
```bash
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
dotnet build
dotnet run
```

---

##  First Steps

### 1. Launch in Interactive Mode
```bash
dotnet run
```

Menu:
```
=== EasySave ===
1. Create Backup
2. Execute Backup
3. List Backups
4. Exit
Select an option:
```

### 2. Create Your First Backup
- Press **1**
- Enter name: `MyBackup`
- Enter source folder: `/home/user/Documents`
- Enter target folder: `/home/user/Backups`
- Choose type: `full` (or `diff` for differential)

### 3. Execute the Backup
- Press **2**
- Select index: `0`
- It's running! ?

### 4. List Your Backups
- Press **3**
- See your configured tasks

---

## ? Command Line Mode

### Create Quickly
```bash
dotnet run -- create "DocBackup" "C:\Users\Doc" "D:\Backup" full
```

### Execute Quickly
```bash
# Single backup
dotnet run -- execute 0

# Multiple backups
dotnet run -- execute 0-2
dotnet run -- execute 0,2,4
```

### List
```bash
dotnet run -- list
```

---

##  Backup Types

| Type | First Time | Usage |
|------|------------|-------|
| **Full** | ? Do it | Complete backup (all files) |
| **Diff** | ? Too long | Update (modified files only) |

**Tip**: Do a `full` initially, then use `diff` for following ones.

---

##  Configuration

Files auto-created in:
- **Windows**: `C:\ProgramData\EasySave\`
- **Linux/macOS**: `~/.local/share/EasySave/`

| File | Content |
|------|---------|
| `backup_config.json` | Your tasks |
| `state.json` | Progress info |
| `logs/` | History |

---

##  Common Errors

| Problem | Solution |
|---------|----------|
| "Source directory does not exist" | Check source path |
| "Target directory does not exist" | Create target folder |
| "Invalid job name" | Use alphanumeric + hyphen/underscore |
| "A backup job with this name already exists" | Change the name |
| "Maximum number of backup jobs (5) reached" | Delete an old backup |

---

##  Change Language

Edit `Program.cs` line 2:

```csharp
// English
var language = new LanguageProvider("en");

// French
var language = new LanguageProvider("fr");
```

Recompile:
```bash
dotnet build
dotnet run
```

---

##  Practical Examples

### Example 1: Backup a drive
```bash
dotnet run -- create "DriveD" "D:\" "E:\BackupD" full
dotnet run -- execute 0
```

### Example 2: Regular backups
```bash
# Day 1: Full backup
dotnet run -- create "Weekly-Full" "/data" "/backup/weekly" full
dotnet run -- execute 0

# Day 8: Quick differential
dotnet run -- create "Weekly-Update" "/data" "/backup/weekly" diff
dotnet run -- execute 1
```

### Example 3: Batch execution
```bash
# Create multiple backups
dotnet run -- create "Photos" "/home/photos" "/backup/photos" full
dotnet run -- create "Videos" "/home/videos" "/backup/videos" full
dotnet run -- create "Documents" "/home/docs" "/backup/docs" full

# Execute them all
dotnet run -- execute 0-2
```

---

##  Track Progress

During backup, app updates:
- Files processed
- Size transferred
- Percentage completed
- Elapsed time

Check in `state.json`:
```json
{
  "progress": 45.5,
  "remainingFiles": 55,
  "remainingSize": 524288000
}
```

---

##  Quick Troubleshooting

### Reinstall
```bash
dotnet clean
dotnet build
```

### Reset
Delete `%ProgramData%\EasySave\` or `~/.local/share/EasySave/`

### Check Logs
Browse `logs/` folder for history

---

## ? First Backup Checklist

- [ ] Compile: `dotnet build`
- [ ] Launch: `dotnet run`
- [ ] Create backup (menu 1)
- [ ] Specify source AND target
- [ ] Type = `full`
- [ ] Execute (menu 2)
- [ ] Verify files copied ?
- [ ] List (menu 3) for summary

---

##  Need Help?

- **Bug**: [Report on GitHub](https://github.com/joanperreyprato/EasySave/issues)
- **Question**: See `DOCUMENTATION.md`
- **Technical**: See `TECHNICAL_DOCUMENTATION.md`

---

**Version**: 1.0 | **Updated**: January 2024

 You're ready! Start your first backup now!
