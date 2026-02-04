# EasySave v1.0 - Documentation Technique

##  Guide pour les développeurs

---

## Table des matières

1. [Architecture générale](#architecture-générale)
2. [Description des composants](#description-des-composants)
3. [Flux d'exécution](#flux-dexécution)
4. [Système de traduction](#système-de-traduction)
5. [Gestion d'état](#gestion-détat)
6. [Validation des données](#validation-des-données)
7. [Gestion des erreurs](#gestion-des-erreurs)
8. [Patterns utilisés](#patterns-utilisés)
9. [Extensibilité](#extensibilité)

---

## Architecture générale

### Diagramme UML simplifié

```

                     Program.cs                           
              (Point d'entrée principal)                 

                 
        
                         
       
   ConsoleUI    LanguageProvider
   (UI)         (Traduction)
       
        
   
     BackupManager        
     (Gestion tâches)     
   
        
   
     BackupExecutor       
     (Exécution)          
   
        
   
                          
    
 StateManager      JsonLogger    
 (État)            (Logs)        
    
```

---

## Description des composants

### 1. **Program.cs** - Point d'entrée

**Responsabilités** :
- Initialisation des services (logger, StateManager, BackupManager, LanguageProvider)
- Routage entre mode interactif et mode CLI
- Gestion des arguments en ligne de commande

**Interfaces clés** :
```csharp
var language = new LanguageProvider("en");
var stateManager = new StateManager();
var logger = new JsonLogger("logs");
var backupManager = new BackupManager(stateManager, logger);
var ui = new ConsoleUI(backupManager, language);
```

**Commandes supportées** :
- `list` - Affiche les sauvegardes
- `create <nom> <source> <cible> [full|diff]` - Crée une sauvegarde
- `execute <indices>` - Exécute des sauvegardes

---

### 2. **ConsoleUI.cs** - Interface utilisateur

**Responsabilités** :
- Affichage du menu interactif
- Collecte des entrées utilisateur
- Affichage des erreurs localisés
- Gestion du flux utilisateur

**Méthodes principales** :

```csharp
public void DisplayMainMenu()        // Affiche le menu principal
public void CreateBackupJobMenu()    // Crée une sauvegarde interactive
public void ExecuteBackupMenu()      // Exécute une sauvegarde interactive
public void DisplayBackupJobs()      // Liste les sauvegardes
```

**Dépendances** :
- `BackupManager` - Gestion des tâches
- `ILanguageProvider` - Traduction

---

### 3. **BackupManager.cs** - Gestionnaire des tâches

**Responsabilités** :
- Validation des paramètres de création
- Gestion de la liste des tâches
- Persistance de la configuration (JSON)
- Orchestration de l'exécution

**Enum d'erreurs** :
```csharp
public enum BackupCreationError
{
    None,
    InvalidName,
    SourcePathInvalid,
    SourcePathNotFound,
    TargetPathInvalid,
    TargetPathNotFound,
    DuplicateName,
    MaxJobsExceeded
}
```

**Validation** :
- Nom : alphanumérique, espaces, tirets, underscores (max 50 caractères)
- Chemins : format absolu valide, répertoires doivent exister
- Unicité : pas de doublon de nom
- Limite : maximum 5 sauvegardes

---

### 4. **BackupExecutor.cs** - Exécuteur des sauvegardes

**Responsabilités** :
- Exécution des sauvegardes complètes
- Exécution des sauvegardes différentielles
- Mise à jour d'état en temps réel
- Calcul de progression

**Algorithme de sauvegarde complète** :
```
1. Lister tous les fichiers du source (récursif)
2. Calculer taille totale et nombre fichiers
3. Pour chaque fichier :
   a. Créer les répertoires destination si besoin
   b. Copier le fichier
   c. Mettre à jour progression et état
4. Marquer complétée, sauvegarder logs
```

**Algorithme de sauvegarde différentielle** :
```
1. Lister tous les fichiers du source
2. Pour chaque fichier :
   a. Vérifier si existe en cible
   b. Si n'existe pas : inclure
   c. Si existe : comparer date/taille
      - Si différent : inclure
3. Copier uniquement les fichiers inclus
4. Mettre à jour progression
```

---

### 5. **StateManager.cs** - Gestion d'état

**Responsabilités** :
- Chargement/sauvegarde de l'état JSON
- Mise à jour de l'état des tâches
- Récupération de l'état actuel

**Persistance** :
```csharp
stateFilePath = "%ProgramData%\EasySave\state.json"
```

**Méthodes** :
```csharp
void UpdateJobState(string jobName, JobState state)
JobState GetJobState(string jobName)
void SaveState()
void LoadState()
```

---

### 6. **JobState.cs** - État d'une tâche

**Propriétés** :
```csharp
public string Name              // Nom de la tâche
public DateTime Timestamp       // Horodatage
public StateStatus Status       // État (Inactive, Active, Completed, Error)
public int TotalFiles          // Nombre total fichiers
public long TotalSize          // Taille totale (octets)
public int RemainingFiles      // Fichiers restants
public long RemainingSize      // Taille restante (octets)
public string CurrentSourceFile // Fichier source courant
public string CurrentTargetFile // Fichier destination courant
public double Progress         // % progression (0-100)
```

**Calcul de progression** :
```csharp
Progress = (TotalFiles - RemainingFiles) / TotalFiles * 100
// ou par taille si TotalFiles = 0
Progress = (TotalSize - RemainingSize) / TotalSize * 100
```

---

### 7. **LanguageProvider.cs** - Traduction

**Responsabilités** :
- Chargement des traductions (fichier JSON ou defaults)
- Récupération de texte localisé
- Changement de langue

**Structure de traduction** :
```json
{
  "en": {
    "menu.create": "Create Backup",
    "menu.execute": "Execute Backup",
    ...
  },
  "fr": {
    "menu.create": "Créer une sauvegarde",
    "menu.execute": "Exécuter la sauvegarde",
    ...
  }
}
```

**Fichier de configuration** :
```
Directory: %ProgramData%\EasySave\
File: languages.json (optionnel)
```

**Fallback** :
- Si clé non trouvée  fallback EN
- Si langue non existante  fallback EN

---

### 8. **CommandLineParser.cs** - Analyse CLI

**Responsabilités** :
- Parsing des arguments en ligne de commande
- Support des plages (1-3) et listes (0,2,4)
- Identification des commandes valides

**Syntaxe supportée** :
```bash
# Plage
execute 1-5         [1, 2, 3, 4, 5]

# Liste
execute 0,2,4       [0, 2, 4]

# Combiné
execute 0-2,5,7     [0, 1, 2, 5, 7]
```

---

## Flux d'exécution

### 1. Mode interactif

```
START
  
Initialiser services
  
Afficher menu principal
  

  Boucle interactive         
     
   Attendre choix user     
     
                            
      
                           
  (1)          (2)        (3) 
                           
                           
  Créer       Exécuter   Lister
                           
      
                            
      Sauvegarder configuration
                              
      Retour menu             
                              

          
       (4) EXIT
          
        END
```

### 2. Création de sauvegarde

```
CreateBackupJobMenu()
  
Boucle :
   Demander nom, source, cible, type
   Valider avec BackupManager.CreateBackupJob()
     IsValidName()
     Vérifier duplicata
     Vérifier limite (5)
     IsValidPath(source)
     Vérifier source existe
     IsValidPath(cible)
     Vérifier cible existe
     Créer et ajouter à liste
  
   Si succès  SaveConfiguration()
               Retour
  
   Si erreur  DisplayCreationError()
                Demander retry
```

### 3. Exécution de sauvegarde

```
ExecuteBackupJob(index)
  
Vérifier index valide
  
Récupérer job et type
   Full  ExecuteFullBackup()
   Diff  ExecuteDifferentialBackup()
  
  Chacun :
   Lister fichiers éligibles
   Calculer taille totale
   Créer état initial
   Pour chaque fichier :
     Copier
     Mettre à jour progression
     Sauvegarder état
   Marquer complétée
   Logger résultat
```

---

## Système de traduction

### Implémentation

Le système utilise `ILanguageProvider` pour abstraction :

```csharp
public interface ILanguageProvider
{
    string GetText(string key);
    void SetLanguage(string languageCode);
}
```

### Utilisation

```csharp
// Dans ConsoleUI
Console.WriteLine(languageProvider.GetText("menu.create"));

// Dans Program.cs
Console.WriteLine(language.GetText("backup.create.error.invalid_name"));
```

### Clés de traduction

Structure hiérarchique recommandée :
```
menu.*               Menu principal
backup.create.*      Création sauvegarde
backup.execute.*     Exécution sauvegarde
backup.list.*        Listing sauvegardes
error.*              Erreurs générales
```

### Ajout d'une nouvelle langue

1. Ajouter dans `LanguageProvider.cs` :
```csharp
translations["es"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["menu.create"] = "Crear Copia de Seguridad",
    ...
};
```

2. Utiliser en changeant `Program.cs` :
```csharp
var language = new LanguageProvider("es");
```

---

## Gestion d'état

### Cycle de vie d'une tâche

```
Inactive (création)
   
Active (exécution en cours)
    État mis à jour après chaque fichier
    Progression augmente
    Temps réel pour monitoring
   
Completed (succès)
    Progress = 100%
    RemainingFiles = 0
    RemainingSize = 0
   
   OU
   
Error (erreur)
    Pas de fin
```

### Sérialisation JSON

```json
{
  "name": "MonBackup",
  "timestamp": "2024-01-15T10:30:00Z",
  "status": "Completed",
  "totalFiles": 100,
  "totalSize": 1073741824,
  "remainingFiles": 0,
  "remainingSize": 0,
  "currentSourceFile": "",
  "currentTargetFile": "",
  "progress": 100.0
}
```

---

## Validation des données

### 1. Validation du nom

```csharp
private bool IsValidName(string name)
{
    // Vérifier non-vide
    if (string.IsNullOrWhiteSpace(name)) return false;
    
    // Regex : alphanumérique, espace, tiret, underscore
    if (!Regex.IsMatch(name, @"^[a-zA-Z0-9\s\-_]+$")) return false;
    
    // Longueur : max 50
    if (name.Length > 50) return false;
    
    return true;
}
```

### 2. Validation de chemin

```csharp
private bool IsValidPath(string path)
{
    if (string.IsNullOrWhiteSpace(path)) return false;
    
    try
    {
        var fullPath = Path.GetFullPath(path);
        return !string.IsNullOrEmpty(fullPath);
    }
    catch
    {
        return false;
    }
}
```

### 3. Vérification d'existence

```csharp
// Source
if (!Directory.Exists(source))
    error = BackupCreationError.SourcePathNotFound;

// Cible
if (!Directory.Exists(target))
    error = BackupCreationError.TargetPathNotFound;
```

---

## Gestion des erreurs

### Stratégie générale

- **Validation précoce** : Erreurs détectées avant persistance
- **Messages localisés** : Erreurs en langue actuelle
- **Recovery** : Possibilité de retry après erreur
- **Silent failures** : Erreurs I/O silencieuses (continue)

### Codes d'erreur création

| Code | Cause | Action |
|------|-------|--------|
| `InvalidName` | Format invalide | Re-prompt |
| `DuplicateName` | Nom existe | Re-prompt |
| `SourcePathInvalid` | Format invalide | Re-prompt |
| `SourcePathNotFound` | Répertoire absent | Re-prompt |
| `TargetPathInvalid` | Format invalide | Re-prompt |
| `TargetPathNotFound` | Répertoire absent | Re-prompt |
| `MaxJobsExceeded` | Limite atteinte | Erreur fatale |

### Gestion pendant exécution

```csharp
// Erreurs individuelles ignorées
try
{
    File.Copy(source, destination, true);
}
catch
{
    // Continuer avec fichier suivant
}

// Erreurs d'état gérées
try
{
    logger.LogAction(log);
}
catch
{
    // Silencieuse
}
```

---

## Patterns utilisés

### 1. **Dependency Injection**

```csharp
public ConsoleUI(BackupManager manager, ILanguageProvider language)
{
    this.backupManager = manager  throw new ArgumentNullException(nameof(manager));
    this.languageProvider = language  throw new ArgumentNullException(nameof(language));
}
```

### 2. **Interface Segregation**

```csharp
public interface ILanguageProvider
{
    string GetText(string key);
    void SetLanguage(string languageCode);
}
```

### 3. **Enum pour états**

```csharp
public enum BackupCreationError { ... }
public enum StateStatus { ... }
public enum BackupType { ... }
```

### 4. **Factory Pattern (implicit)**

```csharp
var job = new BackupJob(name, source, target, type);
var executor = new BackupExecutor(logger, stateManager);
```

---

## Extensibilité

### Ajouter un nouveau type de sauvegarde

1. Ajouter à `BackupType.cs` :
```csharp
public enum BackupType
{
    Full,
    Differential,
    Incremental  //  Nouveau
}
```

2. Implémenter dans `BackupExecutor.cs` :
```csharp
public void ExecuteIncrementalBackup(BackupJob job)
{
    // Implémentation
}
```

3. Ajouter routage dans `BackupManager.cs` :
```csharp
if (job.GetBackupType() == BackupType.Incremental)
    executor.ExecuteIncrementalBackup(job);
```

### Ajouter un nouveau format de configuration

1. Créer interface :
```csharp
public interface IConfigurationStorage
{
    void Save(List<BackupJob> jobs);
    List<BackupJob> Load();
}
```

2. Implémenter (XML, DB, etc.) :
```csharp
public class XmlConfigurationStorage : IConfigurationStorage
{
    // Implémentation XML
}
```

3. Utiliser dans `BackupManager` :
```csharp
private IConfigurationStorage storage;
storage.Save(backupJobs);
```

### Ajouter un nouveau logger

1. Implémenter `ILogger` :
```csharp
public class DatabaseLogger : ILogger
{
    public void LogAction(LogEntry entry) { ... }
}
```

2. Utiliser dans `Program.cs` :
```csharp
var logger = new DatabaseLogger();
```

---

## Performance

### Optimisations actuelles

- **Cached JsonSerializerOptions** : Réutilisation objet options
- **Streaming fichiers** : `File.Copy()` optimisée OS
- **Calcul progressif** : Progression mise à jour fichier par fichier

### Points d'amélioration futurs

- Parallélisation des copies (limitation I/O)
- Compression avant transfert
- Synchronisation delta avancée
- Cache de métadonnées fichiers

---

## Tests recommandés

```csharp
// Test création sauvegarde
[Test]
public void CreateBackupJob_WithValidParams_Succeeds()

// Test validation nom
[Test]
public void IsValidName_InvalidChars_ReturnsFalse()

// Test exécution complète
[Test]
public void ExecuteFullBackup_CopiesAllFiles()

// Test sauvegarde différentielle
[Test]
public void ExecuteDifferentialBackup_OnlyNewFiles()

// Test traduction
[Test]
public void GetText_WithValidKey_ReturnsTranslation()

// Test persistance état
[Test]
public void SaveState_WritesJsonFile()
```

---

## Conclusion

EasySave est architecturé de façon modulaire et extensible, permettant ajouts et modifications sans impacter code existant. Le respect des principes SOLID facilite maintenance et évolution futures.

**Dernière mise à jour** : Janvier 2024
**Version** : 1.0
