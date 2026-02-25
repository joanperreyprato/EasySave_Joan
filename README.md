# EasySave v3.0 - Documentation (Français)

**Version :** 3.0  
**Framework :** .NET 8  
**Langage :** C#
**Dernière mise à jour :** 28/02/2026

---

## Table des Matières

1. [Vue d'ensemble](#1-vue-densemble)
2. [Architecture](#2-architecture)
3. [Fonctionnalités](#3-fonctionnalités)
4. [Installation et Configuration](#4-installation-et-configuration)
5. [Interfaces Utilisateur](#5-interfaces-utilisateur)
6. [Configuration](#6-configuration)
7. [Fonctionnalités Avancées](#7-fonctionnalités-avancées)
8. [Journalisation Distribuée](#8-journalisation-distribuée)
9. [Dépannage](#9-dépannage)
10. [Développement](#10-développement)

---

## 1. Vue d'ensemble

### Qu'est-ce qu'EasySave ?

EasySave est une application professionnelle de sauvegarde conçue pour les petites et moyennes entreprises. Elle fournit des solutions de sauvegarde fiables, rapides et sécurisées avec des fonctionnalités avancées incluant :

- **Sauvegarde Complète et Différentielle** : Choisissez entre des copies complètes ou des modifications incrémentielles
- **Chiffrement de Fichiers** : Sécurisez vos données sensibles avec le chiffrement AES-256
- **Traitement Prioritaire** : Garantit que les fichiers critiques sont sauvegardés en premier
- **Exécution Parallèle** : Exécutez plusieurs tâches de sauvegarde simultanément
- **Surveillance en Temps Réel** : Mettez en pause, reprenez et arrêtez les sauvegardes à la volée
- **Détection de Logiciels Métier** : Évite les conflits avec les applications métier en cours d'exécution
- **Journalisation Distribuée** : Collection centralisée des logs depuis plusieurs clients
- **Multi-Interface** : Interfaces en ligne de commande (CLI) et graphique (WPF)

### Avantages Clés

- ✅ **Protection Instance Unique** : Une seule instance peut s'exécuter à la fois
- ✅ **Maximum 5 Tâches de Sauvegarde** : Optimisé pour les besoins des PME
- ✅ **Support Multi-Langue** : Anglais et Français
- ✅ **Architecture Extensible** : Conception modulaire pour une personnalisation facile
- ✅ **Journalisation Professionnelle** : Formats JSON et XML avec rotation quotidienne
- ✅ **Support Docker** : Serveur de logs conteneurisé pour un déploiement facile

---

## 2. Architecture

### Structure du Projet

EasySave est organisé en plusieurs projets pour la séparation des préoccupations :

```
EasySave/
├── EasySave.Core/           # Logique métier principale
│   ├── BackupManager.cs     # Gestion des tâches
│   ├── BackupExecutor.cs    # Moteur d'exécution de sauvegarde
│   ├── JobController.cs     # Contrôle Pause/Reprendre/Arrêter
│   ├── StateManager.cs      # Persistance de l'état des tâches
│   ├── LanguageProvider.cs  # Internationalisation
│   └── BusinessSoftwareMonitor.cs
│
├── EasySave.CLI/            # Interface en ligne de commande
│   ├── Program.cs           # Point d'entrée CLI
│   └── ConsoleUI.cs         # Menus console
│
├── EasySave.WPF/            # Interface graphique
│   ├── MainWindow.xaml      # Interface principale
│   ├── AddJobWindow.xaml    # Dialogue de création de tâche
│   └── JobViewModel.cs      # Modèle de vue MVVM
│
├── EasyLog/                 # Bibliothèque de journalisation
│   ├── JsonLogger.cs        # Écriture de logs JSON
│   ├── XmlLogger.cs         # Écriture de logs XML
│   ├── SocketLogger.cs      # Journalisation distante
│   └── CompositeLogger.cs   # Journalisation multi-cibles
│
├── CryptoSoft/              # Bibliothèque de chiffrement
│   └── CryptoService.cs     # Chiffrement AES-256
│
└── EasySave.LogServer/      # Serveur de logs centralisé
    ├── Program.cs           # Point d'entrée serveur
    ├── LogServerService.cs  # Récepteur de logs TCP
    └── Dockerfile           # Image conteneur
```

### Composants Principaux

#### BackupManager
- Gère la création, suppression et exécution des tâches de sauvegarde
- Valide la configuration des tâches (nom, chemins, types)
- Applique les règles métier (max 5 tâches, noms uniques)
- Coordonne avec StateManager pour la persistance

#### BackupExecutor
- Exécute les sauvegardes complètes et différentielles
- Gère la synchronisation des fichiers volumineux (basée sur sémaphore)
- Traite d'abord les extensions de fichiers prioritaires
- S'intègre avec CryptoSoft pour le chiffrement
- Supporte les opérations de pause, reprise et annulation

#### JobController
- Fournit un contrôle en temps réel sur les tâches en cours d'exécution
- Gère les jetons d'annulation et les événements de pause
- Gestion concurrente thread-safe des tâches

#### StateManager
- Persiste les états des tâches dans des fichiers JSON
- Suit la progression, le statut et les statistiques
- Fournit des mises à jour d'état en temps réel

#### LanguageProvider
- Pattern Singleton pour la gestion des langues
- Supporte Anglais et Français
- Charge les traductions depuis `languages.json`
- Repli sur traductions intégrées

---

## 3. Fonctionnalités

### 3.1 Types de Sauvegarde

#### Sauvegarde Complète
Copie tous les fichiers de la source vers la destination, écrasant les fichiers existants.

```csharp
BackupType.Full
```

**Cas d'usage** : Sauvegarde initiale, archives mensuelles, snapshots de récupération après sinistre.

#### Sauvegarde Différentielle
Copie uniquement les fichiers nouveaux ou modifiés depuis la dernière sauvegarde complète.

```csharp
BackupType.Differential
```

**Cas d'usage** : Sauvegardes quotidiennes, mises à jour incrémentielles, scénarios sensibles à la bande passante.

### 3.2 Chiffrement de Fichiers

EasySave s'intègre avec la bibliothèque CryptoSoft pour chiffrer les fichiers pendant la sauvegarde.

**Algorithme** : AES-256-CBC avec vérification d'intégrité HMAC-SHA256  
**Dérivation de Clé** : SHA-256 du mot de passe fourni  
**Format** : `[Longueur IV][IV][Texte Chiffré][HMAC]`

**Configuration** :
- Définir les extensions à chiffrer dans `backup_config.json`
- Fournir la clé de chiffrement via l'interface ou ligne de commande
- Les fichiers sont chiffrés à la volée pendant la sauvegarde

**Exemple de Configuration** :
```json
{
  "encryptExtensions": [".docx", ".xlsx", ".pdf", ".txt"],
  "businessSoftwareNames": ["excel", "word", "outlook"]
}
```

### 3.3 Extensions de Fichiers Prioritaires

Les fichiers avec extensions prioritaires sont sauvegardés en premier, garantissant que les données critiques sont enregistrées avant les fichiers moins importants.

**Configuration** :
```json
{
  "priorityExtensions": [".docx", ".xlsx", ".pdf"]
}
```

**Comportement** :
1. Les fichiers prioritaires de toutes les tâches sont traités en premier
2. Les fichiers non prioritaires attendent que tous les fichiers prioritaires soient terminés
3. Coordination globale empêche l'inversion de priorité

### 3.4 Gestion des Fichiers Volumineux

Les fichiers volumineux sont transférés séquentiellement pour éviter de surcharger les ressources système.

**Configuration** :
- Définir `largeFileThresholdKb` dans le constructeur BackupManager
- Mettre à `0` pour désactiver (transfert parallèle de tous les fichiers)
- Recommandé : `10240` (10 Mo)

**Comportement** :
- Les fichiers plus grands que le seuil acquièrent un sémaphore global
- Un seul fichier volumineux est transféré à la fois sur toutes les tâches
- Les fichiers plus petits continuent en parallèle

### 3.5 Surveillance des Logiciels Métier

Empêche les conflits de sauvegarde en détectant les applications métier en cours d'exécution.

**Configuration** :
```json
{
  "businessSoftwareNames": ["excel", "word", "outlook", "quickbooks"]
}
```

**Comportement** :
- Surveille les processus en cours avant de démarrer la sauvegarde
- Bloque la sauvegarde si un processus surveillé est détecté
- Enregistre l'erreur avec les noms des processus

### 3.6 Contrôle des Tâches en Temps Réel

Contrôlez les sauvegardes en cours sans redémarrer l'application.

**Opérations** :
- **Pause** : Suspend la sauvegarde après la fin du fichier actuel
- **Reprendre** : Continue une sauvegarde en pause
- **Arrêter** : Annule immédiatement la sauvegarde (définit le statut sur Inactif)

**API** :
```csharp
jobController.PauseJob("jobName");
jobController.ResumeJob("jobName");
jobController.StopJob("jobName");
```

### 3.7 Exécution Parallèle

Exécutez plusieurs tâches de sauvegarde simultanément pour maximiser le débit.

**Interface WPF** :
- Le bouton "Démarrer Tout" exécute toutes les tâches en parallèle
- Chaque tâche s'exécute dans une Task séparée
- Mises à jour de progression en temps réel

**Interface CLI** :
- Ligne de commande : `easysave execute 1 2 3`
- Exécution séquentielle par défaut

---

## 4. Installation et Configuration

### 4.1 Prérequis Système

- **Système d'Exploitation** : Windows 10/11, Windows Server 2019+
- **Runtime .NET** : .NET 8.0 ou supérieur
- **Mémoire** : 512 Mo minimum, 2 Go recommandé
- **Espace Disque** : 100 Mo pour l'application, variable pour les logs

### 4.2 Compilation depuis les Sources

#### Prérequis
```bash
# Installer .NET 8 SDK
winget install Microsoft.DotNet.SDK.8

# Cloner le dépôt
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
```

#### Compiler Tous les Projets
```bash
dotnet build EasySave.sln --configuration Release
```

#### Compiler des Projets Spécifiques
```bash
# Version CLI
dotnet build EasySave.CLI/EasySave.CLI.csproj -c Release

# Version WPF
dotnet build EasySave.WPF/EasySave.WPF.csproj -c Release

# Serveur de Logs
dotnet build EasySave.LogServer/EasySave.LogServer.csproj -c Release
```

### 4.3 Installation

#### Installation CLI
```bash
# Publier un exécutable autonome
dotnet publish EasySave.CLI/EasySave.CLI.csproj -c Release -r win-x64 --self-contained true

# Copier dans le répertoire d'installation
xcopy /E /I EasySave.CLI\bin\Release\net8.0\win-x64\publish C:\EasySave
```

#### Installation WPF
```bash
# Publier un exécutable autonome
dotnet publish EasySave.WPF/EasySave.WPF.csproj -c Release -r win-x64 --self-contained true

# Copier dans le répertoire d'installation
xcopy /E /I EasySave.WPF\bin\Release\net8.0-windows\win-x64\publish C:\EasySave
```

### 4.4 Configuration Initiale

1. **Lancer l'application** (CLI ou WPF)
2. **Fichiers de configuration** sont créés automatiquement dans :
   - `%ProgramData%\EasySave\backup_config.json`
   - `%ProgramData%\EasySave\language.json`
   - `%ProgramData%\EasySave\state.json`
3. **Répertoire de logs** est créé à :
   - `%ProgramData%\EasySave\logs\`

---

## 5. Interfaces Utilisateur

### 5.1 Interface en Ligne de Commande (CLI)

#### Mode Interactif

Lancer sans arguments :
```bash
EasySave.CLI.exe
```

**Menu Principal** :
```
=== EasySave ===
1. Créer une tâche de sauvegarde
2. Exécuter une sauvegarde
3. Lister les tâches de sauvegarde
4. Modifier une tâche de sauvegarde
5. Changer de langue
6. Quitter
```

#### Mode Ligne de Commande

**Lister toutes les tâches** :
```bash
EasySave.CLI.exe list
```

**Créer une tâche** :
```bash
EasySave.CLI.exe create "MaSauvegarde" "C:\Source" "D:\Backup" full
EasySave.CLI.exe create "MaSauvegarde" "C:\Source" "D:\Backup" diff
```

**Exécuter des tâches** :
```bash
# Exécuter la tâche 1
EasySave.CLI.exe execute 1

# Exécuter les tâches 1, 2 et 3
EasySave.CLI.exe execute 1 2 3

# Alternative : exécution directe
EasySave.CLI.exe 1-3
EasySave.CLI.exe 1,2,3
```

**Spécifier le répertoire de logs** :
```bash
EasySave.CLI.exe --logdir "C:\MesLogs" list
EasySave.CLI.exe -l=C:\MesLogs execute 1
```

### 5.2 Interface Graphique WPF

#### Fonctionnalités de la Fenêtre Principale

**Liste des Tâches** :
- Voir toutes les tâches de sauvegarde configurées
- Barres de progression en temps réel
- Indicateurs de statut (Arrêté, Actif, En Pause, Terminé, Erreur)

**Boutons de Contrôle** :
- **Ajouter Tâche** : Créer une nouvelle tâche de sauvegarde
- **Démarrer Tout** : Exécuter toutes les tâches en parallèle
- **Arrêter Tout** : Annuler toutes les tâches en cours
- **Mettre Tout en Pause** : Mettre en pause toutes les tâches actives
- **Démarrer Sélection** : Exécuter la tâche sélectionnée
- **Arrêter Sélection** : Annuler la tâche sélectionnée
- **Pause Sélection** : Mettre en pause la tâche sélectionnée
- **Reprendre Sélection** : Reprendre la tâche en pause
- **Supprimer Sélection** : Retirer la tâche de la configuration

**Paramètres** :
- **Langue** : Anglais / Français
- **Format de Log** : JSON / XML
- **Seuil Fichiers Volumineux** : Valeur en Ko (0 = désactivé)
- **Clé de Chiffrement** : Mot de passe pour le chiffrement des fichiers

#### Ajouter une Tâche de Sauvegarde

1. Cliquer sur "**Ajouter Tâche**"
2. Entrer les détails de la tâche :
   - **Nom** : Alphanumérique, max 50 caractères
   - **Source** : Chemin de répertoire existant
   - **Destination** : Chemin de répertoire existant
   - **Type** : Complète ou Différentielle
3. Cliquer sur "**Créer**"

#### Exécuter une Sauvegarde

1. Sélectionner la/les tâche(s) dans la liste
2. Cliquer sur "**Démarrer Sélection**" ou "**Démarrer Tout**"
3. Surveiller la progression en temps réel
4. Utiliser les boutons de contrôle pour mettre en pause/reprendre/arrêter selon les besoins

---

## 6. Configuration

### 6.1 Fichier de Configuration des Sauvegardes

**Emplacement** : `%ProgramData%\EasySave\backup_config.json`

**Exemple** :
```json
{
  "backupJobs": [
    {
      "name": "DocumentsBackup",
      "sourcePath": "C:\\Users\\User\\Documents",
      "targetPath": "D:\\Backups\\Documents",
      "type": "Full",
      "logFormat": "Json"
    }
  ],
  "encryptExtensions": [".docx", ".xlsx", ".pdf", ".txt"],
  "businessSoftwareNames": ["excel", "word", "outlook"],
  "priorityExtensions": [".docx", ".xlsx"]
}
```

**Champs** :
- `backupJobs` : Tableau de définitions de tâches de sauvegarde
- `encryptExtensions` : Extensions de fichiers à chiffrer pendant la sauvegarde
- `businessSoftwareNames` : Noms de processus à surveiller (sans .exe)
- `priorityExtensions` : Extensions à traiter en premier

### 6.2 Paramètres d'Application

**Emplacement** : `EasySave.WPF\appsettings.json` ou `EasySave.CLI\appsettings.json`

**Exemple** :
```json
{
  "RemoteLogging": {
    "Enabled": false,
    "ServerHost": "192.168.1.100",
    "ServerPort": 5555,
    "EnableLocalLogging": true
  },
  "Logging": {
    "LogDirectory": "./logs",
    "LogFormat": "Json"
  }
}
```

**Champs** :
- `RemoteLogging.Enabled` : Activer le serveur de logs centralisé
- `RemoteLogging.ServerHost` : IP ou nom d'hôte du serveur de logs
- `RemoteLogging.ServerPort` : Port TCP (par défaut : 5555)
- `RemoteLogging.EnableLocalLogging` : Conserver les logs locaux quand le distant est activé
- `Logging.LogDirectory` : Chemin du répertoire de logs local
- `Logging.LogFormat` : "Json" ou "Xml"

### 6.3 Configuration de Langue

**Emplacement** : `%ProgramData%\EasySave\language.json`

**Contenu** :
```json
{
  "currentLanguage": "fr"
}
```

Changer à `"en"` pour l'anglais.

### 6.4 Fichier d'État

**Emplacement** : `%ProgramData%\EasySave\state.json`

Contient l'état d'exécution pour toutes les tâches de sauvegarde. Géré automatiquement par StateManager.

**Exemple** :
```json
{
  "DocumentsBackup": {
    "name": "DocumentsBackup",
    "status": "Completed",
    "totalFiles": 150,
    "totalSize": 52428800,
    "remainingFiles": 0,
    "remainingSize": 0,
    "progress": 100.0,
    "currentSourceFile": "rapport.docx",
    "currentTargetFile": "D:\\Backups\\Documents\\rapport.docx",
    "timestamp": "2024-01-15T10:30:45Z"
  }
}
```

---

## 7. Fonctionnalités Avancées

### 7.1 Chiffrement de Fichiers

#### Activer le Chiffrement

1. Ajouter les extensions à `encryptExtensions` dans `backup_config.json`
2. Définir la clé de chiffrement dans l'interface (WPF) ou via invite (CLI)
3. Lancer la sauvegarde - les fichiers correspondants sont chiffrés automatiquement

#### Détails du Chiffrement

- **Algorithme** : AES-256-CBC
- **Intégrité** : HMAC-SHA256
- **Dérivation de Clé** : SHA-256(mot de passe)
- **IV** : Aléatoire, stocké avec le texte chiffré

#### Déchiffrer les Fichiers

Utiliser la bibliothèque CryptoSoft ou la fonction de déchiffrement WPF :

```csharp
CryptoService.DecryptFile(inputPath, outputPath, password);
```

**Dialogue de Déchiffrement WPF** :
1. Cliquer sur "**Déchiffrer Fichier**" (si disponible)
2. Sélectionner le fichier chiffré
3. Entrer le mot de passe
4. Choisir l'emplacement de sortie

### 7.2 Sauvegarde Différentielle

Les sauvegardes différentielles comparent les dates de modification des fichiers :

```csharp
// Un fichier est éligible pour la sauvegarde différentielle si :
// - Le fichier n'existe pas dans la cible, OU
// - Le fichier source est plus récent que le fichier cible

DateTime sourceTime = File.GetLastWriteTimeUtc(sourceFile);
DateTime targetTime = File.GetLastWriteTimeUtc(targetFile);

if (!File.Exists(targetFile) || sourceTime > targetTime)
{
    // Copier le fichier
}
```

**Avantages** :
- Plus rapide qu'une sauvegarde complète
- Moins d'utilisation de bande passante
- Ensembles de sauvegarde plus petits

**Considérations** :
- Nécessite une sauvegarde complète comme référence
- Ne détecte pas les suppressions
- Plusieurs différentielles peuvent s'accumuler

### 7.3 Traitement des Fichiers Prioritaires

Les fichiers prioritaires sont traités avant les fichiers non prioritaires globalement sur toutes les tâches.

**Implémentation** :
```csharp
// Compteur global suit les fichiers prioritaires en attente
private static int PendingPriorityFilesCount = 0;

// Les fichiers prioritaires incrémentent le compteur
lock (PriorityLock)
{
    PendingPriorityFilesCount += priorityFiles.Count;
}

// Les fichiers non prioritaires attendent que le compteur atteigne zéro
while (PendingPriorityFilesCount > 0)
{
    Thread.Sleep(100);
}
```

**Cas d'Usage** :
- Assurer que les documents critiques sont sauvegardés en premier
- Fichiers de base de données avant les fichiers de logs
- Fichiers de configuration avant les fichiers médias

### 7.4 Synchronisation des Fichiers Volumineux

Empêche plusieurs fichiers volumineux de se transférer simultanément.

**Configuration** :
```csharp
BackupManager manager = new BackupManager(
    stateManager, 
    logger, 
    configPath: null, 
    largeFileThresholdKb: 10240 // 10 Mo
);
```

**Comportement** :
- Les fichiers > seuil acquièrent `LargeFileSemaphore`
- Un seul fichier volumineux se transfère globalement
- Les petits fichiers continuent en parallèle

### 7.5 Surveillance des Logiciels Métier

Empêche les conflits de sauvegarde avec les fichiers ouverts.

**Configuration** :
```json
{
  "businessSoftwareNames": ["excel", "word", "outlook"]
}
```

**Logique de Surveillance** :
```csharp
if (businessMonitor.IsBusinessSoftwareRunning())
{
    Console.WriteLine("Sauvegarde bloquée : Logiciel métier en cours d'exécution");
    state.Status = StateStatus.Error;
    return;
}
```

**Détection de Processus** :
- Utilise `Process.GetProcesses()` pour énumérer
- Compare les noms de processus (insensible à la casse)
- Retire le suffixe ".exe" pour la comparaison

---

## 8. Journalisation Distribuée

### 8.1 Vue d'Ensemble

EasySave supporte la journalisation centralisée via socket TCP vers un serveur de logs dédié.

**Architecture** :
```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│ EasySave CLI │────────▶│ Serveur Logs │◀────────│ EasySave WPF │
│  Client 1    │  TCP    │   (Docker)   │  TCP    │  Client 2    │
└──────────────┘ :5555   └──────────────┘ :5555   └──────────────┘
       │                         │
       │                         │
       ▼                         ▼
  Logs Locaux             Logs Centralisés
```

### 8.2 Configuration du Serveur de Logs

#### Déploiement Docker (Recommandé)

**Prérequis** :
- Docker Desktop installé

**Avec Docker Compose** :
```bash
# À la racine du dépôt
docker-compose up -d
```

**Build Docker Manuel** :
```bash
# Construire l'image
docker build -f EasySave.LogServer/Dockerfile -t easysave-logserver:latest .

# Exécuter le conteneur
docker run -d \
  --name easysave-logserver \
  -p 5555:5555 \
  -v $(pwd)/logs:/app/logs \
  -e LOG_SERVER_PORT=5555 \
  -e LOG_SERVER_DIRECTORY=/app/logs \
  easysave-logserver:latest
```

#### Déploiement Natif

```bash
# Compiler le serveur
dotnet build EasySave.LogServer/EasySave.LogServer.csproj -c Release

# Exécuter le serveur
cd EasySave.LogServer/bin/Release/net8.0
EasySave.LogServer.exe
```

**Variables d'Environnement** :
- `LOG_SERVER_PORT` : Port TCP (par défaut : 5555)
- `LOG_SERVER_DIRECTORY` : Chemin de stockage des logs (par défaut : `/app/logs`)

### 8.3 Configuration Client

**Modifier `appsettings.json`** :
```json
{
  "RemoteLogging": {
    "Enabled": true,
    "ServerHost": "192.168.1.100",
    "ServerPort": 5555,
    "EnableLocalLogging": true
  }
}
```

**Champs** :
- `Enabled` : Mettre à `true` pour activer la journalisation distante
- `ServerHost` : Adresse IP ou nom d'hôte du serveur de logs
- `ServerPort` : Port TCP (doit correspondre au serveur)
- `EnableLocalLogging` : Si `true`, les logs sont écrits localement ET à distance

### 8.4 Format de Log

**Entrée de Log JSON** :
```json
{
  "timestamp": "2024-01-15T10:30:45.123Z",
  "backupName": "DocumentsBackup",
  "sourceFilePath": "\\\\MACHINE1\\C$\\Users\\User\\Documents\\rapport.docx",
  "targetFilePath": "\\\\SERVEUR_BACKUP\\Backups\\Documents\\rapport.docx",
  "fileSize": 1048576,
  "transferTime": 1250,
  "encryptionTime": 0,
  "type": "Full",
  "machineName": "POSTE-TRAVAIL-01"
}
```

### 8.5 Organisation des Logs

Les logs sont organisés par machine cliente :

```
logs/
├── POSTE-TRAVAIL-01/
│   ├── 2024-01-15.json
│   ├── 2024-01-16.json
│   └── 2024-01-17.json
├── POSTE-TRAVAIL-02/
│   ├── 2024-01-15.json
│   └── 2024-01-16.json
└── 192.168.1.50/
    └── 2024-01-15.json
```

**Convention de Nommage** :
- Répertoire : `NomMachine` ou adresse IP
- Fichier : `AAAA-MM-JJ.json`
- Rotation quotidienne : Nouveau fichier créé automatiquement chaque jour

### 8.6 Gestion du Serveur

**Voir les logs en temps réel** :
```bash
docker logs -f easysave-logserver
```

**Arrêter le serveur** :
```bash
docker stop easysave-logserver
```

**Redémarrer le serveur** :
```bash
docker restart easysave-logserver
```

**Supprimer le serveur** :
```bash
docker stop easysave-logserver
docker rm easysave-logserver
```

**Sauvegarder les logs du serveur** :
```bash
tar -czf logs-backup-$(date +%Y%m%d).tar.gz logs/
```

---

## 9. Dépannage

### 9.1 Problèmes Courants

#### "Another instance is already running"

**Cause** : Le mutex empêche plusieurs instances  
**Solution** : Fermer les autres fenêtres EasySave ou tuer le processus

```bash
# Trouver le processus
tasklist | findstr EasySave

# Tuer le processus
taskkill /F /IM EasySave.CLI.exe
taskkill /F /IM EasySave.WPF.exe
```

#### "Source path not found"

**Cause** : Le répertoire n'existe pas ou chemin incorrect  
**Solution** : Vérifier le chemin et s'assurer que le répertoire existe

```bash
# Vérifier si le chemin existe
dir "C:\Chemin\Source"

# Créer le répertoire si nécessaire
mkdir "C:\Chemin\Source"
```

#### "Cannot create backup job - max jobs exceeded"

**Cause** : Limite de 5 tâches appliquée  
**Solution** : Supprimer les tâches inutilisées avant de créer de nouvelles

#### "Backup blocked: Business software running"

**Cause** : Processus surveillé détecté  
**Solution** : Fermer les applications métier ou retirer de `businessSoftwareNames`

#### Journalisation distante ne fonctionne pas

**Causes** :
- Serveur non démarré
- Pare-feu bloquant le port 5555
- IP/port serveur incorrect dans la config

**Solutions** :
```bash
# Vérifier que le serveur fonctionne
docker ps | grep logserver

# Tester la connectivité
telnet 192.168.1.100 5555

# Vérifier le pare-feu (Windows)
netsh advfirewall firewall add rule name="EasySave LogServer" dir=in action=allow protocol=TCP localport=5555
```

### 9.2 Analyse des Logs

**Voir les logs de sauvegarde** :
```bash
# Logs JSON
type %ProgramData%\EasySave\logs\2024-01-15.json

# Filtrer une sauvegarde spécifique
findstr "DocumentsBackup" %ProgramData%\EasySave\logs\2024-01-15.json
```

**Compter les fichiers sauvegardés** :
```powershell
(Get-Content "$env:ProgramData\EasySave\logs\2024-01-15.json" | ConvertFrom-Json).Count
```

**Calculer la taille totale transférée** :
```powershell
(Get-Content "$env:ProgramData\EasySave\logs\2024-01-15.json" | ConvertFrom-Json | 
    Measure-Object -Property fileSize -Sum).Sum / 1MB
```

### 9.3 Optimisation des Performances

#### Performance de sauvegarde lente

**Solutions** :
1. **Augmenter le seuil de fichiers volumineux** : Moins de points de synchronisation
2. **Réduire les extensions prioritaires** : Moins de surcharge de coordination
3. **Désactiver le chiffrement** : Gain de performance significatif
4. **Utiliser un SSD pour la destination** : Vitesses d'écriture plus rapides
5. **Ajuster les paramètres réseau** : Pour les destinations de sauvegarde réseau

#### Utilisation élevée de mémoire

**Solutions** :
1. **Baisser le seuil de fichiers volumineux** : Réduit les transferts parallèles
2. **Désactiver la journalisation distante** : Réduit la mise en mémoire tampon
3. **Fermer les autres applications** : Libérer les ressources système

#### Congestion réseau

**Solutions** :
1. **Planifier les sauvegardes hors heures de pointe**
2. **Utiliser les sauvegardes différentielles** : Moins de données transférées
3. **Activer la synchronisation de fichiers volumineux** : Évite la saturation de bande passante
4. **Compresser les fichiers avant sauvegarde** : Réduire la taille de transfert

---

## 10. Développement

### 10.1 Structure du Projet

**Solution** : `EasySave.sln`

**Projets** :
1. **EasySave.Core** (Bibliothèque de Classes)
   - Cible : .NET 8
   - But : Logique métier principale
   - Dépendances : Aucune

2. **EasySave.CLI** (Application Console)
   - Cible : .NET 8
   - But : Interface en ligne de commande
   - Dépendances : EasySave.Core, EasyLog, CryptoSoft

3. **EasySave.WPF** (Application WPF)
   - Cible : .NET 8 (Windows)
   - But : Interface graphique
   - Dépendances : EasySave.Core, EasyLog, CryptoSoft

4. **EasyLog** (Bibliothèque de Classes)
   - Cible : .NET 8
   - But : Infrastructure de journalisation
   - Dépendances : Aucune

5. **CryptoSoft** (Bibliothèque de Classes)
   - Cible : .NET 8
   - But : Chiffrement de fichiers
   - Dépendances : Aucune

6. **EasySave.LogServer** (Application Console)
   - Cible : .NET 8
   - But : Serveur de logs centralisé
   - Dépendances : Aucune

7. **EasySave.Tests** (Projet de Tests Unitaires)
   - Cible : .NET 8
   - But : Tests automatisés
   - Dépendances : Tous les projets

### 10.2 Compilation

#### Build Debug
```bash
dotnet build EasySave.sln --configuration Debug
```

#### Build Release
```bash
dotnet build EasySave.sln --configuration Release
```

#### Projet Unique
```bash
dotnet build EasySave.Core/EasySave.Core.csproj -c Release
```

### 10.3 Tests

```bash
# Exécuter tous les tests
dotnet test EasySave.Tests/EasySave.Tests.csproj

# Exécuter avec sortie détaillée
dotnet test --verbosity detailed

# Générer un rapport de couverture
dotnet test --collect:"XPlat Code Coverage"
```

### 10.4 Publication

#### Exécutable Autonome
```bash
dotnet publish EasySave.CLI/EasySave.CLI.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o ./publish/cli
```

#### Déploiement Dépendant du Framework
```bash
dotnet publish EasySave.WPF/EasySave.WPF.csproj \
  -c Release \
  -r win-x64 \
  --self-contained false \
  -o ./publish/wpf
```

### 10.5 Étendre EasySave

#### Ajouter un Nouveau Type de Sauvegarde

1. Définir la valeur enum dans `BackupType.cs` :
```csharp
public enum BackupType
{
    Full,
    Differential,
    Incremental  // Nouveau type
}
```

2. Implémenter la logique d'exécution dans `BackupExecutor.cs` :
```csharp
public void ExecuteIncrementalBackup(BackupJob job)
{
    // Implémentation
}
```

3. Mettre à jour `BackupManager.ExecuteBackupJob()` :
```csharp
if (job.GetBackupType() == BackupType.Incremental)
    executor.ExecuteIncrementalBackup(job);
```

#### Ajouter un Nouveau Format de Log

1. Créer une classe de logger dans `EasyLog` :
```csharp
public class CsvLogger : ILogger
{
    public LogFormat Format => LogFormat.Csv;
    
    public void LogAction(LogEntry entry)
    {
        // Logique de formatage CSV
    }
}
```

2. Enregistrer dans `LoggerFactory.cs` :
```csharp
ILogger localLogger = logFormat.ToLower() switch
{
    "xml" => new XmlLogger(logDirectory),
    "csv" => new CsvLogger(logDirectory),
    _ => new JsonLogger(logDirectory)
};
```

#### Ajouter une Nouvelle Langue

1. Ajouter les traductions à `languages.json` :
```json
{
  "en": { ... },
  "fr": { ... },
  "es": {
    "menu.create": "Crear trabajo de copia de seguridad",
    "menu.execute": "Ejecutar copia de seguridad",
    ...
  }
}
```

2. La langue est automatiquement chargée par LanguageProvider

### 10.6 Standards de Code

- **Version C#** : 12.0
- **Framework Cible** : .NET 8
- **Nullable** : Activé
- **Analyse de Code** : Activée (règles CA)
- **Nommage** : PascalCase pour public, camelCase pour privé
- **Documentation** : Commentaires XML pour les APIs publiques

---

## Support & Contributions

**Version :** 3.0  
**Date :** 2024  
**Responsable :** Joan Perreyprato  
**Dépôt :** https://github.com/joanperreyprato/EasySave  
**Licence :** Propriétaire - Solution de Sauvegarde PME

Pour les problèmes, questions ou demandes de fonctionnalités, veuillez visiter le dépôt GitHub.
