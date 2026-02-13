# EasySave 2.0 - Quick Start Guide

Get started with EasySave 2.0 in minutes!

---

##  Installation

### Windows
```bash
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
dotnet build
```

### Launch the application

**Graphical Interface (WPF)**:
```bash
cd EasySave.WPF
dotnet run
```

**Console Interface (CLI)**:
```bash
cd EasySave.UI
dotnet run
```

---

##  Graphical Interface (WPF)

### 1. Create a backup job
1. Click **Add job**
2. Fill in:
   - Name: `MyDocuments`
   - Source: `C:\Users\MyName\Documents`
   - Destination: `D:\Backups\Documents`
   - Log format: JSON or XML
3. Click **OK**

### 2. Execute a backup
- **Single job**: Select it, click **Start selected**
- **All jobs**: Click **Start all**

### 3. Encryption (optional)
1. Go to **Encryption** tab
2. Enter an encryption key
3. Add file extensions to encrypt (e.g., `.txt`, `.docx`)
4. When executing, choose whether to encrypt

### 4. Change language
Use the dropdown menu at the top right (English / Français)

---

##  Console Interface (CLI)

### Interactive mode
```bash
cd EasySave.UI
dotnet run
```

Available menu:
- **1**: Create backup
- **2**: Execute backup
- **3**: List backups
- **4**: Edit backup
- **5**: Change language
- **6**: Exit

### Command mode
```bash
# Create
dotnet run -- create "Photos" "C:\Photos" "D:\Backup" full

# Execute
dotnet run -- execute 1

# List
dotnet run -- list
```

---

##  Key Features

### Backup types
| Type | Description |
|------|-------------|
| **Full** | Copies all files |
| **Differential** | Copies only modified files |

### AES-256 Encryption
- Protects sensitive files
- Filter by extensions
- Custom encryption key
- Integrity verification (HMAC-SHA256)

### Log formats
- **JSON**: Easy to parse
- **XML**: Compatible with legacy tools

---

##  Configuration

Files stored in:
- **Windows**: `C:\ProgramData\EasySave\`

| File | Content |
|------|---------|
| `backup_config.json` | Job configuration |
| `state.json` | Real-time status |
| `logs/` | Operation history |

---

##  Common Errors

| Problem | Solution |
|---------|----------|
| Invalid source path | Verify the folder exists |
| Invalid destination path | Create the folder or check permissions |
| Invalid name | Use only alphanumeric, spaces, dashes |
| Encryption error | Verify the key is set |

---

##  Usage Examples

### Backup documents with encryption
1. WPF Interface: Add the job
2. Encryption tab: Enter a key
3. Add extensions: `.docx`, `.pdf`, `.xlsx`
4. Execute and choose "Yes" for encryption

### Batch automatic backup (CLI)
```bash
cd EasySave.UI
dotnet run -- create "Job1" "C:\Data" "D:\Backup" full
dotnet run -- create "Job2" "C:\Photos" "D:\Backup" full
dotnet run -- execute 1,2
```

---

##  Tips

 **First backup**: Always use **Full**

 **Regular updates**: Use **Differential** to save time

 **Sensitive files**: Enable encryption with a strong key

 **Monitoring**: State is updated in real-time in `state.json`

 **Logs**: Choose XML for compatibility, JSON for performance

---

##  Need Help

- **Full documentation**: `DOCUMENTATION.md`
- **Technical documentation**: `TECHNICAL_DOCUMENTATION.md`
- **FAQ**: `FAQ.md`
- **GitHub Issues**: [github.com/joanperreyprato/EasySave/issues](https://github.com/joanperreyprato/EasySave/issues)

---

**Version**: 2.0 | **Updated**: January 2025

 You're ready! Launch EasySave now!
