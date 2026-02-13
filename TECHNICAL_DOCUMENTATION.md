# EasySave 2.0 - Documentation Technique

Guide pour les développeurs et contributeurs

---

## Architecture

### Vue d'Ensemble

```
???????????????????????????????????????????????
?           Interface Utilisateur             ?
???????????????????????????????????????????????
?   EasySave.WPF   ?      EasySave.UI         ?
?   (GUI WPF)      ?      (Console CLI)       ?
???????????????????????????????????????????????
         ?                   ?
         ?????????????????????
                    ?
         ?????????????????????????
         ?   EasySave v1.0       ?
         ?   (Bibliothèque Core) ?
         ?                       ?
         ?  - BackupManager      ?
         ?  - BackupJob          ?
         ?  - BackupExecutor     ?
         ?  - StateManager       ?
         ?  - LanguageProvider   ?
         ?????????????????????????
              ?          ?
     ???????????????  ??????????????
     ?   EasyLog   ?  ? CryptoSoft ?
     ?  (Logging)  ?  ?(Encryption)?
     ???????????????  ??????????????
```

---

## Composants Principaux

### 1. EasySave.WPF (Interface Graphique)

**MainWindow.xaml.cs**
- Gère l'interface WPF
- `ObservableCollection<JobViewModel>` pour binding
- Gestion des événements boutons
- Onglet chiffrement
- Changement de langue dynamique

**Fonctionnalités clés** :
```csharp
// Ajouter un travail
BtnAddJob.Click += (_, __) => AddJob();

// Exécuter avec chiffrement optionnel
void StartBackupByIndex(int oneBasedIndex) {
    // Demande à l'utilisateur
    // Execute backup
    // Si chiffrement : chiffre fichiers après copie
}
```

### 2. EasySave.UI (Console)

**Program.cs**
- Mode interactif (menu)
- Mode commande (`create`, `execute`, `list`)
- Parsing des arguments CLI
- Gestion multilingue

**Commandes** :
```csharp
// Parsing indices : "1-3" ou "1,3,5"
int[] indices = CommandLineParser.ParseArguments(tokens);

// Exécution
backupManager.ExecuteBackupJobs(indices);
```

### 3. EasySave v1.0 (Bibliothèque Core)

#### BackupManager
- Création et validation des travaux
- Persistance dans `backup_config.json`
- Orchestration des exécutions

```csharp
public enum BackupCreationError {
    None, InvalidName, SourcePathInvalid, 
    SourcePathNotFound, TargetPathInvalid, 
    TargetPathNotFound, DuplicateName, MaxJobsExceeded
}

public bool CreateBackupJob(string name, string source, 
    string target, BackupType type, out BackupCreationError error)
```

#### BackupJob
- Représente un travail de sauvegarde
- Méthode `Execute()` effectue la copie
- Met à jour l'état en temps réel

```csharp
public void Execute(ILogger logger, StateManager stateManager) {
    // Liste fichiers
    // Copie fichier par fichier
    // Met à jour progression
    // Logs actions
}
```

#### BackupExecutor
- Exécution des sauvegardes Full et Differential
- Calcul de progression
- Gestion des erreurs silencieuses

#### StateManager
- Charge/sauvegarde `state.json`
- Met à jour l'état en temps réel
- Fournit état actuel pour monitoring

```csharp
public class JobState {
    public string Name { get; set; }
    public DateTime Timestamp { get; set; }
    public StateStatus Status { get; set; }
    public int TotalFiles { get; set; }
    public long TotalSize { get; set; }
    public int RemainingFiles { get; set; }
    public long RemainingSize { get; set; }
    public double Progress { get; set; }
}
```

#### LanguageProvider
- Singleton multilingue
- Support FR et EN
- Fallback automatique

```csharp
public static LanguageProvider GetInstance(string language = "en")
public string GetText(string key)
public void SetLanguage(string languageCode)
```

### 4. EasyLog (Système de Logs)

**ILogger Interface**
```csharp
public interface ILogger {
    void LogAction(LogEntry entry);
}
```

