# 🗺️ Plan de refonte — EasySave Joan
> Basé sur le **cahier des charges officiel CESI** (PDF extrait le 07/09/2026)

---

## ⚠️ Différences importantes entre l'original et le cahier des charges

| Point | Original (autre groupe) | Cahier des charges officiel | Notre version |
|---|---|---|---|
| **CryptoSoft** | AES-256-CBC + HMAC-SHA256 (sur-ingéniéré) | **XOR bit à bit** avec clé ≥ 64 bits, **exécutable console** séparé, code retour positif=ms / négatif=erreur | XOR conforme, exécutable `CryptoSoft.exe` |
| **Limite jobs v1.0** | Non appliquée dans le code | **Strictement limité à 5** (v1.0 et v1.1), **illimité** à partir de v2.0 | Vérification stricte |
| **Logs v1.0** | JSON (NDJSON invalide) | **JSON uniquement**, lisibles Notepad (retours à la ligne), emplacement serveur-compatible | JSON valide + indenté |
| **Logs v1.1** | Non différencié | Ajout du **choix XML ou JSON** par l'utilisateur | Sélection configurable |
| **Business software v2.0** | Pause auto | **Interdit le lancement** d'un travail (v2.0), en v3.0 → **pause auto** + reprise | Implémentation fidèle par version |
| **Business software v3.0** | Pause auto | Pause obligatoire de TOUS les travaux, reprise automatique à la fermeture | Pause collective |
| **CryptoSoft mono-instance** | Non implémenté | Requis en **v3.0** : un seul CryptoSoft en simultané | Mutex système |
| **Logs Docker v3.0** | Optionnel | 3 modes : local seul, distant seul, ou les deux | 3 modes configurables |
| **Interface v2.0** | WPF MVVM | WPF **ou** Avalonia au choix | WPF MVVM (notre choix) |

---

## 📦 Versions — Fonctionnalités exactes (cahier des charges)

### ✅ v1.0 — CLI + Logs JSON
| Fonctionnalité | Détail |
|---|---|
| Application **console** .NET 8 | CLI uniquement |
| **Max 5 travaux** de sauvegarde | Vérification stricte dans le code |
| Travail = nom + source + cible + type | Full ou Différentielle |
| **Multi-langue** FR / EN | Dès la v1.0 |
| Exécution via ligne de commande | `EasySave.exe 1-3` ou `EasySave.exe 1;3` |
| Répertoires : locaux, externes, réseau | Chemins UNC supportés |
| Tous fichiers + sous-répertoires copiés | Récursif |
| **Fichier log journalier** (JSON) | Timestamp, nom, source UNC, dest UNC, taille, temps transfert (ms, négatif=erreur) |
| **EasyLog.dll** séparée | Bibliothèque dédiée au logging, évolutive |
| **Fichier état temps réel** (JSON) | Nom, timestamp, état, nb fichiers, taille, progression, fichier en cours |
| Emplacements serveur-compatibles | Pas de `C:\temp\`, utiliser `%ProgramData%\EasySave\` |
| JSON lisible Notepad | Indenté, retours à la ligne |

**Tag git : `v1.0`**

---

### ✅ v1.1 — Choix format log (JSON ou XML)
| Fonctionnalité | Détail |
|---|---|
| Tout v1.0 + | |
| **Choix du format log** | L'utilisateur choisit JSON ou XML |
| Format XML du log | Valide, lisible, compatible avec la suite |

> ⚠️ v1.1 sort **en même temps** que v2.0 (Livrable 2 du projet)

**Tag git : `v1.1`**

---

### ✅ v2.0 — Interface WPF + Chiffrement + Business Software
| Fonctionnalité | Détail |
|---|---|
| **Interface graphique WPF** | Architecture MVVM obligatoire |
| **Nombre de travaux illimité** | Plus de limite à 5 |
| **CryptoSoft** — Exécutable séparé | `CryptoSoft.exe <source> <destination>` |
| Algorithme chiffrement | **XOR bit à bit** entre fichier et clé (≥ 64 bits) |
| Clé lue depuis fichier de config | `cryptosoft.config` ou équivalent |
| Code retour CryptoSoft | > 0 : temps en ms / < 0 : erreur |
| Seuls les fichiers aux extensions définies | Configurable dans les paramètres |
| **Temps de chiffrement dans les logs** | 0=pas de chiffrement, >0=ms, <0=erreur |
| **Logiciel métier v2.0** | **Interdit le lancement** du travail si process détecté |
| L'arrêt doit être loggué | Entrée dans le fichier log |
| Choix JSON ou XML | Hérité de v1.1, présent en v2.0 aussi |
| Dark/Light mode | (ajout personnel "à notre sauce") |
| Multi-langue FR/EN | Dans l'interface WPF |

**Tag git : `v2.0`**

---

### ✅ v3.0 — Parallélisme + Pause/Play/Stop + Docker
| Fonctionnalité | Détail |
|---|---|
| **Sauvegarde en parallèle** | Abandon du mode séquentiel |
| **Fichiers prioritaires** | Aucun fichier non-prioritaire tant qu'il y a des prioritaires en attente sur n'importe quel job |
| Extensions prioritaires | Liste configurable dans les paramètres |
| **Interdiction transferts parallèles volumineux** | Un seul fichier > n Ko transféré à la fois (n paramétrable) |
| Pendant transfert gros fichier | Les autres jobs peuvent transférer des petits (si règle priorité ok) |
| **Pause** par travail ou global | Effective après le transfert du fichier en cours |
| **Play** par travail ou global | Démarrage ou reprise de pause |
| **Stop** par travail ou global | Arrêt immédiat (fin du fichier en cours) |
| **Progression temps réel** | Au minimum un % par travail dans l'UI |
| **Business software v3.0** | Pause **automatique** de TOUS les travaux, reprise auto à la fermeture |
| **CryptoSoft mono-instance** | Un seul processus CryptoSoft sur la machine (Mutex système) |
| **Centralisation logs Docker** | Serveur TCP sous Docker, 3 modes : local, distant, ou les deux |
| Fichier log centralisé | Un seul fichier par jour, différenciation par machine |

**Tag git : `v3.0`**

---

## 🏗️ Architecture cible (notre sauce)

```
EasySave_Joan/
├── EasySave.Core/               # Logique métier pure (zéro dépendance externe)
│   ├── Models/                  # BackupJob, JobState, BackupType...
│   ├── Interfaces/              # IBackupService, ILanguageProvider, IJobController...
│   ├── Services/                # BackupService, StateManager, BusinessSoftwareMonitor...
│   ├── Configuration/           # AppConfig, LoggingConfig, EncryptionConfig...
│   └── Exceptions/              # EasySaveException, BackupException...
│
├── EasySave.CLI/                # Console (v1.0 et v1.1)
│   ├── Program.cs
│   └── ConsoleMenu.cs
│
├── EasySave.WPF/                # Interface WPF (v2.0+)
│   ├── ViewModels/              # MainViewModel, JobViewModel, SettingsViewModel...
│   ├── Views/                   # MainWindow, AddJobDialog, SettingsView...
│   ├── Styles/                  # Thèmes dark/light (ResourceDictionary)
│   └── Converters/              # IValueConverter pour le binding MVVM
│
├── EasyLog/                     # DLL de logging (v1.0+)
│   ├── ILogger.cs
│   ├── LogEntry.cs
│   ├── JsonLogger.cs            # JSON indenté valide
│   ├── XmlLogger.cs             # XML valide avec racine <Logs>
│   ├── SocketLogger.cs          # TCP vers LogServer (IDisposable correct)
│   ├── CompositeLogger.cs       # Pattern Composite
│   └── LoggerFactory.cs         # Factory sans réflexion
│
├── CryptoSoft/                  # Exécutable console séparé (v2.0+)
│   ├── Program.cs               # cryptosoft.exe <source> <destination>
│   ├── XorCryptoService.cs      # XOR bit à bit, clé ≥ 64 bits
│   └── config.json              # Clé de chiffrement configurable
│
├── EasySave.LogServer/          # Serveur TCP Docker (v3.0)
│   ├── Program.cs
│   ├── LogServerService.cs      # Async multi-client, 3 modes
│   └── Dockerfile               # Multi-stage build
│
└── EasySave.Tests/              # Tests xUnit + Moq
    ├── Core.Tests/
    ├── EasyLog.Tests/
    └── CryptoSoft.Tests/
