# 📐 Dossier de Modélisation UML — EasySave

Ce document regroupe les diagrammes UML requis par le cahier des charges et le calendrier des livrables (v1.0 à v3.0).  
Tous les diagrammes sont rédigés au format standard **Mermaid** (rendu direct sur GitHub ou VS Code) et **PlantUML** pour exportation PDF/Word.

---

## 1. Diagramme des Cas d'Utilisation (Use Cases)

```mermaid
flowchart LR
    User(["👤 Utilisateur"])
    SysUser(["💻 Système OS / Processus"])

    subgraph EasySave ["Application EasySave (v1.x -> v3.0)"]
        UC_Create["Créer un travail de sauvegarde (v1: 5 max, v2/v3: illimité)"]
        UC_Run["Exécuter sauvegardes (v1/v2: séquentiel, v3: parallèle)"]
        UC_Control["Contrôler les travaux (Play / Pause / Stop) (v3)"]
        UC_Lang["Changer la langue (FR / EN en temps réel)"]
        UC_Format["Sélectionner format de logs (JSON / XML) (v1.1+)"]
        UC_Crypto["Chiffrer via CryptoSoft (XOR 64-bit) (v2+)"]
        UC_Detect["Mettre en pause si Logiciel Métier actif (v2/v3)"]
        UC_Remote["Centraliser logs sur serveur distant (Docker TCP) (v3)"]
    end

    User --> UC_Create
    User --> UC_Run
    User --> UC_Control
    User --> UC_Lang
    User --> UC_Format

    UC_Run -.->|<<include>>| UC_Crypto
    SysUser --> UC_Detect
    UC_Detect -.->|<<interrompt/suspend>>| UC_Run
    UC_Run -.->|<<optionnel>>| UC_Remote
```

---

## 2. Diagramme de Classes (Architecture & Design Patterns)