**JsonLogger / XmlLogger**
- Logs dans `C:\ProgramData\EasySave\logs\`
- Format JSON ou XML
- Timestamp automatique

**LogEntry**
```csharp
public class LogEntry {
    public string JobName { get; set; }
    public string SourcePath { get; set; }
    public string TargetPath { get; set; }
    public long FileSize { get; set; }
    public long TransferTime { get; set; }
    public long EncryptionTime { get; set; } // -1 si erreur
    public EntryType Type { get; set; } // File ou Summary
}
```

### 5. CryptoSoft (Chiffrement)

**CryptoService**
```csharp
public static class CryptoService {
    // Chiffre fichier avec AES-256-CBC
    public static void EncryptFile(string input, string output, string key)
    
    // Déchiffre fichier
    public static void DecryptFile(string input, string output, string key)
    
    // Chiffre stream, retourne bytes
    public static byte[] Encrypt(Stream inputStream, string key)
    
    // Déchiffre stream, retourne bytes
    public static byte[] Decrypt(Stream inputStream, string key)
}
```

**Format chiffré** :
```
[1 byte: IV length][IV bytes][Ciphertext][32 bytes: HMAC-SHA256]
```

**Algorithme** :
- Clé dérivée : `SHA256(key_text)`
- Chiffrement : AES-256-CBC
- Intégrité : HMAC-SHA256

---

## Flux d'Exécution

### Création d'un Travail (WPF)

```
User clicks "Add job"
  ?
  ?
AddJobWindow dialog
  ?
  ?
User fills form ? OK
  ?
  ?
BackupManager.CreateBackupJob()
  ?
  ??? Validate name (alphanumeric)
  ??? Check source exists
  ??? Check target exists
  ??? Check no duplicate
  ??? Check limit (5 jobs)
  ?
  ?
Add to Jobs list
  ?
  ?
Save to backup_config.json
  ?
  ?
Update UI
```

### Exécution avec Chiffrement (WPF)

```
User clicks "Start selected"
  ?
  ?
For each selected job:
  ?
  ??? Ask user: "Encrypt?"
  ?    ?
  ?    ??? No ? Execute backup normally
  ?    ?
  ?    ??? Yes
  ?         ?
  ?         ??? Check encryption key exists
  ?         ?
  ?         ??? Execute backup (copy files)
  ?         ?
  ?         ??? For each copied file:
  ?              ?
  ?              ??? Check extension in filter list
  ?              ?
  ?              ??? If match:
  ?              ?    ?
  ?              ?    ??? CryptoService.EncryptFile()
  ?              ?    ??? Measure encryption time
  ?              ?    ??? Replace original with encrypted
  ?              ?    ??? Log encryption time
  ?              ?
  ?              ??? Continue with next file
  ?
  ?
Update state to "Completed"
```

### Sauvegarde Différentielle

```
BackupExecutor.ExecuteDifferentialBackup()
  ?
  ??? List all files in source
  ?
  ??? For each file:
  ?    ?
  ?    ??? Check if exists in target
  ?    ?
  ?    ??? If not exists ? Include
  ?    ?
  ?    ??? If exists:
  ?         ?
  ?         ??? Compare modification date
  ?         ??? Compare size
  ?         ?
  ?         ??? If different ? Include
  ?
  ??? Copy only included files
  ?
  ??? Log results
