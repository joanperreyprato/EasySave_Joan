#  EasySave - Index de Documentation

Bienvenue dans la documentation complète d'EasySave ! Trouvez rapidement ce que vous cherchez.

---

##  Commencer rapidement

### Pour les utilisateurs impatients
 **[QUICK_START.md](QUICK_START.md)** - 5 minutes pour votre première sauvegarde
- Installation en 3 commandes
- Premier backup en mode interactif
- Premiers essais en CLI

### Vous êtes pressé ?
 **Commande pour démarrer** :
```bash
git clone https://github.com/joanperreyprato/EasySave.git && cd EasySave && dotnet run
```

---

##  Documentation complète

### Pour les utilisateurs réguliers
 **[DOCUMENTATION.md](DOCUMENTATION.md)** - Guide complet et détaillé
- Caractéristiques complètes
- Installation détaillée
- Utilisation (mode interactif + CLI)
- Types de sauvegarde expliqués
- Configuration & fichiers
- Codes d'erreur
- Exemples d'utilisation
- Guide de dépannage

**Parfait pour** : Utilisateurs qui découvrent EasySave

---

## ? FAQ - Questions fréquentes

### Vous vous posez une question ?
 **[FAQ.md](FAQ.md)** - 30 questions/réponses
- Installation & configuration
- Création de sauvegardes
- Types de sauvegarde
- Exécution & suivi
- Dépannage complet
- Performance
- Sécurité
- Maintenance

**Parfait pour** : Trouver rapidement une réponse

---

##  Documentation technique

### Pour les développeurs
 **[TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md)** - Spécifications techniques
- Architecture complète avec diagrammes UML
- Description détaillée de chaque composant
- Flux d'exécution
- Système de traduction (i18n)
- Gestion d'état
- Validation des données
- Patterns utilisés (DI, Factory, etc.)
- Points d'extensibilité
- Recommandations de tests

**Parfait pour** : Contributeurs, mainteneurs, développeurs

---

##  Vue d'ensemble par thème

### Installation & Configuration
| Question | Document |
|----------|----------|
| Comment installer ? | [QUICK_START.md](QUICK_START.md) |
| Où sont les fichiers ? | [FAQ.md - Q2](FAQ.md) |
| Comment changer la langue ? | [FAQ.md - Q3](FAQ.md) |
| Installation détaillée | [DOCUMENTATION.md](DOCUMENTATION.md) |

### Utilisation
| Question | Document |
|----------|----------|
| Premier backup | [QUICK_START.md](QUICK_START.md) |
| Mode interactif | [DOCUMENTATION.md](DOCUMENTATION.md) |
| Mode ligne de commande | [DOCUMENTATION.md](DOCUMENTATION.md) |
| Exemples concrets | [QUICK_START.md](QUICK_START.md) |

### Sauvegardes
| Question | Document |
|----------|----------|
| Full vs Diff ? | [FAQ.md - Q8](FAQ.md) |
| Types de sauvegarde | [DOCUMENTATION.md](DOCUMENTATION.md) |
| Combien de sauvegardes ? | [FAQ.md - Q5](FAQ.md) |
| Exécuter plusieurs | [FAQ.md - Q10](FAQ.md) |

### Erreurs & Dépannage
| Erreur | Solution |
|--------|----------|
| "Invalid job name" | [FAQ.md - Q16](FAQ.md) |
| "Source not found" | [FAQ.md - Q17](FAQ.md) |
| "Target not found" | [FAQ.md - Q18](FAQ.md) |
| Nom en doublon | [FAQ.md - Q19](FAQ.md) |
| Max atteint | [FAQ.md - Q20](FAQ.md) |
| Aucun fichier copié | [FAQ.md - Q21](FAQ.md) |
| Dépannage complet | [DOCUMENTATION.md](DOCUMENTATION.md) |

### Performance & Sécurité
| Sujet | Document |
|-------|----------|
| Accélérer sauvegarde | [FAQ.md - Q22](FAQ.md) |
| Espace disque | [FAQ.md - Q23](FAQ.md) |
| Chiffrement | [FAQ.md - Q24](FAQ.md) |
| Permissions | [FAQ.md - Q26](FAQ.md) |
| Disque plein | [FAQ.md - Q25](FAQ.md) |

### Développement
| Sujet | Document |
|-------|----------|
| Architecture | [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md) |
| Composants | [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md) |
| Flux d'exécution | [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md) |
| Extensibilité | [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md) |
| Patterns | [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md) |
| Contribution | [FAQ.md - Q30](FAQ.md) |

---

##  Chemins de lecture recommandés

### Je suis nouveau sur EasySave
```
1. QUICK_START.md          (5 min)
2. DOCUMENTATION.md        (30 min)
3. FAQ.md                  (consulter au besoin)
```

### J'ai une question spécifique
```
1. FAQ.md (chercher Q+numéro)
2. Si non trouvé ? DOCUMENTATION.md
3. Si technique ? TECHNICAL_DOCUMENTATION.md
```

