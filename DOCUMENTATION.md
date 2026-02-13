# EasySave 2.0 - Documentation

Application de sauvegarde de fichiers professionnelle avec interface graphique WPF et chiffrement AES-256.

---

## Table des Matières

1. [Installation](#installation)
2. [Interface Graphique (WPF)](#interface-graphique-wpf)
3. [Interface Console (CLI)](#interface-console-cli)
4. [Types de Sauvegarde](#types-de-sauvegarde)
5. [Chiffrement](#chiffrement)
6. [Configuration](#configuration)
7. [Logs](#logs)
8. [Dépannage](#dépannage)

---

## Installation

### Prérequis
- **.NET 8.0** ou supérieur
- **Windows** (pour WPF) ou **Windows/Linux/macOS** (pour CLI)

### Compilation
```bash
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
dotnet build
```

---

## Interface Graphique (WPF)

### Lancement
```bash
cd EasySave.WPF
dotnet run
```

### Créer un Travail de Sauvegarde

1. Cliquez sur **Add job**
2. Remplissez le formulaire :
   - **Nom** : Alphanumérique, espaces, tirets (max 50 caractères)
   - **Source** : Chemin du dossier source (doit exister)
   - **Destination** : Chemin du dossier destination (doit exister)
   - **Format de log** : JSON (rapide) ou XML (compatible)
3. Cliquez **OK**

### Exécuter des Sauvegardes

| Action | Bouton |
|--------|--------|
| Exécuter une sauvegarde | Sélectionner + **Start selected** |
| Exécuter toutes | **Start all** |
| Arrêter (non supporté) | Notification affichée |

### Chiffrement

1. Allez dans l'onglet **Chiffrement**
2. Entrez une clé de chiffrement dans le champ PasswordBox
3. Ajoutez des extensions à chiffrer :
   - Tapez l'extension (ex: `.docx`)
   - Cliquez **Ajouter**
4. Pour retirer : Sélectionner + **Supprimer**
5. Lors de l'exécution, une boîte de dialogue demande si vous voulez chiffrer

### Changer de Langue

Utilisez le menu déroulant en haut à droite :
- **English**
- **Français**

---

## Interface Console (CLI)

### Lancement
```bash
cd EasySave.UI
dotnet run
```

### Mode Interactif

Menu principal :
```
1. Create Backup    - Créer une sauvegarde
2. Execute Backup   - Exécuter une sauvegarde
3. List Backups     - Lister les sauvegardes
4. Edit Backup      - Modifier une sauvegarde
5. Change Language  - Changer la langue
6. Exit             - Quitter
```

### Mode Commande

#### Créer une sauvegarde
```bash
dotnet run -- create <nom> <source> <destination> <type>
```

Exemple :
```bash
dotnet run -- create "MesDocs" "C:\Documents" "D:\Backup" full
```

#### Exécuter une sauvegarde
```bash
dotnet run -- execute <index>
```

Exemples :
```bash
# Une seule
dotnet run -- execute 1

# Plusieurs (plage)
dotnet run -- execute 1-3

# Plusieurs (liste)
dotnet run -- execute 1,3,5
```

#### Lister les sauvegardes
```bash
dotnet run -- list
```

---

## Types de Sauvegarde

### Sauvegarde Complète (Full)

- **Quand** : Première sauvegarde ou sauvegarde complète périodique
- **Comportement** : Copie TOUS les fichiers de la source vers la destination
- **Durée** : Plus longue
- **Avantage** : Restauration simple (tout est dans la destination)

### Sauvegarde Différentielle (Differential)

- **Quand** : Mises à jour régulières après une sauvegarde complète
- **Comportement** : Copie uniquement les fichiers nouveaux ou modifiés
- **Critère** : Comparaison de la date de modification et de la taille
- **Durée** : Plus rapide
- **Avantage** : Économise du temps et de l'espace

### Recommandation

```
Jour 1   : Full backup       (tout copier)
Jour 2-7 : Differential      (mises à jour rapides)
Jour 8   : Full backup       (nouvelle base)
```

---

## Chiffrement

### Algorithme

- **AES-256-CBC** : Chiffrement symétrique standard
- **HMAC-SHA256** : Vérification d'intégrité
- **Clé dérivée** : SHA-256 de la clé texte

### Utilisation WPF

1. Définir la clé dans l'onglet **Chiffrement**
2. Ajouter les extensions à chiffrer (ex: `.pdf`, `.docx`, `.xlsx`)
3. Lancer la sauvegarde
4. Choisir "Oui" à la question de chiffrement

### Fonctionnement

1. La sauvegarde est exécutée normalement
2. Si chiffrement activé :
   - Pour chaque fichier copié
   - Si extension correspond au filtre
   - Le fichier est chiffré sur place
   - Le temps de chiffrement est loggé

### Format de Fichier Chiffré

```
[1 byte: IV length][IV][Ciphertext][32 bytes: HMAC]
```

### Déchiffrement

Le projet inclut `CryptoSoft` avec méthode `DecryptFile` :
```csharp
CryptoService.DecryptFile(input, output, key);
```

---

## Configuration

### Emplacement des Fichiers

- **Windows** : `C:\ProgramData\EasySave\`

### Fichiers de Configuration

#### backup_config.json
Stocke les travaux de sauvegarde :
```json
[
  {
    "name": "MesDocs",
    "sourcePath": "C:\\Documents",
    "targetPath": "D:\\Backup",
    "type": 0,
    "logFormat": 0
  }
]
```

| Champ | Description |
|-------|-------------|
| `name` | Nom du travail |
| `sourcePath` | Chemin source |
| `targetPath` | Chemin destination |
| `type` | 0=Full, 1=Differential |
| `logFormat` | 0=JSON, 1=XML |

#### state.json
État en temps réel des sauvegardes :
```json
{
  "name": "MesDocs",
  "timestamp": "2025-01-20T10:30:00Z",
  "status": 1,
  "totalFiles": 100,
  "totalSize": 104857600,
  "remainingFiles": 50,
  "remainingSize": 52428800,
  "currentSourceFile": "C:\\Documents\\file.txt",
  "currentTargetFile": "D:\\Backup\\file.txt",
  "progress": 50.0
}
```

| Statut | Valeur |
|--------|--------|
| Inactive | 0 |
| Active | 1 |
| Completed | 2 |
| Error | 3 |

---

## Logs

### Formats Disponibles

| Format | Avantages | Inconvénients |
|--------|-----------|---------------|
| **JSON** | Facile à parser, compact | Moins lisible visuellement |
| **XML** | Standard, compatible outils legacy | Plus verbeux |

### Emplacement

`C:\ProgramData\EasySave\logs\`

### Structure de Log

#### Entrée Fichier
Loggée après chaque fichier copié :
```json
{
  "timestamp": "2025-01-20T10:30:15Z",
  "jobName": "MesDocs",
  "sourcePath": "C:\\Documents\\file.txt",
  "targetPath": "D:\\Backup\\file.txt",
  "fileSize": 1024000,
  "transferTime": 150,
  "encryptionTime": 50,
  "entryType": "File"
}
```

#### Entrée Résumé
Loggée à la fin de la sauvegarde :
```json
{
  "timestamp": "2025-01-20T10:35:00Z",
  "jobName": "MesDocs",
  "sourcePath": "C:\\Documents",
  "targetPath": "D:\\Backup",
  "fileSize": 104857600,
  "transferTime": 30000,
  "entryType": "Summary"
}
```

| Champ | Description |
|-------|-------------|
| `transferTime` | Temps de transfert (ms) |
| `encryptionTime` | Temps de chiffrement (ms), -1 si erreur |
| `entryType` | File ou Summary |

---

## Dépannage

### Erreurs Courantes

#### "Source path invalid"
**Cause** : Chemin source mal formaté ou inexistant  
**Solution** : Vérifiez que le chemin existe et est accessible

#### "Target path invalid"
**Cause** : Chemin destination mal formaté ou inexistant  
**Solution** : Créez le dossier de destination ou vérifiez les permissions

#### "Invalid job name"
**Cause** : Caractères non autorisés dans le nom  
**Solution** : Utilisez uniquement alphanumérique, espaces, tirets, underscores

#### "Duplicate name"
**Cause** : Un travail avec ce nom existe déjà  
**Solution** : Choisissez un nom unique

#### "Encryption key missing"
**Cause** : Chiffrement demandé mais clé non définie  
**Solution** : Définissez une clé dans l'onglet Chiffrement

### Réinitialiser la Configuration

```bash
# Supprimer le dossier de configuration
Remove-Item -Recurse -Force "$env:ProgramData\EasySave"

# Relancer l'application
dotnet run
```

### Vérifier les Logs

1. Ouvrez `C:\ProgramData\EasySave\logs\`
2. Consultez les fichiers JSON ou XML
3. Recherchez les erreurs (`transferTime: -1` ou `encryptionTime: -1`)

### Performance Lente

**Problèmes possibles** :
- Disque source ou destination lent
- Fichiers très nombreux ou très gros
- Chiffrement activé (ajoute du temps)

**Solutions** :
- Utilisez Differential après la première Full
- Désactivez le chiffrement si non nécessaire
- Vérifiez l'utilisation disque dans le Gestionnaire des tâches

---

## Limitations Connues

- **Pas d'annulation** : Impossible d'arrêter une sauvegarde en cours
- **Pas de pause** : Les sauvegardes doivent se terminer
- **Limite de travaux** : Maximum 5 travaux (modifiable dans le code)
- **Mono-thread** : Les fichiers sont copiés séquentiellement

---

## Conseils d'Utilisation

?? **Première utilisation** : Toujours faire une sauvegarde Full

?? **Fichiers sensibles** : Utilisez le chiffrement avec une clé forte (12+ caractères)

?? **Sauvegardes régulières** : Full hebdomadaire + Differential quotidien

?? **Performance** : JSON est plus rapide que XML

?? **Surveillance** : Consultez `state.json` pour suivre la progression

---

## Support

- **Documentation technique** : [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md)
- **FAQ** : [FAQ.md](FAQ.md)
- **Issues GitHub** : [github.com/joanperreyprato/EasySave/issues](https://github.com/joanperreyprato/EasySave/issues)

---

**Version** : 2.0  
**Dernière mise à jour** : Janvier 2025

? Documentation complète - Prêt à utiliser EasySave !
