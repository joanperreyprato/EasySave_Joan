# EasySave - FAQ (Questions Fréquemment Posées)

---

## Installation & Configuration

### Q1 : Quel est le prérequis pour installer EasySave 

**R :** Vous avez besoin de **.NET 8.0 ou supérieur** installé sur votre machine.

```bash
dotnet --version
```

Téléchargez .NET 8 sur : https://dotnet.microsoft.com/download

---

### Q2 : Où sont stockés les fichiers de configuration 

**R :** Les fichiers sont créés automatiquement dans :

- **Windows** : `C:\ProgramData\EasySave\`
- **Linux** : `/opt/EasySave/` ou `~/.local/share/EasySave/`
- **macOS** : `~/.local/share/EasySave/`

Fichiers créés :
- `backup_config.json` - Vos tâches configurées
- `state.json` - État actuel des sauvegardes
- `logs/` - Historique des opérations

---

### Q3 : Comment changer la langue 

**R :** Modifiez `Program.cs` ligne 2 :

```csharp
var language = new LanguageProvider("fr");  // Français
var language = new LanguageProvider("en");  // Anglais (défaut)
```

Puis recompiler :
```bash
dotnet build
dotnet run
```

---

## Création de sauvegardes

### Q4 : Quel format de nom puis-je utiliser 

**R :** Le nom doit contenir uniquement :
-  Lettres (a-z, A-Z)
-  Chiffres (0-9)
-  Espaces
-  Tirets (-)
-  Underscores (_)
-  Caractères spéciaux (@, #, $, %, etc.)
-  Maximum 50 caractères

**Exemples valides** :
```
 MonBackup
 Backup-2024
 Photos_Famille
 My Important Data
```

**Exemples invalides** :
```
 Mon@Backup (contient @)
 My/Backup (contient /)
 MyBackupVeryLongNameThatExceedsFiftyCharactersLimit (trop long)
```

---

### Q5 : Combien de sauvegardes puis-je créer 

**R :** **Maximum 5 sauvegardes** peuvent être configurées.

Si vous atteindrez cette limite, vous devez en supprimer une avant d'en créer une nouvelle.

---

### Q6 : Les chemins source et cible doivent-ils exister 

**R :** **OUI, les deux doivent exister** avant de créer une sauvegarde.

```bash
#  Erreur : Le dossier n'existe pas
dotnet run -- create "Backup" "/chemin/inexistant" "/backup" full

#  OK : Les deux dossiers existent
dotnet run -- create "Backup" "/chemin/existant" "/backup/existant" full
```

**Solution** : Créez d'abord les dossiers :
```bash
# Windows
mkdir C:\Backups

# Linux/macOS
mkdir ~/Backups
```

---

### Q7 : Puis-je sauvegarder vers un disque USB ou un réseau 

**R :** **OUI**, tant que le chemin est accessible comme répertoire local.

```bash
# Disque USB (exemple Windows)
dotnet run -- create "USB-Backup" "C:\Users\Data" "E:\Backup" full

# Partage réseau (exemple Linux)
dotnet run -- create "Network-Backup" "/home/data" "/mnt/network/backup" full
```

---

## Types de sauvegarde

### Q8 : Quelle est la différence entre "Full" et "Diff" 

**R :**

| Aspect | Full | Diff |
|--------|------|------|
| **Fichiers copiés** | TOUS | Seulement nouveaux/modifiés |
| **Temps** |  Plus long |  Très rapide |
| **Espace disque** |  Beaucoup |  Moins |
| **Quand l'utiliser** | 1ère fois | Mises à jour régulières |
| **Exemples** | Backup initial | Backup hebdomadaire |

---

### Q9 : Comment les fichiers modifiés sont-ils détectés en mode Diff 

**R :** EasySave compare :

1. **Date de modification** - Si le fichier source est plus récent
2. **Taille du fichier** - Si tailles différentes

Un fichier est copié si l'une de ces conditions est vraie.

---

### Q10 : Puis-je exécuter plusieurs sauvegardes à la fois 

**R :** **OUI**, en ligne de commande :

```bash
# Exécuter indices 0, 1, 2
dotnet run -- execute 0-2

# Exécuter spécifiques
dotnet run -- execute 0,2,4