### Je veux contribuer
```
1. QUICK_START.md          (installation)
2. TECHNICAL_DOCUMENTATION.md (comprendre l'archi)
3. Code source             (lire + modifier)
4. Tests                   (valider)
```

### Je rencontre un problème
```
1. FAQ.md - Section Dépannage
2. DOCUMENTATION.md - Section Dépannage
3. Vérifier backup_config.json et state.json
4. Voir les logs dans le dossier logs/
```

---

##  Sommaires détaillés

### QUICK_START.md
- Installation rapide
- Premiers pas
- Mode ligne de commande
- Types de sauvegarde
- Configuration
- Erreurs courantes
- Exemples pratiques
- Suivi progression
- Dépannage rapide
- Checklist

### DOCUMENTATION.md
- Caractéristiques
- Installation détaillée
- Utilisation (mode interactif + CLI)
- Types de sauvegarde
- Architecture
- Configuration
- Codes d'erreur
- État des tâches
- Validation des entrées
- Exemples d'utilisation
- Dépannage complet

### TECHNICAL_DOCUMENTATION.md
- Architecture générale (diagrammes UML)
- Description de chaque composant
- Flux d'exécution (diagrammes)
- Système de traduction (i18n)
- Gestion d'état (cycles de vie)
- Validation des données
- Gestion des erreurs
- Patterns utilisés (DI, Interfaces, Enums)
- Points d'extensibilité
- Recommandations de tests

### FAQ.md
- **Installation & Configuration** (Q1-Q3)
- **Création de sauvegardes** (Q4-Q7)
- **Types de sauvegarde** (Q8-Q10)
- **Exécution & Suivi** (Q11-Q13)
- **Mode CLI** (Q14-Q15)
- **Dépannage** (Q16-Q21)
- **Performance & Optimisation** (Q22-Q23)
- **Sécurité & Fiabilité** (Q24-Q26)
- **Maintenance** (Q27-Q29)
- **Développement** (Q30)

---

##  Liens utiles

### Ressources externes
- **GitHub** : [joanperreyprato/EasySave](https://github.com/joanperreyprato/EasySave)
- **.NET** : [dotnet.microsoft.com](https://dotnet.microsoft.com)
- **Git** : [git-scm.com](https://git-scm.com)

### Dans le projet
- **Source code** : `EasySave v1.0/` (fichiers .cs)
- **Tests** : (à ajouter)
- **Configuration** : `%ProgramData%\EasySave\` ou `~/.local/share/EasySave/`

---

##  Conseils de lecture

### Pour des réponses rapides
-  **5 min** ? QUICK_START.md
-  **2 min** ? FAQ.md (chercher votre question)
-  **Instantané** ? Ctrl+F dans le fichier

### Pour comprendre le projet
-  **30 min** ? DOCUMENTATION.md
-  **60 min** ? TECHNICAL_DOCUMENTATION.md

### Pour contribuer
-  **90 min** ? Tout lire + explorer code
-  **30 min** ? TECHNICAL_DOCUMENTATION.md + code

---

##  Support & Contributions

### Besoin d'aide ?
1. Consulter FAQ.md
2. Lire DOCUMENTATION.md (section Dépannage)
3. Ouvrir une issue sur GitHub

### Vous trouvez un bug ?
[Signaler sur GitHub](https://github.com/joanperreyprato/EasySave/issues/new)

### Vous avez une idée ?
[Discuter sur GitHub Discussions](https://github.com/joanperreyprato/EasySave/discussions)

### Vous voulez contribuer ?
Voir FAQ.md - Q30 (Contribution)

---

##  Statistiques documentation

| Document | Pages | Temps lecture | Pour qui |
|----------|-------|----------------|----------|
| QUICK_START.md | 3 | 5 min | Tous |
| DOCUMENTATION.md | 8 | 30 min | Utilisateurs |
| TECHNICAL_DOCUMENTATION.md | 10 | 60 min | Développeurs |
| FAQ.md | 7 | Variable | Tous |
| **TOTAL** | **28 pages** | **~2 heures** | - |

---

##  Ordre de lecture recommandé

**Pour démarrer** :
```
1 QUICK_START.md
2 Tester l'application
3 DOCUMENTATION.md (si besoin)
4 FAQ.md (consulter au besoin)
```

**Pour explorer** :
```
1 DOCUMENTATION.md (complet)
2 Lire code source (comprendre)
3 TECHNICAL_DOCUMENTATION.md (approfondissement)
```

**Pour développer** :
```
1 QUICK_START.md (setup)
2 TECHNICAL_DOCUMENTATION.md (archi)
3 Explorer code (détails)
4 Modifier + Tester
```

---

## ? Checklist de documentation

Documentation complète inclus :
- ? Guide rapide (5 min)
- ? Guide utilisateur complet
- ? FAQ (30 questions)
- ? Documentation technique
- ? Exemples pratiques
- ? Guide dépannage
- ? Index de navigation (ce fichier)

---

**Dernière mise à jour** : Janvier 2024
**Version** : 1.0
**Statut** : Documentation complète ?

 **Vous avez tout ce qu'il faut pour commencer !**

Choisissez votre document ci-dessus et lancez-vous ! 
