# EasySave 2.0 - FAQ (Français)

Questions fréquemment posées sur EasySave 2.0

---

## Installation & Configuration

### Q1: Quels sont les prérequis ?
**.NET 8.0** ou supérieur. Windows pour WPF, tous OS pour CLI.

### Q2: Comment installer ?
```bash
git clone https://github.com/joanperreyprato/EasySave.git
cd EasySave
dotnet build
```

### Q3: Où sont stockés les fichiers de configuration ?
**Windows** : `C:\ProgramData\EasySave\`

---

## Utilisation

### Q4: Comment créer une sauvegarde en WPF ?
**Add job** ? Remplir nom/source/destination ? **OK**

### Q5: Comment exécuter plusieurs sauvegardes ?
Sélectionnez-les (Ctrl+Clic) puis **Start selected**, ou **Start all** pour toutes.

### Q6: Puis-je annuler une sauvegarde en cours ?
Non, cette fonctionnalité n'est pas implémentée actuellement.

### Q7: Comment créer une sauvegarde en CLI ?
```bash
dotnet run -- create "Nom" "C:\Source" "D:\Dest" full
```

---

## Types de Sauvegarde

### Q8: Quelle différence entre Full et Differential ?
- **Full** : Copie tous les fichiers
- **Differential** : Copie uniquement les fichiers nouveaux/modifiés

### Q9: Quand utiliser Full vs Differential ?
**Full** pour la première fois ou périodiquement. **Differential** pour les mises à jour quotidiennes.

### Q10: Comment Differential détecte les modifications ?
Compare la date de modification et la taille des fichiers.

---

## Chiffrement

### Q11: Comment activer le chiffrement ?
1. Onglet **Chiffrement** ? Entrer une clé
2. Ajouter extensions (`.docx`, `.pdf`)
3. Lors de l'exécution, choisir "Oui"

### Q12: Quel algorithme est utilisé ?
**AES-256-CBC** avec vérification d'intégrité **HMAC-SHA256**.

### Q13: Comment déchiffrer un fichier ?
Utilisez `CryptoService.DecryptFile(input, output, key)` du projet CryptoSoft.

### Q14: Puis-je chiffrer seulement certains fichiers ?
Oui, ajoutez uniquement les extensions voulues dans la liste de filtrage.

---

## Configuration & Logs

### Q15: Où sont les logs ?
`C:\ProgramData\EasySave\logs\`

### Q16: JSON ou XML pour les logs ?
**JSON** : Plus rapide et compact. **XML** : Compatible avec outils legacy.

### Q17: Comment réinitialiser la configuration ?
Supprimez `C:\ProgramData\EasySave\` et relancez l'application.

---

## Performance

### Q18: Ma sauvegarde est lente, pourquoi ?
- Disque lent
- Nombreux fichiers
- Chiffrement activé

**Solution** : Utilisez Differential après Full, désactivez le chiffrement si inutile.

### Q19: Combien de travaux puis-je créer ?
Maximum **5 travaux** (modifiable dans le code si besoin).

---

## Erreurs

### Q20: "Source path invalid"
Le chemin source n'existe pas ou est mal formaté. Vérifiez le chemin.

### Q21: "Encryption key missing"
Vous avez demandé le chiffrement sans définir de clé. Allez dans l'onglet Chiffrement.

### Q22: Erreur pendant la copie de fichier
L'application continue avec les fichiers suivants. Consultez les logs (`transferTime: -1`).

---

## Divers

### Q23: Comment changer la langue ?
**WPF** : Menu déroulant en haut à droite. **CLI** : Menu option 5.

### Q24: Puis-je sauvegarder vers un réseau ?
Oui, si le chemin réseau est monté et accessible (ex: `\\serveur\partage`).

### Q25: L'application fonctionne-t-elle hors ligne ?
Oui, aucune connexion Internet requise.

---

## Support

### Q26: Où trouver plus d'aide ?
- [DOCUMENTATION.md](DOCUMENTATION.md) - Guide complet
- [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md) - Documentation technique
- [GitHub Issues](https://github.com/joanperreyprato/EasySave/issues) - Signaler un bug

---

**Version** : 2.0 | **Dernière mise à jour** : Janvier 2025
