# EasySave 2.0

 **Application de sauvegarde de fichiers avec interface graphique et chiffrement**

 **File backup application with GUI and encryption**

---

##  Fonctionnalités / Features

###  Français
-  **Interface graphique WPF** moderne et intuitive
-  **Interface console CLI** pour automatisation
-  **Deux types de sauvegarde** : complète et différentielle
-  **Chiffrement AES-256** avec filtrage par extensions
-  **Logs JSON et XML** au choix
-  **Multilingue** : Français et Anglais
-  **Suivi en temps réel** de la progression
-  **Gestion d'état** persistante

###  English
-  **Modern WPF GUI** with intuitive interface
-  **CLI console interface** for automation
-  **Two backup types**: full and differential
-  **AES-256 encryption** with extension filtering
-  **JSON and XML logs** available
-  **Multilingual**: French and English
-  **Real-time progress** tracking
-  **Persistent state** management

---

##  Installation Rapide / Quick Start

### Prérequis / Prerequisites
- .NET 8.0 ou supérieur / or higher
- Windows (WPF) ou Windows/Linux/macOS (CLI)

### Compiler / Build
```bash
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
dotnet build
```

### Lancer / Run

**Interface graphique / GUI (WPF)**:
```bash
cd EasySave.WPF
dotnet run
```

**Console / CLI**:
```bash
cd EasySave.UI
dotnet run
```

---

##  Documentation

###  Français
- **[Guide Rapide](QUICK_START.md)** - Démarrer en 5 minutes
- **[Documentation Complète](DOCUMENTATION.md)** - Guide utilisateur détaillé
- **[Documentation Technique](TECHNICAL_DOCUMENTATION.md)** - Pour développeurs
- **[FAQ](FAQ.md)** - Questions fréquentes

###  English
- **[Quick Start](QUICK_START_EN.md)** - Get started in 5 minutes
- **[Full Documentation](DOCUMENTATION_EN.md)** - Detailed user guide
- **[Technical Documentation](TECHNICAL_DOCUMENTATION_EN.md)** - For developers
- **[FAQ](FAQ_EN.md)** - Frequently asked questions

---

##  Exemples / Examples

###  Interface WPF
1. Ajoutez un travail avec **Add job**
2. Configurez le chiffrement dans l'onglet **Chiffrement**
3. Lancez avec **Start all** ou **Start selected**

###  WPF Interface
1. Add a job with **Add job**
2. Configure encryption in **Encryption** tab
3. Start with **Start all** or **Start selected**

### CLI / Console
```bash
# Créer / Create
dotnet run -- create "MyBackup" "C:\Source" "D:\Dest" full

# Exécuter / Execute
dotnet run -- execute 1

# Lister / List
dotnet run -- list
```

---

##  Chiffrement / Encryption

###  Utilisation
1. Définissez une clé de chiffrement
2. Sélectionnez les extensions à chiffrer (`.docx`, `.pdf`, etc.)
3. Lors de l'exécution, choisissez de chiffrer ou non
4. Algorithme : **AES-256-CBC** avec **HMAC-SHA256**

###  Usage
1. Set an encryption key
2. Select extensions to encrypt (`.docx`, `.pdf`, etc.)
3. When executing, choose whether to encrypt
4. Algorithm: **AES-256-CBC** with **HMAC-SHA256**

---

##  Structure du Projet / Project Structure

```
EasySave/
 EasySave.WPF/          # Interface graphique / GUI
 EasySave.UI/           # Interface console / CLI
 EasySave v1.0/         # Bibliothèque principale / Core library
 EasyLog/               # Système de logs / Logging system
 CryptoSoft/            # Module de chiffrement / Encryption module
 EasySave.Tests/        # Tests unitaires / Unit tests
 docs/                  # Documentation / Documentation
```

---

##  Technologies

- **C# / .NET 8.0**
- **WPF** pour l'interface graphique / for GUI
- **AES-256** pour le chiffrement / for encryption
- **JSON/XML** pour les logs / for logging
- **xUnit** pour les tests / for testing

---

##  Statut / Status

| Fonctionnalité / Feature | Statut / Status |
|--------------------------|-----------------|
| Interface WPF / GUI |  Complète / Complete |
| Interface CLI / Console |  Complète / Complete |
| Sauvegarde Full / Full Backup |  Fonctionnelle / Working |
| Sauvegarde Diff / Differential |  Fonctionnelle / Working |
| Chiffrement / Encryption |  AES-256 |
| Multilingue / Multilingual |  FR + EN |
| Tests / Tests |  Unitaires / Unit tests |

---

##  Contribution

 Les contributions sont les bienvenues ! Consultez [DOCUMENTATION.md](DOCUMENTATION.md) pour les détails.

 Contributions are welcome! See [DOCUMENTATION.md](DOCUMENTATION.md) for details.

---

##  License

Projet académique / Academic project

---

##  Liens / Links

- **GitHub**: [github.com/joanperreyprato/EasySave](https://github.com/joanperreyprato/EasySave)
- **Issues**: [GitHub Issues](https://github.com/joanperreyprato/EasySave/issues)

---

##  Contact

 Pour toute question, ouvrez une issue sur GitHub.

 For any question, open an issue on GitHub.

---

**Version**: 2.0 | **Date**: Janvier 2025 / January 2025

 **EasySave - Sauvegardez vos données en toute simplicité / Backup your data with ease**
