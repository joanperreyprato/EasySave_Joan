# EasySave — ProSoft Backup Manager

Welcome to **EasySave v1.1**, the professional backup solution by ProSoft. 

## Features
* Create up to 5 backup jobs simultaneously.
* Support for **Full** and **Differential** backup types.
* Interactive Console UI in **English** and **French**.
* Automate execution via command line arguments (`EasySave.CLI.exe 1-3`).
* Logs every transfer in either **JSON** or **XML** format (daily rotation).
* Real-time state tracking in `%ProgramData%\EasySave\state.json`.

---

## 🇫🇷 Manuel d'utilisation (Français)

### Lancement interactif
Double-cliquez sur l'exécutable ou lancez-le dans le terminal sans arguments :
```bash
EasySave.CLI.exe
```

1. **Menu principal** : Naviguez avec les numéros (1 à 5) pour lister, créer, exécuter, supprimer un travail, ou changer les paramètres.
2. **Création d'un travail (Max 5)** : Le logiciel vous demandera un nom (alphanumérique), un dossier source existant, et un dossier de destination. Choisissez "Complète" (Full) ou "Différentielle" (Differential).
3. **Exécution** : Lancez un ou plusieurs travaux en tapant leurs numéros (ex: `1-3` ou `1;2;4`).
4. **Paramètres** : Changez la langue ou le format des logs journaliers (JSON/XML).

### Mode Command-Line
Pour l'intégration avec des scripts (Cron, Tâches planifiées), vous pouvez lancer les sauvegardes directement :
```bash
EasySave.CLI.exe 1-3      # Exécute les travaux de 1 à 3
EasySave.CLI.exe 1;4      # Exécute les travaux 1 et 4
EasySave.CLI.exe all      # Exécute tous les travaux
```

### Fichiers de données
* **Configuration des jobs** : `%ProgramData%\EasySave\backup_config.json`
* **État temps réel** : `%ProgramData%\EasySave\state.json`
* **Dossier des logs** : `%ProgramData%\EasySave\Logs\`

---

## 🇬🇧 User Manual (English)

### Interactive Mode
Launch the executable without arguments to enter interactive mode:
```bash
EasySave.CLI.exe
```

1. **Main Menu**: Use numbers (1-5) to list, create, execute, delete jobs, or change settings.
2. **Job Creation (Max 5)**: Provide an alphanumeric name, a valid source directory, and a target directory. Select between "Full" and "Differential" modes.
3. **Execution**: Run one or multiple jobs by entering their IDs (e.g. `1-3` or `1;2;4`).
4. **Settings**: Change the UI language or the log format (JSON/XML).

### Command-Line Mode
For scripting integration, you can execute jobs without entering the UI:
```bash
EasySave.CLI.exe 1-3      # Executes jobs 1, 2, and 3
EasySave.CLI.exe 1;4      # Executes jobs 1 and 4
EasySave.CLI.exe all      # Executes all configured jobs
```

### Data Files Location
* **Job Configuration**: `%ProgramData%\EasySave\backup_config.json`
* **Real-time State**: `%ProgramData%\EasySave\state.json`
* **Daily Logs Directory**: `%ProgramData%\EasySave\Logs\`