```

---

## 🎨 Règles qualité (toutes versions)

| Critère | Règle |
|---|---|
| **Langue du code** | Anglais uniquement (variables, méthodes, classes, commentaires) |
| **Commentaires** | XML/DocString sur tout le `public` |
| **Lisibilité** | Fonctions courtes, 0 copier-coller, nommage conventionnel C# |
| **MVVM strict** | Zéro logique métier dans le code-behind WPF |
| **DI par interfaces** | `IBackupService`, `ILogger`, `ILanguageProvider` partout |
| **JSON logs** | Tableau JSON valide et indenté (lisible Notepad) |
| **XML logs** | Document XML valide avec racine `<Logs>` |
| **Emplacement fichiers** | `%ProgramData%\EasySave\` (pas de `C:\temp\`) |
| **Tests** | xUnit + Moq, dossiers temporaires, `IDisposable` pour nettoyage |
| **README** | FR + EN, mis à jour à chaque version, max 1 page pour l'utilisateur |
| **Release note** | Obligatoire à chaque version |
| **Git** | 1 commit + 1 tag par version livrée |

---

## 🚀 Ordre d'exécution

```
Phase 1 : v1.0 — Core + CLI + EasyLog (JSON) + Tests
Phase 2 : v1.1 — Ajout XML au choix + Tests
Phase 3 : v2.0 — WPF MVVM + CryptoSoft.exe (XOR) + Business Monitor (interdit lancement)
Phase 4 : v3.0 — Parallélisme + Pause/Play/Stop + CryptoSoft mono-instance + LogServer Docker
```

> [!IMPORTANT]
> **CryptoSoft** dans ce projet = **XOR bit à bit** (pas AES-256). C'est un exécutable console séparé (`CryptoSoft.exe`), pas une DLL. L'original avait fait AES ce qui ne respectait pas le cahier des charges.

> [!NOTE]
> **v1.1 sort en même temps que v2.0** (Livrable 2). Je les développerai donc en parallèle mais chacune aura son propre tag git.

> [!TIP]
> Utilisez `/goal` pour lancer toute la réalisation en mode autonome overnight si vous souhaitez que les 4 versions soient faites en une seule session.

---

## 📝 TODO / Idées d'amélioration (hors cahier des charges)

- [ ] **Benchmark de performance (Temps d'exécution) :** Comparer le temps de copie de notre application avec d'autres logiciels du marché (comme Robocopy, FreeFileSync, rsync, ou un simple copier-coller Windows) sur un même dataset. Utile pour justifier nos performances lors de la soutenance (surtout après l'ajout du parallélisme en v3.0).