```

---

## Validation des Données

### Nom de Travail
```csharp
Regex.IsMatch(name, @"^[a-zA-Z0-9\s\-_]+$")
name.Length <= 50
```

### Chemins
```csharp
Path.GetFullPath(path) // Valide format
Directory.Exists(path)  // Vérifie existence
```

---

## Gestion des Erreurs

### Stratégie
- **Validation précoce** : Avant persistance
- **Erreurs silencieuses** : Continue malgré erreur fichier individuel
- **Logs** : `transferTime: -1` ou `encryptionTime: -1` indiquent erreur
- **Messages localisés** : Selon langue active

### Codes d'Erreur Création
```csharp
InvalidName             ? "Nom invalide"
SourcePathNotFound      ? "Source introuvable"
TargetPathNotFound      ? "Destination introuvable"
DuplicateName           ? "Nom déjà utilisé"
MaxJobsExceeded         ? "Limite atteinte (5 max)"
```

---

## Patterns Utilisés

### 1. Singleton
```csharp
// LanguageProvider
public static LanguageProvider GetInstance(string language = "en")
```

### 2. Dependency Injection
```csharp
public ConsoleUI(BackupManager manager, ILanguageProvider language)
public BackupManager(StateManager stateManager, ILogger logger = null)
```

### 3. Observer (via WPF)
```csharp
public ObservableCollection<JobViewModel> Jobs { get; }
```

### 4. Strategy
```csharp
ILogger logger = useXml ? new XmlLogger(logDir) : new JsonLogger(logDir);
```

---

## Extensibilité

### Ajouter un Nouveau Type de Sauvegarde

1. Ajouter à enum :
```csharp
public enum BackupType {
    Full = 0,
    Differential = 1,
    Incremental = 2  // Nouveau
}
```

2. Implémenter dans BackupExecutor :
```csharp
public void ExecuteIncrementalBackup(BackupJob job) {
    // Implémentation
}
```

3. Router dans BackupManager :
```csharp
if (type == BackupType.Incremental)
    executor.ExecuteIncrementalBackup(job);
```

### Ajouter une Nouvelle Langue

1. Modifier LanguageProvider :
```csharp
translations["es"] = new Dictionary<string, string> {
    ["window.title"] = "EasySave 2.0",
    // ... autres clés
};
```

2. Ajouter option UI (WPF ou CLI)

---

## Tests

### Tests Unitaires Recommandés

```csharp
// Validation
[Test] void IsValidName_ValidName_ReturnsTrue()
[Test] void IsValidName_InvalidChars_ReturnsFalse()
[Test] void IsValidPath_InvalidPath_ReturnsFalse()

// Création
[Test] void CreateBackupJob_ValidParams_Succeeds()
[Test] void CreateBackupJob_DuplicateName_Fails()

// Exécution
[Test] void ExecuteFullBackup_CopiesAllFiles()
[Test] void ExecuteDifferentialBackup_OnlyModifiedFiles()

// Chiffrement
[Test] void EncryptFile_ValidKey_Succeeds()
[Test] void DecryptFile_CorrectKey_RestoresOriginal()
[Test] void DecryptFile_WrongKey_ThrowsException()
```

---

## Performance

### Optimisations Actuelles
- `File.Copy()` natif (optimisé OS)
- Logs bufferisés
- État sauvegardé seulement après chaque fichier

### Limitations
- Mono-thread (fichiers copiés séquentiellement)
- Pas de compression
- Chiffrement ajoute ~30-50% de temps

### Améliorations Futures
- Multi-threading pour copie parallèle
- Compression optionnelle avant chiffrement
- Cache métadonnées fichiers pour Differential
- Progression UI temps réel (polling state.json)

---

## Configuration Build

### Projets
```
EasySave.WPF         ? .NET 8.0, WPF, Windows
EasySave.UI          ? .NET 8.0, Console, Cross-platform
EasySave v1.0        ? .NET 8.0, Class Library
EasyLog              ? .NET 8.0, Class Library
CryptoSoft           ? .NET 8.0, Class Library
EasySave.Tests       ? xUnit, .NET 8.0
```

### Dépendances
```
EasySave.WPF     ? Référence EasySave v1.0, EasyLog, CryptoSoft
EasySave.UI      ? Référence EasySave v1.0, EasyLog
EasySave v1.0    ? Référence EasyLog
```

---

## Contribution

### Structure de Branche
```
main             ? Production stable
develop          ? Intégration features
feature/*        ? Nouvelles fonctionnalités
bugfix/*         ? Corrections de bugs
```

### Pull Request
1. Fork le projet
2. Créer branche feature
3. Commiter changements
4. Tester (dotnet test)
5. Soumettre PR vers develop

---

## Support

- **Issues GitHub** : [github.com/joanperreyprato/EasySave/issues](https://github.com/joanperreyprato/EasySave/issues)
- **Documentation utilisateur** : [DOCUMENTATION.md](DOCUMENTATION.md)
- **FAQ** : [FAQ.md](FAQ.md)

---

**Version** : 2.0  
**Dernière mise à jour** : Janvier 2025

? Documentation technique complète pour développeurs
