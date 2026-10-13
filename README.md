# 🛡️ EasySave — Logiciel de Sauvegarde ProSoft

> Application de sauvegarde professionnelle développée en C# / .NET 8.0 pour ProSoft.  
> Disponible en **mode Console** (v1.x) et **interface graphique WPF** (v2.0 / v3.0).

---

## 📋 Table des matières

- [Présentation](#-présentation)
- [Évolution des versions](#-évolution-des-versions)
- [Architecture du projet](#-architecture-du-projet)
- [Prérequis](#-prérequis)
- [Installation & Lancement](#-installation--lancement)
- [Guide d'utilisation](#-guide-dutilisation)
- [CryptoSoft — Logiciel de chiffrement](#-cryptosoft--logiciel-de-chiffrement)
- [LogServer — Serveur de logs Docker](#-logserver--serveur-de-logs-docker)
- [Tests](#-tests)
- [Technologies utilisées](#-technologies-utilisées)

---

## 🎯 Présentation

**EasySave** est un logiciel de sauvegarde développé par l'équipe ProSoft. Il permet de :

- ✅ Créer des travaux de sauvegarde (complète ou différentielle)
- ✅ Exécuter les sauvegardes en parallèle avec contrôle Play/Pause/Stop
- ✅ Chiffrer les fichiers sensibles via **CryptoSoft** (XOR 64 bits)
- ✅ Détecter un logiciel métier et mettre en pause automatiquement
- ✅ Centraliser les logs sur un serveur Docker distant
- ✅ Interface bilingue Français / Anglais (changement en temps réel)
- ✅ Thèmes Dark / Light

---

## 📈 Évolution des versions

```
v1.0 ──► v1.1 ──────────► v2.0 ──────────────────► v3.0
 │         │                │                         │
 │         │                │                         ├── Sauvegardes en parallèle
 │         │                │                         ├── Play / Pause / Stop par job
 │         │                │                         ├── Fichiers prioritaires
 │         │                │                         ├── Limitation gros fichiers
 │         │                │                         ├── CryptoSoft mono-instance
 │         │                │                         ├── EasySave mono-instance
 │         │                │                         ├── LogServer Docker (TCP)
 │         │                │                         └── Nouveau design (sidebar)
 │         │                │
 │         │                ├── Interface graphique WPF (MVVM)
 │         │                ├── Travaux illimités
 │         │                ├── Chiffrement CryptoSoft XOR
 │         │                ├── Détection logiciel métier
 │         │                ├── Thèmes Dark / Light
 │         │                └── Barres de progression
 │         │
 │         └── Choix du format de log (JSON ou XML)
 │
 ├── Application console
 ├── Maximum 5 travaux
 ├── Sauvegarde complète & différentielle
 ├── Logs JSON (via DLL EasyLog)
 ├── Fichier d'état (state.json)
 └── Bilingue FR / EN
```

| Fonction | v1.0 | v1.1 | v2.0 | v3.0 |
|---|:---:|:---:|:---:|:---:|
| Interface | Console | Console | WPF | WPF |
| Langues | FR/EN | FR/EN | FR/EN | FR/EN |
| Nb travaux max | 5 | 5 | Illimité | Illimité |
| Format logs | JSON | JSON/XML | JSON/XML | JSON/XML |
| CryptoSoft | ❌ | ❌ | ✅ | ✅ (mono-instance) |
| Logiciel métier | ❌ | ❌ | ✅ (blocage) | ✅ (auto-pause) |
| Exécution | Séquentielle | Séquentielle | Séquentielle | **Parallèle** |
| Play/Pause/Stop | ❌ | ❌ | ❌ | ✅ |
| Fichiers prioritaires | ❌ | ❌ | ❌ | ✅ |
| Gros fichiers limités | ❌ | ❌ | ❌ | ✅ |
| LogServer Docker | ❌ | ❌ | ❌ | ✅ |

### Accéder à une version spécifique

```bash
git checkout v1.0    # Version 1.0 — Console basique
git checkout v1.1    # Version 1.1 — Console + XML
git checkout v2.0    # Version 2.0 — Interface graphique WPF
git checkout main    # Version 3.0 — Version actuelle complète
```

---

## 🏗️ Architecture du projet

```
EasySave_Joan/
│
├── EasySave.CLI/            # Application console (v1.x)
│   └── Program.cs           # Point d'entrée CLI
│
├── EasySave.WPF/            # Application graphique (v2.0 / v3.0)
│   ├── Views/               # Fenêtres XAML
│   ├── ViewModels/          # Logique MVVM
│   └── Styles/              # Thèmes Dark & Light
│
├── EasySave.Core/           # Bibliothèque métier partagée
│   ├── Models/              # BackupJob, BackupType, JobStatus
│   ├── Services/            # BackupServiceV3, AppSettings, StateManager
│   │   ├── JobController.cs          # Pause/Stop par job (ManualResetEventSlim)
│   │   ├── PriorityTransferGuard.cs  # Règle fichiers prioritaires
│   │   └── LargeFileTransferGuard.cs # Limitation gros fichiers (SemaphoreSlim)
│   └── Interfaces/          # Contrats (ILanguageProvider, etc.)
│
├── EasyLog/                 # DLL de logging (JSON, XML, Socket, Composite)
│   ├── LoggerFactory.cs     # Fabrique de loggers
│   ├── SocketLogger.cs      # Envoi TCP vers le LogServer
│   └── CompositeLogger.cs   # Pattern Composite (local + distant)
│
├── CryptoSoft/              # Logiciel de chiffrement XOR (mono-instance)
│   └── Program.cs
│
├── EasySave.LogServer/      # Serveur TCP de centralisation des logs
│   ├── Program.cs           # Serveur async multi-clients
│   └── Dockerfile
│
├── EasySave.Tests/          # Tests unitaires (xUnit)
├── EasySave.Tests.WPF/      # Tests de composants WPF
├── EasySave.Tests.Integration/ # Tests d'intégration
│
├── docker-compose.yml       # Lancement du LogServer
└── .github/workflows/       # CI GitHub Actions
```

---

## ⚙️ Prérequis

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 (requis pour WPF)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optionnel, pour le LogServer)

---

## 🚀 Installation & Lancement

### 1. Cloner le projet

```bash
git clone https://github.com/joanperreyprato/EasySave_Joan.git
cd EasySave_Joan
```

### 2. Compiler

```bash
dotnet build EasySave.sln
```

### 3. Lancer l'application

**Mode graphique (WPF) — Recommandé :**
```bash
dotnet run --project EasySave.WPF
```

**Mode console (CLI) :**
```bash
dotnet run --project EasySave.CLI
```

**Depuis VS Code :**  
Ouvrez le projet, puis appuyez sur `F5`. Choisissez la configuration souhaitée dans le menu déroulant :
- 🚀 Launch EasySave (WPF - Interface Graphique)
- 📟 Launch EasySave (CLI - Mode Console)

> ⚠️ **EasySave est mono-instance** : une seule instance (WPF ou CLI) peut tourner à la fois sur le même PC.

---

## 📖 Guide d'utilisation

### Créer un travail de sauvegarde

1. Cliquez sur **＋ Nouveau** dans la barre latérale gauche
2. Remplissez le formulaire :
   - **Nom** : nom unique du travail (ex: "Sauvegarde Documents")
   - **Source** : dossier à sauvegarder (ex: `C:\Users\Joan\Documents`)
   - **Cible** : dossier de destination (ex: `D:\Backups\Documents`)
   - **Type** : Complète (tous les fichiers) ou Différentielle (uniquement les fichiers modifiés)
3. Cliquez sur **✓ Créer**

### Exécuter une sauvegarde

- **Un seul job** : Cochez la case du job → cliquez sur **▶ Lancer sélection**
- **Tous les jobs** : Cliquez sur **▶▶ Lancer tout**

### Contrôler les sauvegardes en cours (v3.0)

- **⏸ Suspendre** : Met en pause le job sélectionné (attend la fin du fichier en cours)
- **▶ Reprendre** : Relance un job en pause
- **⏹ Arrêter** : Annule définitivement le job sélectionné
- Les boutons **"Toutes les sauvegardes"** appliquent l'action à tous les jobs en même temps

### Paramètres

En bas de la fenêtre, vous trouverez les paramètres configurables :

| Paramètre | Description | Exemple |
|---|---|---|
| **Logiciel métier** | Nom du processus qui bloque les sauvegardes | `notepad` |
| **Extensions à chiffrer** | Extensions traitées par CryptoSoft | `.docx, .xlsx, .pdf` |
| **Extensions prioritaires** | Extensions copiées en premier | `.doc, .pdf` |
| **Gros fichier (Ko)** | Taille max pour les transferts parallèles | `1024` |
| **Format de log** | JSON ou XML | `JSON` |

Cliquez sur **Sauvegarder** pour appliquer les paramètres.

### Changer la langue

Cliquez sur le bouton **EN** / **FR** en bas de la barre latérale. Toute l'interface se traduit instantanément sans redémarrer.

### Changer le thème

Cliquez sur le bouton **☀️ Light** / **🌙 Dark** pour basculer entre les thèmes.

---

## 🔐 CryptoSoft — Logiciel de chiffrement

**CryptoSoft** est un exécutable console indépendant qui chiffre/déchiffre les fichiers avec l'algorithme **XOR bit à bit** (clé de 64 bits minimum).

### Utilisation manuelle

```bash
CryptoSoft.exe <fichier_source> <fichier_destination>
```

### Fonctionnement dans EasySave

Quand EasySave copie un fichier dont l'extension est dans la liste "Extensions à chiffrer", il appelle automatiquement CryptoSoft pour chiffrer le fichier avant de le copier dans le dossier cible.

### Mono-instance

CryptoSoft utilise un `Mutex` système (`Global\CryptoSoftMutex_EasySave`). Si un autre processus CryptoSoft est déjà en cours, le suivant attend (timeout de 30 secondes).

### Configuration

La clé de chiffrement se trouve dans :
```
%ProgramData%\EasySave\crypto_key.txt
```

---

## 🐳 LogServer — Serveur de logs Docker

Le LogServer est un serveur TCP qui reçoit et affiche les logs de sauvegarde en temps réel avec des couleurs.

### Lancement

```bash
docker-compose up -d --build
```

### Consultation des logs

```bash
docker logs -f easysave-logserver
```

Vous verrez en temps réel :
```
[21:36:23] [test 1] Copied 'rapport.docx' (1024 B) in 5ms [🔒 1ms]
[21:36:23] [test 1] Copied 'photo.jpg' (2048 B) in 3ms
```

### Modes de fonctionnement

| Mode | Description |
|---|---|
| **Local uniquement** | Les logs sont écrits sur le PC (JSON/XML) |
| **Distant uniquement** | Les logs sont envoyés au serveur Docker |
| **Local + Distant** | Les logs sont écrits localement ET envoyés au serveur (mode par défaut) |

### Arrêt

```bash
docker-compose down
```

---

## 🧪 Tests

Le projet contient **46 tests automatisés** répartis en 3 catégories :

| Type | Projet | Nombre | Ce qui est testé |
|---|---|:---:|---|
| **Unitaires** | `EasySave.Tests` | 40 | Logique métier, validation, logs, guards |
| **Composants** | `EasySave.Tests.WPF` | 4 | Interface WPF sans fenêtre réelle |
| **Intégration** | `EasySave.Tests.Integration` | 2 | Sauvegarde complète avec vrais fichiers |

### Lancer les tests

```bash
dotnet test EasySave.sln
```

### Intégration Continue (CI)

Les tests sont exécutés automatiquement à chaque `push` sur GitHub via **GitHub Actions** (`.github/workflows/tests.yml`).

---

## 🛠️ Technologies utilisées

| Technologie | Usage |
|---|---|
| **C# / .NET 8.0** | Langage et framework principal |
| **WPF (MVVM)** | Interface graphique Windows |
| **xUnit** | Framework de tests |
| **Docker** | Conteneurisation du LogServer |
| **GitHub Actions** | Intégration continue (CI) |
| **TCP Sockets** | Communication réseau pour les logs |
| **XOR Cipher** | Algorithme de chiffrement (CryptoSoft) |

---

## 👥 Auteur

- **Joan Perrey Prato** — Étudiant PGE A3 FISA Informatique — CESI

---

*Projet réalisé dans le cadre du cours de Programmation Système — Génie Logiciel 2025-2026*
