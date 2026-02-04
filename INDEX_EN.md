# ?? EasySave - English Documentation Index

Welcome to EasySave's complete documentation! Find what you need quickly.

---

## ?? Quick Start

### For impatient users
?? **[QUICK_START_EN.md](QUICK_START_EN.md)** - 5 minutes to first backup
- Installation in 3 commands
- First backup in interactive mode
- First CLI trials

### In a rush?
?? **Getting started command**:
```bash
git clone https://github.com/joanperreyprato/EasySave.git && cd EasySave && dotnet run
```

---

## ?? Complete Documentation

### For regular users
?? **[DOCUMENTATION_EN.md](DOCUMENTATION_EN.md)** - Complete and detailed guide
- All features explained
- Detailed installation
- Usage (interactive + CLI modes)
- Backup types explained
- Configuration & files
- Error codes
- State monitoring
- Input validation
- Usage examples
- Complete troubleshooting

**Perfect for**: Users discovering EasySave

---

## ? FAQ - Frequently Asked Questions

### Have a question?
?? **[FAQ_EN.md](FAQ_EN.md)** - 30 questions/answers
- Installation & Configuration
- Backup creation
- Backup types
- Execution & Monitoring
- CLI mode
- Complete troubleshooting
- Performance
- Security
- Maintenance
- Development

**Perfect for**: Finding a quick answer

---

## ?? Technical Documentation

### For developers
?? **[TECHNICAL_DOCUMENTATION_EN.md](TECHNICAL_DOCUMENTATION_EN.md)** - Technical specifications
- Complete architecture with UML diagrams
- Detailed description of each component
- Execution flows
- Translation system (i18n)
- State management
- Data validation
- Design patterns (DI, Factory, etc.)
- Extensibility points
- Test recommendations

**Perfect for**: Contributors, maintainers, developers

---

## ?? Overview by Topic

### Installation & Configuration
| Question | Document |
|----------|----------|
| How to install? | [QUICK_START_EN.md](QUICK_START_EN.md) |
| Where are files? | [FAQ_EN.md - Q2](FAQ_EN.md) |
| How to change language? | [FAQ_EN.md - Q3](FAQ_EN.md) |
| Detailed installation | [DOCUMENTATION_EN.md](DOCUMENTATION_EN.md) |

### Usage
| Question | Document |
|----------|----------|
| First backup | [QUICK_START_EN.md](QUICK_START_EN.md) |
| Interactive mode | [DOCUMENTATION_EN.md](DOCUMENTATION_EN.md) |
| Command line mode | [DOCUMENTATION_EN.md](DOCUMENTATION_EN.md) |
| Concrete examples | [QUICK_START_EN.md](QUICK_START_EN.md) |

### Backups
| Question | Document |
|----------|----------|
| Full vs Diff? | [FAQ_EN.md - Q8](FAQ_EN.md) |
| Backup types | [DOCUMENTATION_EN.md](DOCUMENTATION_EN.md) |
| How many backups? | [FAQ_EN.md - Q5](FAQ_EN.md) |
| Execute multiple | [FAQ_EN.md - Q10](FAQ_EN.md) |

### Errors & Troubleshooting
| Error | Solution |
|-------|----------|
| "Invalid job name" | [FAQ_EN.md - Q16](FAQ_EN.md) |
| "Source not found" | [FAQ_EN.md - Q17](FAQ_EN.md) |
| "Target not found" | [FAQ_EN.md - Q18](FAQ_EN.md) |
| Duplicate name | [FAQ_EN.md - Q19](FAQ_EN.md) |
| Max reached | [FAQ_EN.md - Q20](FAQ_EN.md) |
| No files copied | [FAQ_EN.md - Q21](FAQ_EN.md) |
| Complete troubleshooting | [DOCUMENTATION_EN.md](DOCUMENTATION_EN.md) |

### Performance & Security
| Topic | Document |
|-------|----------|
| Speed up backup | [FAQ_EN.md - Q22](FAQ_EN.md) |
| Disk space | [FAQ_EN.md - Q23](FAQ_EN.md) |
| Encryption | [FAQ_EN.md - Q24](FAQ_EN.md) |
| Permissions | [FAQ_EN.md - Q26](FAQ_EN.md) |
| Disk full | [FAQ_EN.md - Q25](FAQ_EN.md) |

### Development
| Topic | Document |
|--------|----------|
| Architecture | [TECHNICAL_DOCUMENTATION_EN.md](TECHNICAL_DOCUMENTATION_EN.md) |
| Components | [TECHNICAL_DOCUMENTATION_EN.md](TECHNICAL_DOCUMENTATION_EN.md) |
| Execution flows | [TECHNICAL_DOCUMENTATION_EN.md](TECHNICAL_DOCUMENTATION_EN.md) |
| Extensibility | [TECHNICAL_DOCUMENTATION_EN.md](TECHNICAL_DOCUMENTATION_EN.md) |
| Design patterns | [TECHNICAL_DOCUMENTATION_EN.md](TECHNICAL_DOCUMENTATION_EN.md) |
| Contributing | [FAQ_EN.md - Q30](FAQ_EN.md) |

---

## ?? Recommended Reading Paths