Ce diagramme illustre les principaux Design Patterns mis en œuvre :
- **MVVM** (Model-View-ViewModel pour l'interface graphique WPF)
- **Factory** (`LoggerFactory`)
- **Composite Pattern** (`CompositeLogger` combinant JSON, XML et Socket TCP)
- **Singleton** (`LanguageProvider`, `PriorityTransferGuard`)

```mermaid
classDiagram
    %% Core Model
    class BackupJob {
        +string Name
        +string SourceDirectory
        +string TargetDirectory
        +BackupType Type
        +JobStatus Status
    }

    class BackupType {
        <<enumeration>>
        Full
        Differential
    }

    class JobStatus {
        <<enumeration>>
        Idle
        Running
        Paused
        Stopped
        Completed
        Failed
    }

    BackupJob --> BackupType
    BackupJob --> JobStatus

    %% Services & Guards
    class BackupServiceV3 {
        -StateManager _stateManager
        -AppSettings _settings
        -BusinessSoftwareMonitor _monitor
        -Dictionary~string, JobController~ _controllers
        +ExecuteJobsParallel(List~string~ jobNames, ILogger logger)
        +PauseJob(string name)
        +ResumeJob(string name)
        +StopJob(string name)
    }

    class JobController {
        -ManualResetEventSlim _pauseEvent
        -CancellationTokenSource _cts
        +WaitIfPaused()
        +ThrowIfStopped()
        +Pause()
        +Resume()
        +Stop()
    }

    class PriorityTransferGuard {
        <<singleton>>
        -static PriorityTransferGuard _instance
        -int _priorityJobsRunning
        +AcquirePrioritySlot(bool isPriority)
        +ReleasePrioritySlot()
    }

    class LargeFileTransferGuard {
        <<singleton>>
        -SemaphoreSlim _largeFileLock
        +AcquireLargeFileSlot(long fileSize, long thresholdBytes)
        +ReleaseLargeFileSlot()
    }

    BackupServiceV3 --> JobController
    BackupServiceV3 --> PriorityTransferGuard
    BackupServiceV3 --> LargeFileTransferGuard

    %% Logging & Patterns
    class ILogger {
        <<interface>>
        +Log(LogEntry entry)
    }

    class JsonLogger {
        -string _logDir
        +Log(LogEntry entry)
    }

    class XmlLogger {
        -string _logDir
        +Log(LogEntry entry)
    }

    class SocketLogger {
        -string _host
        -int _port
        +Log(LogEntry entry)
    }

    class CompositeLogger {
        -List~ILogger~ _loggers
        +Log(LogEntry entry)
        +AddLogger(ILogger logger)
    }

    class LoggerFactory {
        <<factory>>
        +Create(LogFormat format, string dir) ILogger
        +CreateComposite(IEnumerable~ILogger~ loggers) ILogger
    }

    ILogger <|.. JsonLogger
    ILogger <|.. XmlLogger
    ILogger <|.. SocketLogger
    ILogger <|.. CompositeLogger
    CompositeLogger o-- ILogger : compose
    LoggerFactory ..> ILogger : instancie

    %% MVVM Pattern
    class MainViewModel {
        +ObservableCollection~JobViewModel~ Jobs
        +ICommand RunSelectedJobsCommand
        +ICommand RunAllJobsCommand
        +ICommand PauseJobCommand
        +ICommand ResumeJobCommand
        +ICommand StopJobCommand
        +string Item[string key]
    }

    class JobViewModel {
        +BackupJob Job
        +double ProgressPercentage
        +string StatusText
    }

    MainViewModel o-- JobViewModel
    MainViewModel --> BackupServiceV3
```

---

## 3. Diagramme de Séquence (Sauvegarde Parallèle, Détection Métier & CryptoSoft)

Ce diagramme détaille le cycle d'exécution d'un fichier en v3.0 :

```mermaid
sequenceDiagram
    autonumber
    actor User as Utilisateur
    participant UI as WPF MainViewModel
    participant Svc as BackupServiceV3
    participant GuardP as PriorityGuard
    participant GuardL as LargeFileGuard
    participant Monitor as BusinessSoftwareMonitor
    participant Crypto as CryptoSoft (Mutex)
    participant Log as CompositeLogger

    User->>UI: Clic sur "▶ Lancer sélection"
    UI->>Svc: ExecuteJobsParallel(jobs)
    
    par Pour chaque travail de sauvegarde
        loop Pour chaque fichier du répertoire
            Svc->>Monitor: IsBusinessSoftwareRunning()
            alt Logiciel métier détecté (ex: Calculatrice)
                Monitor-->>Svc: true
                Svc->>UI: Notification Status = "PausedByBusinessSoftware"
                Note over Svc: Attend terminaison du processus métier
            end

            alt Fichier Non Prioritaire
                Svc->>GuardP: WaitIfPriorityFilesPending()
                GuardP-->>Svc: Autorisation accordée
            end

            alt Taille Fichier > Seuil n Ko
                Svc->>GuardL: AcquireLargeFileSlot()
                GuardL-->>Svc: Slot accordé (1 seul gros fichier à la fois)
            end

            opt Extension à chiffrer
                Svc->>Crypto: Exécuter CryptoSoft.exe (source, dest)
                Note over Crypto: Acquisition Global\CryptoSoftMutex
                Crypto-->>Svc: ExitCode > 0 (temps chiffrement ms)
            end

            alt Gros fichier transféré
                Svc->>GuardL: ReleaseLargeFileSlot()
            end

            Svc->>Log: Log(LogEntry)
            par Écriture locale & distante
                Log->>Log: JsonLogger / XmlLogger (disque local)
                Log->>Log: SocketLogger (TCP 8080 Docker)
            end
            
            Svc->>UI: Update Progress (%) & State.json
        end
    end
    Svc-->>UI: Jobs Completed
    UI-->>User: Affichage "✓ Sauvegarde terminée"
```

---

## 4. Diagramme de Déploiement (Infrastructure v3.0)

```mermaid
flowchart TB
    subgraph ClientHost ["Poste Client / Serveur Applicatif"]
        subgraph AppScope ["Processus EasySave (Mono-Instance Mutex)"]
            WPF["EasySave.WPF.exe"]
            Core["EasySave.Core.dll"]
            EasyLog["EasyLog.dll"]
        end
        
        CryptoExe["CryptoSoft.exe (Processus indépendant / Mutex)"]
        LocalLogs[("Fichiers Logs Locaux\n(JSON / XML)")]
        LocalState[("state.json &\nbackup_config.json")]

        WPF --> Core
        Core --> EasyLog
        Core -->|Lancement Process| CryptoExe
        EasyLog -->|Écriture disque| LocalLogs
        Core -->|Persistance| LocalState
    end

    subgraph DockerHost ["Hôte Docker (Centralisation Logs)"]
        subgraph Container ["Conteneur : easysave-logserver"]
            LogSvr["EasySave.LogServer\n(TCP Listener : 8080)"]
            CentralLog[("Fichier Journal Unique\nCentralisé")]
            LogSvr -->|Écriture| CentralLog
        end
    end

    EasyLog -->|Flux TCP Socket / Port 8080| LogSvr
```
