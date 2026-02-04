# EasySave v1.0

**[ Français](#français) | [ English](#english)**

---

<a name="français"></a>

#  EasySave - Documentation Française

**Application de sauvegarde multilingue et performante** - Développée en C# avec .NET 8

##  Table des matières

- [Caractéristiques](#caractéristiques)
- [Installation rapide](#installation-rapide)
- [Utilisation](#utilisation)
- [Types de sauvegarde](#types-de-sauvegarde)
- [FAQ](#faq-français)

---

## ? Caractéristiques

- ? **Deux modes de fonctionnement** : interactif et ligne de commande
- ? **Deux types de sauvegarde** :
  - **Complète** (Full) : copie tous les fichiers
  - **Différentielle** (Diff) : copie uniquement les fichiers nouveaux ou modifiés
- ? **Multilingue** : Support de l'anglais et du français
- ? **Suivi d'état** : progression en temps réel des sauvegardes
- ? **Journalisation** : enregistrement détaillé des opérations
- ? **Configuration persistante** : sauvegarde des tâches de sauvegarde
- ? **Gestion robuste des erreurs** : validation complète des chemins et paramètres
- ? **Architecture modulaire** : Facile à étendre et maintenir

---

##  Installation rapide

### Prérequis
- **.NET 8.0 ou supérieur**
- **Git** (pour cloner le repository)

### Étapes

1. **Cloner le repository**
   ```bash
   git clone https://github.com/joanperreyprato/EasySave.git
   cd EasySave
   ```

2. **Compiler**
   ```bash
   dotnet build
   ```

3. **Exécuter**
   ```bash
   dotnet run
   ```

---

##  Utilisation

### Mode interactif

```bash
dotnet run
```

**Menu principal** :
```
=== EasySave ===
1. Create Backup (Créer une sauvegarde)
2. Execute Backup (Exécuter une sauvegarde)
3. List Backups (Lister les sauvegardes)
4. Exit (Quitter)
```

### Mode ligne de commande

#### Créer une sauvegarde
```bash
dotnet run -- create <nom> <source> <cible> [full|diff]
```

**Exemple** :
```bash
dotnet run -- create "MonBackup" "C:\Users\Data" "D:\Backup" full
```

#### Exécuter une sauvegarde
```bash
# Exécuter une sauvegarde
dotnet run -- execute 0

# Exécuter plusieurs (indices 0, 1, 2)
dotnet run -- execute 0-2

# Exécuter spécifiques (0, 2, 4)
dotnet run -- execute 0,2,4
```

#### Lister les sauvegardes
```bash
dotnet run -- list
```

---

##  Types de sauvegarde

### Sauvegarde complète (Full)
- Copie **TOUS les fichiers** du dossier source
- Idéale pour les premières sauvegardes
- Temps d'exécution plus long
- Espace disque : égal à la source

### Sauvegarde différentielle (Diff)
- Copie uniquement les fichiers **nouveaux ou modifiés**
- Détecte les modifications par date et taille
- Idéale pour les mises à jour régulières
- Très rapide après première sauvegarde
- Espace disque : seulement pour fichiers modifiés

---

##  Configuration

### Fichiers automatiquement créés

- **Windows** : `C:\ProgramData\EasySave\`
- **Linux/macOS** : `~/.local/share/EasySave/`

**Fichiers** :
- `backup_config.json` - Tâches configurées
- `state.json` - État actuel des sauvegardes
- `logs/` - Historique des opérations

### Changer la langue

Modifiez `Program.cs` ligne 2 :
```csharp
var language = new LanguageProvider("fr");  // Français
var language = new LanguageProvider("en");  // Anglais
```

---

## ? FAQ Français

Pour 30 questions-réponses détaillées, consultez **[FAQ.md](FAQ.md)**

**Questions couvertes** :
- ? Installation & Configuration (Q1-Q3)
- ? Création de sauvegardes (Q4-Q7)
- ? Types de sauvegarde (Q8-Q10)
- ? Exécution & Suivi (Q11-Q13)
- ? Mode CLI (Q14-Q15)
- ? Dépannage (Q16-Q21)
- ? Performance (Q22-Q23)
- ? Sécurité (Q24-Q26)
- ? Maintenance (Q27-Q29)
- ? Contribution (Q30)

---

##  Documentation complète

- **[START_HERE.md](START_HERE.md)** - Guide visuel d'accueil
- **[QUICK_START.md](QUICK_START.md)** - Démarrage 5 minutes
- **[DOCUMENTATION.md](DOCUMENTATION.md)** - Guide utilisateur complet (30 pages)
- **[TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md)** - Guide développeur
- **[FAQ.md](FAQ.md)** - 30 Questions-Réponses
- **[INDEX.md](INDEX.md)** - Index de navigation

---

<a name="english"></a>

#  EasySave - English Documentation

**Multilingual and efficient backup application** - Built with C# and .NET 8

##  Table of Contents

- [Features](#features)
- [Quick Start](#quick-start)
- [Usage](#usage)
- [Backup Types](#backup-types)
- [FAQ](#faq-english)

---

## ? Features

- ? **Two operating modes** : interactive and command-line
- ? **Two backup types** :
  - **Full** : copies all files
  - **Differential** : copies only new or modified files
- ? **Multilingual** : English and French support
- ? **Real-time tracking** : progress monitoring during backups
- ? **Logging** : detailed operation records
- ? **Persistent configuration** : saves backup tasks
- ? **Robust error handling** : complete path and parameter validation
- ? **Modular architecture** : Easy to extend and maintain

---

##  Quick Start

### Requirements
- **.NET 8.0 or higher**
- **Git** (to clone the repository)

### Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/joanperreyprato/EasySave.git
   cd EasySave
   ```

2. **Build**
   ```bash
   dotnet build
   ```

3. **Run**
   ```bash
   dotnet run
   ```

---

##  Usage

### Interactive Mode

```bash
dotnet run
```

**Main Menu** :
```
=== EasySave ===
1. Create Backup
2. Execute Backup
3. List Backups
4. Exit
```

### Command Line Mode

#### Create a backup
```bash
dotnet run -- create <name> <source> <target> [full|diff]
```

**Example** :
```bash
dotnet run -- create "MyBackup" "C:\Users\Data" "D:\Backup" full
```

#### Execute a backup
```bash
# Execute one backup
dotnet run -- execute 0

# Execute multiple (indices 0, 1, 2)
dotnet run -- execute 0-2

# Execute specific (0, 2, 4)
dotnet run -- execute 0,2,4
```

#### List backups
```bash
dotnet run -- list
```

---

##  Backup Types

### Full Backup
- Copies **ALL files** from source folder
- Ideal for initial backups
- Longer execution time
- Disk space : equal to source size

### Differential Backup
- Copies only **new or modified files**
- Detects changes by date and size
- Ideal for regular updates
- Very fast after initial backup
- Disk space : only for modified files

---

##  Configuration

### Auto-Created Files

- **Windows** : `C:\ProgramData\EasySave\`
- **Linux/macOS** : `~/.local/share/EasySave/`

**Files** :
- `backup_config.json` - Configured tasks
- `state.json` - Current backup state
- `logs/` - Operation history

### Change Language

Edit `Program.cs` line 2 :
```csharp
var language = new LanguageProvider("en");  // English
var language = new LanguageProvider("fr");  // French
```

---

## ? FAQ English

For 30 detailed Q&A, see **[FAQ.md](FAQ.md)**

**Topics covered** :
- ? Installation & Configuration (Q1-Q3)
- ? Backup Creation (Q4-Q7)
- ? Backup Types (Q8-Q10)
- ? Execution & Monitoring (Q11-Q13)
- ? CLI Mode (Q14-Q15)
- ? Troubleshooting (Q16-Q21)
- ? Performance (Q22-Q23)
- ? Security (Q24-Q26)
- ? Maintenance (Q27-Q29)
- ? Contributing (Q30)

---

##  Complete Documentation

- **[START_HERE.md](START_HERE.md)** - Visual welcome guide
- **[QUICK_START.md](QUICK_START.md)** - 5-minute start
- **[DOCUMENTATION.md](DOCUMENTATION.md)** - Complete user guide (30 pages)
- **[TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md)** - Developer guide
- **[FAQ.md](FAQ.md)** - 30 Q&A
- **[INDEX.md](INDEX.md)** - Navigation index

---

##  Getting Started

### Choose Your Path

**I'm new to EasySave** ?
1. Read [QUICK_START.md](QUICK_START.md) (5 min)
2. Run `dotnet run`
3. Create first backup

**I have questions** ?
1. Check [FAQ.md](FAQ.md)
2. Use Ctrl+F to search

**I'm a developer** ?
1. Read [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md)
2. Explore source code

---

##  Support

### Get Help
-  Check [DOCUMENTATION.md](DOCUMENTATION.md)
- ? See [FAQ.md](FAQ.md)
-  Read [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md)
-  Browse [INDEX.md](INDEX.md)

### Report Issues
-  [GitHub Issues](https://github.com/joanperreyprato/EasySave/issues)

### Contribute
-  Fork ? Branch ? Commit ? Pull Request
- See [FAQ.md](FAQ.md) Q30 for details

---

##  Key Information

| Aspect | Details |
|--------|---------|
| **Version** | 1.0 |
| **Language** | Français / English   |
| **Built with** | C# .NET 8 |
| **License** | [See LICENSE](LICENSE) |
| **Repository** | [GitHub](https://github.com/joanperreyprato/EasySave) |

---

## ? Documentation Statistics

- **8 Markdown files** created
- **~31 pages** of documentation
- **~19,000 words**
- **30+ Q&A**
- **10+ practical examples**
- **100% topic coverage**
- **Bilingual**  

---

##  Ready to Start?

 **[Read START_HERE.md](START_HERE.md)** for visual guide
 **[Read QUICK_START.md](QUICK_START.md)** for 5-minute start
 **Run `dotnet run`** to begin now

---

**Last Updated** : January 2024
**Version** : 1.0
**Status** : ? Complete Documentation