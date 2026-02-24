# 🚀 Quick Start - EasySave Distributed Logging

## Démarrage Rapide (5 minutes)

### Étape 1 : Démarrer le serveur de logs

#### Windows
```powershell
.\start-logserver.ps1
```
Choisir l'option **1** (Start server)

#### Linux/Mac
```bash
chmod +x start-logserver.sh
./start-logserver.sh
```
Choisir l'option **1** (Start server)

### Étape 2 : Configuration du client

1. **Lancer EasySave**
2. **Aller dans l'onglet "Paramètres"**
3. **Dans la section "🌐 Serveur de logs distant" :**
   - ☑️ Cocher "Activer le logging distant"
   - 📝 Adresse du serveur : `localhost` (ou l'IP du serveur distant)
   - 📝 Port : `5555`
   - ☑️ Cocher "Conserver également les logs locaux" (recommandé)
4. **Cliquer sur "🔌 Tester la connexion"**
5. **Fermer et rouvrir EasySave**

### Étape 3 : Vérifier que ça fonctionne

1. Créer une sauvegarde dans EasySave et la lancer
2. Voir les logs en temps réel :
```bash
docker logs -f easysave-logserver
```

### Étape 4 : Voir les logs enregistrés

Les logs sont dans le dossier `logs/` à la racine du projet :
```
logs/
├── VOTRE-PC/
│   └── 2024-01-15.json
```

---

## 📚 Documentation Complète

Pour plus d'informations, consultez :
- [Guide complet du système de logging distribué](DISTRIBUTED_LOGGING_GUIDE.md)
- [README du serveur de logs](EasySave.LogServer/README.md)

## 🐛 Problèmes ?

### Le serveur ne démarre pas
- Assurez-vous que Docker Desktop est lancé
- Vérifiez que le port 5555 n'est pas déjà utilisé

### Le client ne se connecte pas
1. Vérifiez que le serveur est démarré : `docker ps`
2. Testez la connexion : `telnet localhost 5555`
3. Vérifiez le firewall

### Besoin d'aide ?
Consultez la section Troubleshooting dans [DISTRIBUTED_LOGGING_GUIDE.md](DISTRIBUTED_LOGGING_GUIDE.md#-troubleshooting)

---

**Prochaines étapes :**
- Déployer le serveur sur un serveur distant
- Configurer plusieurs clients EasySave
- Mettre en place des backups automatiques des logs
