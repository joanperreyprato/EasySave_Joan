# EasySave - FAQ (Frequently Asked Questions)

---

## Installation & Configuration

### Q1: What are the prerequisites to install EasySave

**A:** You need **.NET 8.0 or higher** installed on your machine.

```bash
dotnet --version
```

Download .NET 8 from: https://dotnet.microsoft.com/download

---

### Q2: Where are configuration files stored

**A:** Files are automatically created in:

- **Windows**: `C:\ProgramData\EasySave\`
- **Linux**: `/opt/EasySave/` or `~/.local/share/EasySave/`
- **macOS**: `~/.local/share/EasySave/`

Created files:
- `backup_config.json` - Your configured tasks
- `state.json` - Current backup state
- `logs/` - Operation history

---

### Q3: How do I change the language

**A:** Edit `Program.cs` line 2:

```csharp
var language = new LanguageProvider("en");  // English
var language = new LanguageProvider("fr");  // French
```

Then recompile:
```bash
dotnet build
dotnet run
```

---

## Backup Creation

### Q4: What name format can I use

**A:** The name can only contain:
-  Letters (a-z, A-Z)
-  Numbers (0-9)
-  Spaces
-  Hyphens (-)
-  Underscores (_)
-  Special characters (@, #, $, %, etc.)
-  Maximum 50 characters

**Valid examples**:
```
 MyBackup
 Backup-2024
 Photos_Family
 My Important Data
```

**Invalid examples**:
```
 My@Backup (contains @)
 My/Backup (contains /)
 MyBackupVeryLongNameThatExceedsFiftyCharactersLimit (too long)
```

---

### Q5: How many backups can I create

**A:** **Maximum 5 backups** can be configured.

If you reach this limit, you must delete one before creating a new one.

---

### Q6: Must source and target paths exist

**A:** **YES, both must exist** before creating a backup.

```bash
#  Error: Folder doesn't exist
dotnet run -- create "Backup" "/nonexistent/path" "/backup" full

#  OK: Both folders exist
dotnet run -- create "Backup" "/existing/path" "/backup/existing" full
```

**Solution**: Create folders first:
```bash
# Windows
mkdir C:\Backups

# Linux/macOS
mkdir ~/Backups
```

---

### Q7: Can I backup to a USB drive or network

**A:** **YES**, as long as the path is accessible as a local directory.

```bash
# USB drive (example Windows)
dotnet run -- create "USB-Backup" "C:\Users\Data" "E:\Backup" full

# Network share (example Linux)
dotnet run -- create "Network-Backup" "/home/data" "/mnt/network/backup" full
```

---

## Backup Types

### Q8: What's the difference between "Full" and "Diff"

**A:**

| Aspect | Full | Diff |
|--------|------|------|
| **Files copied** | ALL | Only new/modified |
| **Time** |  Longer |  Very fast |
| **Disk space** |  Much |  Less |
| **When to use** | 1st time | Regular updates |
| **Examples** | Initial backup | Weekly backup |

---

### Q9: How are modified files detected in Diff mode

**A:** EasySave compares:

1. **Modification date** - If source file is newer
2. **File size** - If sizes differ

A file is copied if either condition is true.

---

### Q10: Can I run multiple backups at once

**A:** **YES**, from the command line:

```bash
# Run indices 0, 1, 2
dotnet run -- execute 0-2

# Run specific indices
dotnet run -- execute 0,2,4

# Run all one by one
dotnet run -- execute 0-4
```

**Note**: They run **sequentially** (one after another), not in parallel.

---

## Execution & Monitoring

### Q11: How do I track backup progress

**A:** State is updated in real-time in `state.json`:

```json
{
  "name": "MyBackup",
  "progress": 35.5,
  "status": "Active",
  "totalFiles": 1000,
  "remainingFiles": 650,
  "currentSourceFile": "/home/user/file123.txt"
}
```

**View state**:
```bash
# Live monitoring (Linux/macOS)
watch -n 1 'cat ~/.local/share/EasySave/state.json | jq .'

# Windows PowerShell
Get-Content $env:PROGRAMDATA\EasySave\state.json -Wait
```

---

### Q12: What happens if a backup fails

**A:** State changes to `Error` and:
- Already copied files remain in place
- Not-yet-processed files are not copied
- You can restart the backup to continue

```bash
dotnet run -- execute 0  # Restart
```

---

### Q13: How do I cancel a running backup

**A:** Press `Ctrl+C` (standard interrupt).

**Warning**: Already copied files will NOT be rolled back.

---

## Command Line Mode

### Q14: How do I create a backup via CLI

**A:**

```bash
dotnet run -- create <name> <source> <target> <type>
```

**Parameters**:
- `<name>` : Unique backup name
- `<source>` : Folder to backup
- `<target>` : Destination folder
- `<type>` : `full` or `diff` (default: full)

**Examples**:
```bash
# Full backup
dotnet run -- create "Documents" "C:\Users\Docs" "D:\Backup" full

# Differential backup
dotnet run -- create "Photos" "/home/photos" "/backup/photos" diff