### I'm new to EasySave
```
1. QUICK_START_EN.md          (5 min)
2. DOCUMENTATION_EN.md        (30 min)
3. FAQ_EN.md                  (consult as needed)
```

### I have a specific question
```
1. FAQ_EN.md (search Q+number)
2. If not found ? DOCUMENTATION_EN.md
3. If technical ? TECHNICAL_DOCUMENTATION_EN.md
```

### I want to contribute
```
1. QUICK_START_EN.md          (installation)
2. TECHNICAL_DOCUMENTATION_EN.md (understand architecture)
3. Source code                (read + modify)
4. Tests                      (validate)
```

### I'm having problems
```
1. FAQ_EN.md - Troubleshooting section
2. DOCUMENTATION_EN.md - Troubleshooting section
3. Check backup_config.json and state.json
4. See logs in logs/ folder
```

---

## ?? Detailed Table of Contents

### QUICK_START_EN.md
- Quick installation
- First steps
- Command line mode
- Backup types
- Configuration
- Common errors
- Practical examples
- Track progress
- Quick troubleshooting
- Checklist

### DOCUMENTATION_EN.md
- Features
- Detailed installation
- Usage (interactive + CLI)
- Backup types
- Architecture
- Configuration
- Error codes
- Task state
- Input validation
- Usage examples
- Complete troubleshooting

### TECHNICAL_DOCUMENTATION_EN.md
- Architecture (UML diagrams)
- Each component description
- Execution flows (diagrams)
- Translation system (i18n)
- State management (lifecycles)
- Data validation
- Error handling
- Design patterns (DI, Interfaces, Enums)
- Extensibility points
- Test recommendations

### FAQ_EN.md
- **Installation & Configuration** (Q1-Q3)
- **Backup Creation** (Q4-Q7)
- **Backup Types** (Q8-Q10)
- **Execution & Monitoring** (Q11-Q13)
- **CLI Mode** (Q14-Q15)
- **Troubleshooting** (Q16-Q21)
- **Performance & Optimization** (Q22-Q23)
- **Security & Reliability** (Q24-Q26)
- **Maintenance** (Q27-Q29)
- **Development** (Q30)

---

## ?? Useful Links

### External Resources
- **GitHub**: [joanperreyprato/EasySave](https://github.com/joanperreyprato/EasySave)
- **.NET**: [dotnet.microsoft.com](https://dotnet.microsoft.com)
- **Git**: [git-scm.com](https://git-scm.com)

### In Project
- **Source code**: `EasySave v1.0/` (.cs files)
- **Tests**: (to add)
- **Configuration**: `%ProgramData%\EasySave\` or `~/.local/share/EasySave/`

---

## ?? Reading Tips

### For quick answers
- ?? **5 min** ? QUICK_START_EN.md
- ?? **2 min** ? FAQ_EN.md (search your question)
- ?? **Instant** ? Ctrl+F in document

### To understand the project
- ?? **30 min** ? DOCUMENTATION_EN.md
- ?? **60 min** ? TECHNICAL_DOCUMENTATION_EN.md

### To contribute
- ?? **90 min** ? Read all + explore code
- ?? **30 min** ? TECHNICAL_DOCUMENTATION_EN.md + code

---

## ?? Support & Contributions

### Need help?
1. Check FAQ_EN.md
2. Read DOCUMENTATION_EN.md (Troubleshooting section)
3. Open an issue on GitHub

### Found a bug?
[Report on GitHub](https://github.com/joanperreyprato/EasySave/issues/new)

### Have an idea?
[Discuss on GitHub](https://github.com/joanperreyprato/EasySave/discussions)

### Want to contribute?
See FAQ_EN.md - Q30 (Contributing)

---

## ?? Documentation Statistics

| Document | Pages | Reading Time | For Who |
|----------|-------|---------------|---------|
| QUICK_START_EN.md | 3 | 5 min | Everyone |
| DOCUMENTATION_EN.md | 8 | 30 min | Users |
| TECHNICAL_DOCUMENTATION_EN.md | 10 | 60 min | Developers |
| FAQ_EN.md | 7 | Variable | Everyone |
| **TOTAL** | **28 pages** | **~2 hours** | - |

---

## ?? Recommended Reading Order

**To get started**:
```
1?? QUICK_START_EN.md
2?? Test the application
3?? DOCUMENTATION_EN.md (if needed)
4?? FAQ_EN.md (consult as needed)
```

**To explore**:
```
1?? DOCUMENTATION_EN.md (complete)
2?? Read source code (understand)
3?? TECHNICAL_DOCUMENTATION_EN.md (go deeper)
```

**To develop**:
```
1?? QUICK_START_EN.md (setup)
2?? TECHNICAL_DOCUMENTATION_EN.md (architecture)
3?? Explore code (details)
4?? Modify + Test
```

---

## ? Documentation Checklist

Complete documentation includes:
- ? Quick guide (5 min)
- ? Complete user guide
- ? FAQ (30 questions)
- ? Technical documentation
- ? Practical examples
- ? Troubleshooting guide
- ? Navigation index (this file)

---

**Last Updated**: January 2024
**Version**: 1.0
**Status**: Complete Documentation ?

?? **You have everything you need to get started!**

Choose your document above and let's go! ??
