# EasySave 2.0 - FAQ (English)

Frequently asked questions about EasySave 2.0

---

## Installation & Configuration

### Q1: What are the prerequisites
**.NET 8.0** or higher. Windows for WPF, all OS for CLI.

### Q2: How to install
```bash
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
dotnet build
```

### Q3: Where are configuration files stored
**Windows**: `C:\ProgramData\EasySave\`

---

## Usage

### Q4: How to create a backup in WPF
**Add job**  Fill name/source/destination  **OK**

### Q5: How to execute multiple backups
Select them (Ctrl+Click) then **Start selected**, or **Start all** for all.

### Q6: Can I cancel a running backup
No, this feature is not currently implemented.

### Q7: How to create a backup in CLI
```bash
dotnet run -- create "Name" "C:\Source" "D:\Dest" full
```

---

## Backup Types

### Q8: What's the difference between Full and Differential
- **Full**: Copies all files
- **Differential**: Copies only new/modified files

### Q9: When to use Full vs Differential
**Full** for first time or periodically. **Differential** for daily updates.

### Q10: How does Differential detect changes
Compares file modification date and size.

---

## Encryption

### Q11: How to enable encryption
1. **Encryption** tab  Enter a key
2. Add extensions (`.docx`, `.pdf`)
3. When executing, choose "Yes"

### Q12: Which algorithm is used
**AES-256-CBC** with **HMAC-SHA256** integrity verification.

### Q13: How to decrypt a file
Use `CryptoService.DecryptFile(input, output, key)` from CryptoSoft project.

### Q14: Can I encrypt only certain files
Yes, add only desired extensions to the filter list.

---

## Configuration & Logs

### Q15: Where are the logs
`C:\ProgramData\EasySave\logs\`

### Q16: JSON or XML for logs
**JSON**: Faster and compact. **XML**: Compatible with legacy tools.

### Q17: How to reset configuration
Delete `C:\ProgramData\EasySave\` and restart the application.

---

## Performance

### Q18: My backup is slow, why
- Slow disk
- Many files
- Encryption enabled

**Solution**: Use Differential after Full, disable encryption if not needed.

### Q19: How many jobs can I create
Maximum **5 jobs** (modifiable in code if needed).

---

## Errors

### Q20: "Source path invalid"
Source path doesn't exist or is malformed. Check the path.

### Q21: "Encryption key missing"
You requested encryption without setting a key. Go to Encryption tab.

### Q22: Error during file copy
Application continues with next files. Check logs (`transferTime: -1`).

---

## Miscellaneous

### Q23: How to change language
**WPF**: Dropdown menu at top right. **CLI**: Menu option 5.

### Q24: Can I backup to network
Yes, if network path is mounted and accessible (e.g., `\\server\share`).

### Q25: Does the app work offline
Yes, no Internet connection required.

---

## Support

### Q26: Where to find more help
- [DOCUMENTATION.md](DOCUMENTATION.md) - Complete guide
- [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md) - Technical documentation
- [GitHub Issues](https://github.com/joanperreyprato/EasySave/issues) - Report a bug

---

**Version**: 2.0 | **Last updated**: January 2025