# Exécuter toutes une par une
dotnet run -- execute 0-4
```

**Note** : Elles s'exécutent **séquentiellement** (une après l'autre), pas en parallèle.

---

## Exécution & Suivi

### Q11 : Comment suivre la progression d'une sauvegarde 

**R :** L'état est mis à jour en temps réel dans `state.json` :

```json
{
  "name": "MonBackup",
  "progress": 35.5,
  "status": "Active",
  "totalFiles": 1000,
  "remainingFiles": 650,
  "currentSourceFile": "/home/user/file123.txt"
}
```

**Voir l'état** :
```bash
# Afficher l'état en direct (Linux/macOS)
watch -n 1 'cat ~/.local/share/EasySave/state.json | jq .'

# Windows PowerShell
Get-Content $env:PROGRAMDATA\EasySave\state.json -Wait
```

---

### Q12 : Que se passe-t-il si une sauvegarde échoue 

**R :** L'état passe à `Error` et :
- Les fichiers déjà copiés restent en place
- Les fichiers non encore traités ne sont pas copiés
- Vous pouvez relancer la sauvegarde pour continuer

```bash
dotnet run -- execute 0  # Relancer
```

---

### Q13 : Comment annuler une sauvegarde en cours 

**R :** Appuyez sur `Ctrl+C` (interruption standard).

**Attention** : Les fichiers déjà copiés ne seront pas annulés.

---

## Mode CLI (Ligne de commande)

### Q14 : Comment créer une sauvegarde en mode CLI 

**R :**

```bash
dotnet run -- create <nom> <source> <cible> <type>
```

**Paramètres** :
- `<nom>` : Nom unique de votre sauvegarde
- `<source>` : Chemin du dossier à sauvegarder
- `<cible>` : Chemin destination
- `<type>` : `full` ou `diff` (défaut : full)

**Exemples** :
```bash
# Sauvegarde complète
dotnet run -- create "Documents" "C:\Users\Docs" "D:\Backup" full

# Sauvegarde différentielle
dotnet run -- create "Photos" "/home/photos" "/backup/photos" diff

# Sans type (full par défaut)
dotnet run -- create "Videos" "/home/videos" "/backup/videos"
```

---

### Q15 : Comment exécuter plusieurs sauvegardes spécifiques 

**R :** Utilisez plages et listes :

```bash
# Plage : indices 0, 1, 2
dotnet run -- execute 0-2

# Liste : indices spécifiques
dotnet run -- execute 0,2,4

# Combinaison
dotnet run -- execute 0,2-4,7
```

**Résultat** : `[0, 2, 3, 4, 7]`

---

## Dépannage

### Q16 : Erreur "Invalid job name"

**Cause** : Le nom contient des caractères non autorisés ou est vide.

**Solution** :
```bash
#  Mauvais
dotnet run -- create "My@Backup" ...

#  Bon
dotnet run -- create "My_Backup" ...
```

Voir Q4 pour les caractères valides.

---

### Q17 : Erreur "Source directory does not exist"

**Cause** : Le dossier source n'existe pas.

**Solution** :
```bash
# Vérifier que le chemin existe
ls /home/user/Documents  # Linux/macOS
dir C:\Users\Documents   # Windows

# Créer s'il n'existe pas
mkdir -p /home/user/Documents  # Linux/macOS
mkdir C:\Users\Documents        # Windows
```

---

### Q18 : Erreur "Target directory does not exist"

**Cause** : Le dossier destination n'existe pas.

**Solution** :
```bash
# Créer le dossier destination
mkdir -p /backup/target  # Linux/macOS
mkdir C:\Backup\Target   # Windows

# Puis créer la sauvegarde
dotnet run -- create "Backup" "/source" "/backup/target" full
```

---

### Q19 : Erreur "A backup job with this name already exists"

**Cause** : Une sauvegarde avec ce nom existe déjà.

**Solution** :
```bash
# Option 1 : Utiliser un autre nom
dotnet run -- create "MonBackup2" ...

