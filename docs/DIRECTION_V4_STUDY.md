# 🚀 Étude Prospective & Réponses Direction — Soutenance v3.0 / v4.0

> Document d'aide à la soutenance préparé pour répondre expressément aux demandes de la direction ProSoft (Cahier des charges, lignes 917-925).

---

## 🎯 1. Analyse critique des Sauvegardes Parallèles (v3.0)

### La question de la direction :
> *"Est-il pertinent de faire des sauvegardes en parallèle ?"*

### Notre réponse technique argumentée :

| Type de support de stockage | Pertinence du Parallélisme | Explication technique |
|---|:---:|---|
| **Disque mécanique (HDD)** | ⚠️ **Faible / Contre-productif** | Les têtes de lecture physiques d'un HDD doivent faire des allers-retours incessants (*head thrashing*). Les débits s'écroulent à cause des temps de recherche (*seek times*). |
| **SSD / NVMe** | 🟢 **Très pertinent** | Capacité de lecture/écriture concurrente avec des files de commandes profondes (NVMe queues). Le gain de temps est de 40 % à 70 % sur les gros volumes. |
| **Réseau Local (LAN / NAS)** | 🟡 **Conditionné par la bande passante** | Très pertinent si le réseau est dimensionné (1 Gbps / 10 Gbps). C'est précisément pour cela que nous avons implémenté en v3.0 la règle de limitation des gros fichiers (> n Ko) pour éviter la saturation du lien. |

**Conclusion pour le jury :**  
Le parallélisme est excellent dans un environnement moderne (SSD/Réseau d'entreprise), **à la condition absolue** d'intégrer des mécanismes de régulation comme nos sémaphores de seuil (`LargeFileTransferGuard`) pour ne pas étouffer les entrées/sorties.

---

## 💡 2. Propositions d'évolutions pour la Version 4.0

Voici les 4 axes d'évolution recommandés, analysés sous l'angle **Bénéfice Client vs Temps de Développement** :

```
             Bénéfice Client (Valeur Métier)
                       ▲
                       │     [Axe 1] Planificateur (Cron/Scheduler)
           Elevé       │     [Axe 2] Déduplication & Compression
                       │     
                       │     [Axe 3] Sauvegarde Cloud (S3/Azure Blob)
           Moyen       │     
                       │     [Axe 4] Console Web d'administration
          Faible       │
                       └────────────────────────────────────────►
                             Faible           Moyen           Élevé
                                Temps de Développement (Coût)
```

### Détail des axes d'évolution :

### 1. Planificateur automatique de tâches (Scheduler / Cron intégré)
- **Description** : Permettre à l'utilisateur de programmer ses sauvegardes (toutes les nuits à 2h, chaque vendredi, etc.) sans dépendre du planificateur de tâches Windows.
- **Bénéfice Client** : 🟢 **Élevé** (Automatisation complète, plus besoin de lancer manuellement les jobs).
- **Temps de développement** : 🟢 **Court** (1 à 2 jours avec une bibliothèque comme `Quartz.NET` ou `Cronos`).
- **ROI (Retour sur investissement)** : ⭐⭐⭐⭐⭐ **Priorité #1**.

### 2. Compression d'archives (.zip / .tar.gz) & Déduplication
- **Description** : Compresser les dossiers de destination pour économiser l'espace disque et éviter d'écrire des fichiers en double.
- **Bénéfice Client** : 🟢 **Élevé** (Gain de 50 % à 80 % d'espace disque sur les sauvegardes complètes).
- **Temps de développement** : 🟡 **Moyen** (3 à 5 jours via `System.IO.Compression` et empreintes SHA-256).
- **ROI** : ⭐⭐⭐⭐ **Priorité #2**.

### 3. Connecteurs de Stockage Cloud (AWS S3, Azure Blob Storage, SFTP)
- **Description** : Permettre d'envoyer la sauvegarde directement vers un bucket Cloud distant ou un serveur SFTP sécurisé.
- **Bénéfice Client** : 🟢 **Très Élevé** (Plan de Continuité d'Activité / reprise après sinistre incendie/ransomware).
- **Temps de développement** : 🟡 **Moyen à Élevé** (1 à 2 semaines pour gérer les API REST et les reprises sur incident réseau).
- **ROI** : ⭐⭐⭐⭐.

### 4. Interface d'administration Web (Dashboard Blazor / REST API)
- **Description** : Remplacer ou compléter l'interface WPF par un tableau de bord Web accessible sur le réseau interne pour administrer tous les agents EasySave déployés.
- **Bénéfice Client** : 🟡 **Moyen** (Confort d'administration pour les équipes DSI).
- **Temps de développement** : 🔴 **Élevé** (2 à 3 semaines pour sécuriser les API et concevoir l'interface Web).
- **ROI** : ⭐⭐⭐.

---

## 🎤 Synthèse pour votre présentation orale

1. **Sur le parallélisme** :
   > *"Nous avons démontré qu'exécuter les sauvegardes en parallèle réduit drastiquement les délais sur supports SSD et stockage réseau, tout en garantissant la fluidité grâce à notre régulateur de bande passante sur les fichiers volumineux."*

2. **Sur la version 4.0** :
   > *"Pour la v4.0, notre étude bénéfice/coût place la planification automatique (Scheduler) et la compression comme les deux chantiers les plus rentables à court terme pour ProSoft, avant d'aborder les connecteurs Cloud dans une phase ultérieure."*