# Without type (full by default)
dotnet run -- create "Videos" "/home/videos" "/backup/videos"
```

---

### Q15: How do I execute multiple specific backups

**A:** Use ranges and lists:

```bash
# Range: indices 0, 1, 2
dotnet run -- execute 0-2

# Specific indices
dotnet run -- execute 0,2,4

# Combination
dotnet run -- execute 0,2-4,7
```

**Result**: `[0, 2, 3, 4, 7]`

---

## Troubleshooting

### Q16: Error "Invalid job name"

**Cause**: Name contains invalid characters or is empty.

**Solution**:
```bash
#  Wrong
dotnet run -- create "My@Backup" ...

#  Correct
dotnet run -- create "My_Backup" ...
```

See Q4 for valid characters.

---

### Q17: Error "Source directory does not exist"

**Cause**: Source folder doesn't exist.

**Solution**:
```bash
# Check path exists
ls /home/user/Documents  # Linux/macOS
dir C:\Users\Documents   # Windows

# Create if missing
mkdir -p /home/user/Documents  # Linux/macOS
mkdir C:\Users\Documents        # Windows
```

---

### Q18: Error "Target directory does not exist"

**Cause**: Destination folder doesn't exist.

**Solution**:
```bash
# Create destination folder
mkdir -p /backup/target  # Linux/macOS
mkdir C:\Backup\Target   # Windows

# Then create backup
dotnet run -- create "Backup" "/source" "/backup/target" full
```

---

### Q19: Error "A backup job with this name already exists"

**Cause**: A backup with this name already exists.

**Solution**:
```bash
# Option 1: Use different name
dotnet run -- create "MyBackup2" ...

# Option 2: Delete old one (manually)
# Edit backup_config.json and remove entry
```

---

### Q20: Error "Maximum number of backup jobs (5) reached"

**Cause**: You already have 5 backups configured.

**Solution**:
```bash
# Delete unused backup
# Edit backup_config.json and remove entry
```

---

### Q21: No files were copied

**Possible causes**:
- Diff mode active with no new/modified files
- Insufficient permissions
- Empty source folder

**Solution**:
```bash
# Check source contents
ls -la /source

# Use Full to copy all files
dotnet run -- create "Test" "/source" "/target" full
```

---

## Performance & Optimization

### Q22: How do I speed up a backup

**A:**

1. **Use Diff mode** instead of Full
   ```bash
   dotnet run -- create "Backup" "/source" "/target" diff
   ```

2. **Backup fewer files** (exclude large folders)

3. **Use SSD** as destination

4. **Check disk space** on target

---

### Q23: How much disk space do I need

**A:** Depends on your source:

- **Required space** = Total source size
- Example: 100 GB source  100 GB target minimum
- +10% margin recommended

Check:
```bash
# Linux/macOS
du -sh /source
df -h /target

# Windows
Get-ChildItem -Recurse /source | Measure-Object -Sum Length
Get-Volume  # Free space
```

---

## Security & Reliability

### Q24: Are my files encrypted

**A:** **NO**, files are not encrypted by EasySave.

Files are copied as-is. To encrypt:
- Use BitLocker (Windows)
- Use LUKS (Linux)
- Use FileVault (macOS)

---

### Q25: What happens if target disk is full

**A:** Backup stops with `Error` status.

**Solution**:
- Free up space
- Restart backup
- Already copied files remain intact

---

### Q26: Does EasySave preserve file permissions

**A:** **NO**, permissions are not preserved.

Files are copied with default target filesystem permissions.

---

## Maintenance

### Q27: How do I clean old logs

**A:**

```bash
# Delete logs
rm -rf ~/.local/share/EasySave/logs  # Linux/macOS
rmdir %PROGRAMDATA%\EasySave\logs    # Windows
```

EasySave will recreate the folder on next backup.

---

### Q28: Can I manually edit backup_config.json

**A:** **Yes, but carefully**.

Format:
```json
[
  {
    "name": "MyBackup",
    "sourcePath": "/source",
    "targetPath": "/target",
    "type": "Full"
  }
]
```

**Rules**:
- Keep JSON structure valid
- Use absolute paths
- Types: `Full` or `Differential`
- Backup before editing

---

### Q29: How do I reset EasySave

**A:**

```bash
# Windows
rmdir %PROGRAMDATA%\EasySave /s

# Linux/macOS
rm -rf ~/.local/share/EasySave
```

Then restart application to recreate fresh configuration.

---

## Development

### Q30: How do I contribute to the project

**A:**

1. Fork the repository on GitHub
2. Create a branch: `git checkout -b feature/my-feature`
3. Commit: `git commit -m "Add my feature"`
4. Push: `git push origin feature/my-feature`
5. Open a Pull Request

[https://github.com/joanperreyprato/EasySave](https://github.com/joanperreyprato/EasySave)

---

## Support

### Didn't find your answer

Check:
-  **DOCUMENTATION.md** - Complete documentation
-  **QUICK_START.md** - Quick guide
-  **TECHNICAL_DOCUMENTATION.md** - Technical architecture

Or open an issue: [GitHub Issues](https://github.com/joanperreyprato/EasySave/issues)

---

**Last Updated**: January 2024
**Version**: 1.0

Have another question  Contact us on GitHub!