# Option 2 : Supprimer l'ancienne (dans config)
# Modifier backup_config.json manuellement
```

---

### Q20 : Erreur "Maximum number of backup jobs (5) reached"

**Cause** : Vous avez déjà 5 sauvegardes configurées.

**Solution** :
```bash
# Supprimer une sauvegarde inutile
# Éditer backup_config.json et retirer une entrée
```

---

### Q21 : Aucun fichier n'a été copié

**Cause possible** : 
- Mode Diff activé et aucun fichier nouveau/modifié
- Permissions insuffisantes
- Chemin source vide

**Solution** :
```bash
# Vérifier contenu source
ls -la /source

# Utiliser Full pour copier tous les fichiers
dotnet run -- create "Test" "/source" "/target" full
```

---

## Performance & Optimisation

### Q22 : Comment accélérer une sauvegarde 

**R :**

1. **Utiliser le mode Diff** au lieu de Full
   ```bash
   dotnet run -- create "Backup" "/source" "/target" diff
   ```

2. **Sauvegarder moins de fichiers** (exclure dossiers volumineux)

3. **Utiliser un disque SSD** comme destination

4. **Vérifier l'espace disque** sur la cible

---

### Q23 : Combien d'espace disque faut-il 

**R :** Dépend de votre source :

- **Espace requis** = Taille totale source
- Exemple : 100 GB source  100 GB cible minimum
- +10% de marge recommandée

Vérifier :
```bash
# Linux/macOS
du -sh /source
df -h /target

# Windows
Get-ChildItem -Recurse /source | Measure-Object -Sum Length
Get-Volume  # Espace libre
```

---

## Sécurité & Fiabilité

### Q24 : Mes données sont-elles chiffrées 

**R :** **NON**, les données ne sont pas chiffrées par EasySave.

Les fichiers sont copiés tels quels. Pour chiffrer :
- Utiliser BitLocker (Windows)
- Utiliser LUKS (Linux)
- Utiliser FileVault (macOS)

---

### Q25 : Que se passe-t-il si le disque cible est plein 

**R :** La sauvegarde s'arrête avec l'état `Error`.

**Solution** :
- Libérer de l'espace
- Relancer la sauvegarde
- Les fichiers déjà copiés restent intacts

---

### Q26 : EasySave sauvegarde les permissions de fichiers 

**R :** **NON**, les permissions ne sont pas préservées.

Les fichiers sont copiés avec les permissions par défaut du système de fichiers cible.

---

## Maintenance

### Q27 : Comment nettoyer les anciens logs 

**R :**

```bash
# Supprimer les logs
rm -rf ~/.local/share/EasySave/logs  # Linux/macOS
rmdir %PROGRAMDATA%\EasySave\logs    # Windows
```

EasySave recréera le dossier au prochain backup.

---

### Q28 : Puis-je éditer backup_config.json manuellement 

**R :** **Oui, mais avec prudence**.

Format :
```json
[
  {
    "name": "MonBackup",
    "sourcePath": "/source",
    "targetPath": "/target",
    "type": "Full"
  }
]
```

**Règles** :
- Gardez la structure JSON valide
- Utilisez chemins absolus
- Types : `Full` ou `Differential`
- Sauvegarder avant modification

---

### Q29 : Comment réinitialiser EasySave 

**R :**

```bash
# Windows
rmdir %PROGRAMDATA%\EasySave /s

# Linux/macOS
rm -rf ~/.local/share/EasySave
```

Puis relancer l'application pour recréer une configuration vierge.

---

## Développement

### Q30 : Comment contribuer au projet 

**R :**

1. Fork le repository GitHub
2. Créer une branche : `git checkout -b feature/ma-feature`
3. Commit : `git commit -m "Ajout de ma feature"`
4. Push : `git push origin feature/ma-feature`
5. Ouvrir une Pull Request

[https://github.com/joanperreyprato/EasySave](https://github.com/joanperreyprato/EasySave)

---

## Support

### Je n'ai pas trouvé ma réponse

Consultez :
-  **DOCUMENTATION.md** - Documentation complète
-  **QUICK_START.md** - Guide rapide
-  **TECHNICAL_DOCUMENTATION.md** - Architecture technique

Ou ouvrez une issue : [GitHub Issues](https://github.com/joanperreyprato/EasySave/issues)

---

**Dernière mise à jour** : Janvier 2024
**Version** : 1.0

Vous avez une autre question   Contactez-nous sur GitHub !
