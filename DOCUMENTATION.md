# EasySave v1.0 - Documentation

**EasySave** est une application console de sauvegarde de fichiers multilingue et performante, développée en C# avec .NET 8.

##  Table des matières

- [Caractéristiques](#caractéristiques)
- [Installation](#installation)
- [Utilisation](#utilisation)
  - [Mode interactif](#mode-interactif)
  - [Mode ligne de commande](#mode-ligne-de-commande)
- [Types de sauvegarde](#types-de-sauvegarde)
- [Architecture](#architecture)
- [Configuration](#configuration)

---

##  Caractéristiques

-  **Deux modes de fonctionnement** : interactif et ligne de commande
-  **Deux types de sauvegarde** :
  - **Complète** : copie tous les fichiers
  - **Différentielle** : copie uniquement les fichiers nouveaux ou modifiés
-  **Multilingue** : Support de l'anglais et du français
-  **Suivi d'état** : progression en temps réel des sauvegardes
-  **Journalisation** : enregistrement détaillé des opérations
-  **Configuration persistante** : sauvegarde des tâches de sauvegarde
-  **Gestion robuste des erreurs** : validation complète des chemins et paramètres

---

##  Installation

### Prérequis
- .NET 8.0 ou supérieur
- Windows, Linux ou macOS

### Étapes d'installation

1. **Cloner le repository**
   ```bash
   git clone https://github.com/joanperreyprato/EasySave.git
   cd EasySave
   ```

2. **Compiler le projet**
   ```bash
   dotnet build
   ```

3. **Exécuter l'application**
   ```bash
   dotnet run
   ```

---

##  Utilisation

### Mode interactif

Lancez l'application sans arguments pour accéder au menu principal :

```bash
dotnet run
```

#### Menu principal
```
=== EasySave ===
1. Create Backup
2. Execute Backup
3. List Backups
4. Exit
```

#### Options disponibles

**1. Créer une sauvegarde**
- Entrez un nom unique (alphanumérique, espaces, tirets, underscores - max 50 caractères)
- Spécifiez le chemin source (doit exister)
- Spécifiez le chemin cible (doit exister)
- Choisissez le type : `full` (complète) ou `diff` (différentielle)

**2. Exécuter une sauvegarde**
- Sélectionnez la sauvegarde par son index

**3. Lister les sauvegardes**
- Affiche toutes les sauvegardes configurées avec leurs détails

**4. Quitter**
- Ferme l'application

---

### Mode ligne de commande

Utilisez les commandes suivantes :

#### Créer une sauvegarde
```bash
dotnet run -- create <nom> <source> <cible> [full|diff]
```

**Exemples:**
```bash
dotnet run -- create "Mes Documents" "C:\Users\Documents" "D:\Backups" full
dotnet run -- create "Photos" "/home/user/Photos" "/mnt/backup/photos" diff
```

#### Exécuter une sauvegarde
```bash
dotnet run -- execute <index>
```

**Exemples:**
```bash
# Exécuter la sauvegarde à l'index 0
dotnet run -- execute 0

# Exécuter plusieurs sauvegardes (indices 0, 1, 2)
dotnet run -- execute 0-2

# Exécuter des sauvegardes spécifiques (indices 0, 2, 4)
dotnet run -- execute 0,2,4
```

#### Lister les sauvegardes
```bash
dotnet run -- list
```

---

##  Types de sauvegarde

### Sauvegarde complète (Full)
- Copie **tous les fichiers** du dossier source
- Écrase les fichiers existants dans la cible
- Idéale pour les premières sauvegardes ou les restaurations complètes
- Temps d'exécution plus long (première exécution)

### Sauvegarde différentielle (Differential)
- Copie uniquement les fichiers **nouveaux ou modifiés**
- Détecte les modifications par :
  - Date de dernière modification
  - Taille du fichier
- Idéale pour les sauvegardes régulières et incrémentielles
- Temps d'exécution beaucoup plus rapide

---

##  Architecture

### Structure des composants

```
EasySave
 Program.cs              # Point d'entrée principal
 ConsoleUI.cs            # Interface utilisateur console
 BackupManager.cs        # Gestionnaire des tâches de sauvegarde
 BackupExecutor.cs       # Exécuteur des sauvegardes
 BackupJob.cs            # Représentation d'une tâche
 StateManager.cs         # Gestion de l'état des sauvegardes
 JobState.cs             # État d'une tâche en cours
 LanguageProvider.cs     # Système de traduction
 CommandLineParser.cs    # Analyse des arguments CLI
 EasyLog/
     JsonLogger.cs       # Enregistrement des logs
     LogEntry.cs         # Entrée de log
     ILogger.cs          # Interface du logger
```

### Flux de données

```
Program.cs (Point d'entrée)
    
ConsoleUI (Interface utilisateur)
    
BackupManager (Gestion des tâches)
    
BackupExecutor (Exécution)
    
StateManager  État des tâches
    
JsonLogger  Fichiers journaux
```

---

##  Configuration

### Fichiers de configuration

Les fichiers sont automatiquement créés dans :
- **Windows**: `%ProgramData%\EasySave\`
- **Linux/macOS**: `~/.local/share/EasySave/` (ou dossier équivalent)

#### `backup_config.json`
Contient la liste de toutes les tâches de sauvegarde configurées.

**Exemple:**
```json
[
  {
    "name": "Mes Documents",
    "sourcePath": "C:\\Users\\User\\Documents",
    "targetPath": "D:\\Backups\\Documents",
    "type": "Full"
  }
]
```

#### `state.json`
Contient l'état actuel de chaque tâche (progression, fichiers restants, etc.).

**Exemple:**
```json
[
  {
    "name": "Mes Documents",
    "timestamp": "2024-01-15T10:30:00Z",
    "status": "Completed",
    "totalFiles": 150,
    "totalSize": 1048576000,
    "remainingFiles": 0,
    "remainingSize": 0,
    "currentSourceFile": "",
    "currentTargetFile": "",
    "progress": 100.0
  }
]
```

#### `logs/`
Dossier contenant les fichiers journaux JSON avec l'historique des sauvegardes.

### Langues supportées

Deux langues disponibles : **Anglais** (par défaut) et **Français**

Pour changer la langue, modifiez le code dans `Program.cs` :
```csharp
var language = new LanguageProvider("fr");  // Français
var language = new LanguageProvider("en");  // Anglais
```

---

##  Codes d'erreur de création de sauvegarde

| Erreur | Description |
|--------|-------------|
| `InvalidName` | Le nom contient des caractères non autorisés ou dépasse 50 caractères |
| `SourcePathInvalid` | Le chemin source est invalide ou mal formé |
| `SourcePathNotFound` | Le répertoire source n'existe pas |
| `TargetPathInvalid` | Le chemin cible est invalide ou mal formé |
| `TargetPathNotFound` | Le répertoire cible n'existe pas |
| `DuplicateName` | Une sauvegarde avec ce nom existe déjà |
| `MaxJobsExceeded` | Maximum de 5 sauvegardes atteint |

---

##  État des tâches (JobState)

Chaque tâche possède un état avec les propriétés suivantes :

| Propriété | Type | Description |
|-----------|------|-------------|
| `Name` | string | Nom de la tâche |
| `Timestamp` | DateTime | Horodatage du dernier changement |
| `Status` | StateStatus | État actuel (Inactive, Active, Completed, Error) |
| `TotalFiles` | int | Nombre total de fichiers à sauvegarder |
| `TotalSize` | long | Taille totale en octets |
| `RemainingFiles` | int | Fichiers restant à traiter |
| `RemainingSize` | long | Taille restante en octets |
| `CurrentSourceFile` | string | Fichier source en cours de copie |
| `CurrentTargetFile` | string | Fichier destination en cours de copie |
| `Progress` | double | Pourcentage de progression (0-100) |

---

##  Validation des entrées

### Validation du nom de sauvegarde
-  Autorisé : caractères alphanumériques, espaces, tirets (`-`), underscores (`_`)
-  Interdit : caractères spéciaux (`@`, `#`, `$`, etc.)
-  Longueur : 1-50 caractères
-  Unicité : le nom ne doit pas exister déjà

### Validation des chemins
-  Format : chemin absolu valide
-  Existence : le répertoire doit exister
-  Permissions : l'application doit avoir accès en lecture/écriture

---

##  Exemples d'utilisation

### Exemple 1 : Sauvegarde complète initiale
```bash
# Création
dotnet run -- create "MonBackup" "/home/user/data" "/mnt/backup/data" full

# Exécution
dotnet run -- execute 0

# Vérification
dotnet run -- list
```

### Exemple 2 : Sauvegardes régulières différentielles
```bash
# Première sauvegarde (complète)
dotnet run -- create "Photos" "/home/user/Pictures" "/external/backup/pictures" full
dotnet run -- execute 0

# Sauvegardes suivantes (différentielles - plus rapides)
dotnet run -- create "Photos-Weekly" "/home/user/Pictures" "/external/backup/pictures-weekly" diff
dotnet run -- execute 1
```

### Exemple 3 : Exécution multiple
```bash
# Exécuter les sauvegardes 0, 1 et 2
dotnet run -- execute 0-2
```

---

##  Dépannage

### Le répertoire source n'existe pas
**Cause** : Le chemin indiqué n'existe pas ou a été supprimé.
**Solution** : Vérifiez que le chemin est correct et que le répertoire existe.

### Permission refusée
**Cause** : L'application n'a pas les permissions nécessaires.
**Solution** : Vérifiez les permissions en lecture/écriture sur les répertoires source et cible.

### Nom en doublon
**Cause** : Une sauvegarde avec ce nom existe déjà.
**Solution** : Utilisez un nom différent ou supprimez l'ancienne sauvegarde.

### Maximum de sauvegardes atteint
**Cause** : Vous avez créé 5 sauvegardes (limite actuelle).
**Solution** : Supprimez une sauvegarde existante avant d'en créer une nouvelle.

---

##  Licence

Ce projet est fourni à titre informatif.

---

##  Contribution

Les contributions sont bienvenues ! N'hésitez pas à :
- Signaler des bugs
- Proposer des améliorations
- Soumettre des pull requests

---

##  Support

Pour toute question ou problème, consultez le repository GitHub officiel :
[https://github.com/joanperreyprato/EasySave](https://github.com/joanperreyprato/EasySave)

---

**Dernière mise à jour** : Janvier 2024
**Version** : 1.0
