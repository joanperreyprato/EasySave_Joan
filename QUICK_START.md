# EasySave - Guide Rapide

Commencez avec EasySave en 5 minutes ! 

---

## ? Installation rapide

### Windows
```bash
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
dotnet build
dotnet run
```

### Linux / macOS
```bash
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
dotnet build
dotnet run
```

---

##  Premiers pas

### 1. Lancer en mode interactif
```bash
dotnet run
```

Menu :
```
=== EasySave ===
1. Create Backup
2. Execute Backup
3. List Backups
4. Exit
Select an option:
```

### 2. Créer votre première sauvegarde
- Appuyez sur **1**
- Entrez un nom : `MonBackup`
- Entrez le dossier source : `/home/user/Documents`
- Entrez le dossier cible : `/home/user/Backups`
- Choisissez le type : `full` (ou `diff` pour différentiel)

### 3. Exécuter la sauvegarde
- Appuyez sur **2**
- Sélectionnez l'index : `0`
- C'est parti ! ?

### 4. Lister vos sauvegardes
- Appuyez sur **3**
- Voyez vos tâches configurées

---

## ? Mode ligne de commande

### Créer rapidement
```bash
dotnet run -- create "DocBackup" "C:\Users\Doc" "D:\Backup" full
```

### Exécuter rapidement
```bash
# Une seule sauvegarde
dotnet run -- execute 0

# Plusieurs sauvegardes
dotnet run -- execute 0-2
dotnet run -- execute 0,2,4
```

### Lister
```bash
dotnet run -- list
```

---

##  Types de sauvegarde

| Type | Première fois | Utilisation |
|------|---------------|-------------|
| **Full** | ? À faire | Sauvegarde complète (tous fichiers) |
| **Diff** | ? Trop long | Mise à jour (fichiers modifiés seulement) |

**Conseil** : Faites une `full` au départ, puis utilisez `diff` pour les suivantes.

---

##  Configuration

Fichiers créés automatiquement dans :
- **Windows** : `C:\ProgramData\EasySave\`
- **Linux/macOS** : `~/.local/share/EasySave/`

| Fichier | Contenu |
|---------|---------|
| `backup_config.json` | Liste vos tâches |
| `state.json` | Progression en cours |
| `logs/` | Historique |

---

##  Erreurs courantes

| Problème | Solution |
|----------|----------|
| "Source directory does not exist" | Vérifiez le chemin source |
| "Target directory does not exist" | Créez le dossier de destination |
| "Invalid job name" | Utilisez alphanumérique + tiret/underscore |
| "A backup job with this name already exists" | Changez le nom |
| "Maximum number of backup jobs (5) reached" | Supprimez une ancienne sauvegarde |

---

##  Changer la langue

Modifiez `Program.cs` ligne 2 :

```csharp
// Français
var language = new LanguageProvider("fr");

// Anglais
var language = new LanguageProvider("en");
```

Recompiler :
```bash
dotnet build
dotnet run
```

---

##  Exemples pratiques

### Exemple 1 : Sauvegarder une partition
```bash
dotnet run -- create "PartitionD" "D:\" "E:\BackupD" full
dotnet run -- execute 0
```

### Exemple 2 : Sauvegardes régulières
```bash
# Jour 1 : Sauvegarde complète
dotnet run -- create "Weekly-Full" "/data" "/backup/weekly" full
dotnet run -- execute 0

# Jour 8 : Différentiel rapide
dotnet run -- create "Weekly-Update" "/data" "/backup/weekly" diff
dotnet run -- execute 1
```

### Exemple 3 : Batch exécution
```bash
# Créer plusieurs sauvegardes
dotnet run -- create "Photos" "/home/photos" "/backup/photos" full
dotnet run -- create "Videos" "/home/videos" "/backup/videos" full
dotnet run -- create "Documents" "/home/docs" "/backup/docs" full

# Les exécuter toutes
dotnet run -- execute 0-2
```

---

##  Suivi de progression

Pendant une sauvegarde, l'application met à jour :
- Fichiers traités
- Taille transférée
- Pourcentage complété
- Temps écoulé

Vérifiez dans `state.json` :
```json
{
  "progress": 45.5,
  "remainingFiles": 55,
  "remainingSize": 524288000
}
```

---

##  Dépannage rapide

### Réinstaller
```bash
dotnet clean
dotnet build
```

### Réinitialiser
Supprimez `%ProgramData%\EasySave\` ou `~/.local/share/EasySave/`

### Vérifier les logs
Consultez le dossier `logs/` pour l'historique

---

## ? Checklist premier backup

- [ ] Compiler : `dotnet build`
- [ ] Lancer : `dotnet run`
- [ ] Créer sauvegarde (menu 1)
- [ ] Spécifier source ET cible
- [ ] Type = `full`
- [ ] Exécuter (menu 2)
- [ ] Vérifier fichiers copiés ?
- [ ] Lister (menu 3) pour voir résumé

---

##  Besoin d'aide ?

- **Bug** : [Signaler sur GitHub](https://github.com/joanperreyprato/EasySave/issues)
- **Question** : Voir `DOCUMENTATION.md`
- **Technique** : Voir `TECHNICAL_DOCUMENTATION.md`

---

**Version** : 1.0 | **Mise à jour** : Janvier 2024

 Vous êtes prêt ! Commencez votre première sauvegarde maintenant !
