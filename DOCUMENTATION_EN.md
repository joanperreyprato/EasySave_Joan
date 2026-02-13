# EasySave 2.0 - Documentation

Professional file backup application with WPF GUI and AES-256 encryption.

---

## Table of Contents

1. [Installation](#installation)
2. [Graphical Interface (WPF)](#graphical-interface-wpf)
3. [Console Interface (CLI)](#console-interface-cli)
4. [Backup Types](#backup-types)
5. [Encryption](#encryption)
6. [Configuration](#configuration)
7. [Logs](#logs)
8. [Troubleshooting](#troubleshooting)

---

## Installation

### Prerequisites
- **.NET 8.0** or higher
- **Windows** (for WPF) or **Windows/Linux/macOS** (for CLI)

### Build
```bash
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
dotnet build
```

---

## Graphical Interface (WPF)

### Launch
```bash
cd EasySave.WPF
dotnet run
```

### Create a Backup Job

1. Click **Add job**
2. Fill the form:
   - **Name**: Alphanumeric, spaces, dashes (max 50 chars)
   - **Source**: Source folder path (must exist)
   - **Destination**: Destination folder path (must exist)
   - **Log format**: JSON (fast) or XML (compatible)
3. Click **OK**

### Execute Backups

| Action | Button |
|--------|--------|
| Execute one backup | Select + **Start selected** |
| Execute all | **Start all** |
| Stop (not supported) | Notification displayed |

### Encryption

1. Go to **Encryption** tab
2. Enter an encryption key in the PasswordBox field
3. Add extensions to encrypt:
   - Type extension (e.g., `.docx`)
   - Click **Add**
4. To remove: Select + **Remove**
5. When executing, a dialog asks if you want to encrypt

### Change Language

Use the dropdown menu at the top right:
- **English**
- **Français**

---

## Console Interface (CLI)

### Launch
```bash
cd EasySave.UI
dotnet run
```

### Interactive Mode

Main menu:
```
1. Create Backup    - Create a backup
2. Execute Backup   - Execute a backup
3. List Backups     - List backups
4. Edit Backup      - Edit a backup
5. Change Language  - Change language
6. Exit             - Exit
```

### Command Mode

#### Create a backup
```bash
dotnet run -- create <name> <source> <destination> <type>
```

Example:
```bash
dotnet run -- create "MyDocs" "C:\Documents" "D:\Backup" full
```

#### Execute a backup
```bash
dotnet run -- execute <index>
```

Examples:
```bash
# Single
dotnet run -- execute 1

# Multiple (range)
dotnet run -- execute 1-3

# Multiple (list)
dotnet run -- execute 1,3,5
```

#### List backups
```bash
dotnet run -- list
```

---

## Backup Types

### Full Backup

- **When**: First backup or periodic full backup
- **Behavior**: Copies ALL files from source to destination
- **Duration**: Longer
- **Advantage**: Simple restoration (everything in destination)

### Differential Backup

- **When**: Regular updates after a full backup
- **Behavior**: Copies only new or modified files
- **Criteria**: Comparison of modification date and size
- **Duration**: Faster
- **Advantage**: Saves time and space

### Recommendation

```
Day 1   : Full backup       (copy everything)
Day 2-7 : Differential      (quick updates)
Day 8   : Full backup       (new baseline)
```

---

## Encryption

### Algorithm

- **AES-256-CBC**: Standard symmetric encryption
- **HMAC-SHA256**: Integrity verification
- **Derived key**: SHA-256 of text key

### WPF Usage

1. Set key in **Encryption** tab
2. Add extensions to encrypt (e.g., `.pdf`, `.docx`, `.xlsx`)
3. Start backup
4. Choose "Yes" to encryption question

### How It Works

1. Backup executes normally
2. If encryption enabled:
   - For each copied file
   - If extension matches filter
   - File is encrypted in place
   - Encryption time is logged

### Encrypted File Format

```
[1 byte: IV length][IV][Ciphertext][32 bytes: HMAC]
```

### Decryption

The project includes `CryptoSoft` with `DecryptFile` method:
```csharp
CryptoService.DecryptFile(input, output, key);
```

---

## Configuration

### File Location

- **Windows**: `C:\ProgramData\EasySave\`

### Configuration Files

#### backup_config.json
Stores backup jobs:
```json
[
  {
    "name": "MyDocs",
    "sourcePath": "C:\\Documents",
    "targetPath": "D:\\Backup",
    "type": 0,
    "logFormat": 0
  }
]
```

| Field | Description |
|-------|-------------|
| `name` | Job name |
| `sourcePath` | Source path |
| `targetPath` | Destination path |
| `type` | 0=Full, 1=Differential |
| `logFormat` | 0=JSON, 1=XML |

#### state.json
Real-time backup state:
```json
{
  "name": "MyDocs",
  "timestamp": "2025-01-20T10:30:00Z",
  "status": 1,
  "totalFiles": 100,
  "totalSize": 104857600,
  "remainingFiles": 50,
  "remainingSize": 52428800,
  "currentSourceFile": "C:\\Documents\\file.txt",
  "currentTargetFile": "D:\\Backup\\file.txt",
  "progress": 50.0
}
```

| Status | Value |
|--------|-------|
| Inactive | 0 |
| Active | 1 |
| Completed | 2 |
| Error | 3 |

---

## Logs

### Available Formats

| Format | Advantages | Disadvantages |
|--------|------------|---------------|
| **JSON** | Easy to parse, compact | Less visually readable |
| **XML** | Standard, legacy tool compatible | More verbose |

### Location

`C:\ProgramData\EasySave\logs\`

### Log Structure

#### File Entry
Logged after each file copied:
```json
{
  "timestamp": "2025-01-20T10:30:15Z",
  "jobName": "MyDocs",
  "sourcePath": "C:\\Documents\\file.txt",
  "targetPath": "D:\\Backup\\file.txt",
  "fileSize": 1024000,
  "transferTime": 150,
  "encryptionTime": 50,
  "entryType": "File"
}
```

#### Summary Entry
Logged at backup end:
```json
{
  "timestamp": "2025-01-20T10:35:00Z",
  "jobName": "MyDocs",
  "sourcePath": "C:\\Documents",
  "targetPath": "D:\\Backup",
  "fileSize": 104857600,
  "transferTime": 30000,
  "entryType": "Summary"
}
```

| Field | Description |
|-------|-------------|
| `transferTime` | Transfer time (ms) |
| `encryptionTime` | Encryption time (ms), -1 if error |
| `entryType` | File or Summary |

---

## Troubleshooting

### Common Errors

#### "Source path invalid"
**Cause**: Source path malformed or doesn't exist  
**Solution**: Verify path exists and is accessible

#### "Target path invalid"
**Cause**: Destination path malformed or doesn't exist  
**Solution**: Create destination folder or check permissions

#### "Invalid job name"
**Cause**: Unauthorized characters in name  
**Solution**: Use only alphanumeric, spaces, dashes, underscores

#### "Duplicate name"
**Cause**: A job with this name already exists  
**Solution**: Choose a unique name

#### "Encryption key missing"
**Cause**: Encryption requested but key not set  
**Solution**: Set a key in Encryption tab

### Reset Configuration

```bash
# Delete configuration folder
Remove-Item -Recurse -Force "$env:ProgramData\EasySave"

# Restart application
dotnet run
```

### Check Logs

1. Open `C:\ProgramData\EasySave\logs\`
2. Browse JSON or XML files
3. Look for errors (`transferTime: -1` or `encryptionTime: -1`)

### Slow Performance

**Possible issues**:
- Slow source or destination disk
- Very numerous or large files
- Encryption enabled (adds time)

**Solutions**:
- Use Differential after first Full
- Disable encryption if not needed
- Check disk usage in Task Manager

---

## Known Limitations

- **No cancellation**: Cannot stop running backup
- **No pause**: Backups must complete
- **Job limit**: Maximum 5 jobs (modifiable in code)
- **Single-threaded**: Files copied sequentially

---

## Usage Tips

?? **First use**: Always do a Full backup

?? **Sensitive files**: Use encryption with strong key (12+ characters)

?? **Regular backups**: Weekly Full + Daily Differential

?? **Performance**: JSON is faster than XML

?? **Monitoring**: Check `state.json` to track progress

---

## Support

- **Technical documentation**: [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md)
- **FAQ**: [FAQ.md](FAQ.md)
- **GitHub Issues**: [github.com/joanperreyprato/EasySave/issues](https://github.com/joanperreyprato/EasySave/issues)

---

**Version**: 2.0  
**Last updated**: January 2025

? Complete documentation - Ready to use EasySave!
