# 🚀 EasySave Distributed Logging System

## Vue d'ensemble

Ce projet ajoute un système de logging distribué à EasySave, permettant de centraliser les logs de toutes les installations EasySave vers un serveur Docker unique.

## 📁 Architecture

```
EasySave/
├── EasySave.LogServer/          # Nouveau projet serveur
│   ├── LogServerService.cs      # Service de réception des logs
│   ├── Program.cs               # Point d'entrée
│   ├── Dockerfile               # Configuration Docker
│   └── README.md                # Documentation du serveur
├── EasyLog/
│   ├── SocketLogger.cs          # Nouveau logger pour envoi réseau
│   ├── CompositeLogger.cs       # Logger composite (local + distant)
│   └── LoggerFactory.cs         # Factory pour créer les loggers
├── EasySave.Core/
│   └── AppConfig.cs             # Configuration de l'application
├── EasySave.WPF/
│   └── appsettings.json         # Configuration du client
├── docker-compose.yml           # Configuration Docker Compose
├── start-logserver.ps1          # Script de démarrage Windows
└── start-logserver.sh           # Script de démarrage Linux/Mac
```

## 🎯 Fonctionnalités

### Serveur de Logs (EasySave.LogServer)

- ✅ Serveur TCP asynchrone écoutant sur le port 5555 (configurable)
- ✅ Gestion de multiples clients simultanés
- ✅ Organisation automatique des logs par client et par jour
- ✅ Validation JSON des entrées
- ✅ Logs détaillés en temps réel
- ✅ Containerisé avec Docker
- ✅ Configuration via variables d'environnement

### Client (EasySave.WPF)

- ✅ Interface UI pour configurer le logging distant
- ✅ Test de connexion au serveur
- ✅ Option pour conserver les logs locaux en plus du distant
- ✅ Configuration persistante via `appsettings.json`
- ✅ Basculement automatique vers logging local si le serveur est indisponible

## 🔧 Installation et Configuration

### 1. Démarrer le serveur de logs

#### Option A : Windows (PowerShell)
```powershell
.\start-logserver.ps1
```
Sélectionnez l'option 1 pour démarrer le serveur.

#### Option B : Linux/Mac
```bash
chmod +x start-logserver.sh
./start-logserver.sh
```
Sélectionnez l'option 1 pour démarrer le serveur.

#### Option C : Manuellement avec Docker Compose
```bash
docker-compose up -d
```

#### Option D : Sans Docker (développement)
```bash
cd EasySave.LogServer
dotnet run
```

### 2. Configurer les clients EasySave

#### Via l'interface graphique (Recommandé)

1. Ouvrir EasySave
2. Aller dans l'onglet **Paramètres**
3. Dans la section **"🌐 Serveur de logs distant"** :
   - Cocher **"Activer le logging distant"**
   - Entrer l'adresse du serveur (ex: `192.168.1.100` ou `localhost`)
   - Entrer le port (par défaut : `5555`)
   - Optionnel : Cocher **"Conserver également les logs locaux"**
4. Cliquer sur **"🔌 Tester la connexion"**
5. Si le test réussit, la configuration est automatiquement enregistrée
6. Fermer et rouvrir EasySave pour appliquer les changements

#### Via le fichier de configuration

Modifier `EasySave.WPF/appsettings.json` :

```json
{
  "RemoteLogging": {
    "Enabled": true,
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

## 📊 Utilisation

### Visualiser les logs du serveur en temps réel

```bash
# Voir tous les logs
docker logs -f easysave-logserver

# Voir les dernières lignes
docker logs --tail 50 easysave-logserver
```

### Structure des logs sur le serveur

Les logs sont organisés automatiquement :

```
logs/
├── 192.168.1.100/              # Client identifié par IP
│   ├── 2024-01-15.json
│   └── 2024-01-16.json
├── LAPTOP-USER1/               # Client identifié par nom machine
│   ├── 2024-01-15.json
│   └── 2024-01-16.json
└── DESKTOP-OFFICE/
    ├── 2024-01-15.json
    └── 2024-01-16.json
```

### Exemple de log reçu

```json
{
  "timestamp": "2024-01-15T14:30:25.123Z",
  "backupName": "Documents_Backup",
  "sourceFilePath": "C:\\Users\\John\\Documents\\report.pdf",
  "targetFilePath": "D:\\Backups\\Documents\\report.pdf",
  "fileSize": 2048576,
  "transferTime": 145,
  "encryptionTime": 0,
  "type": "File",
  "machineName": "LAPTOP-USER1"
}
```

## 🐳 Gestion du serveur Docker

### Démarrer le serveur
```bash
docker-compose up -d
```

### Arrêter le serveur
```bash
docker-compose down
```

### Redémarrer le serveur
```bash
docker-compose restart
```

### Rebuild complet
```bash
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### Voir le statut
```bash
docker ps | grep easysave
```

### Accéder au shell du conteneur
```bash
docker exec -it easysave-logserver /bin/bash
```

## 🔐 Configuration avancée

### Changer le port du serveur

#### Dans docker-compose.yml
```yaml
services:
  easysave-logserver:
    ports:
      - "8888:5555"  # Host:Container
    environment:
      - LOG_SERVER_PORT=5555  # Port interne
```

#### Ou via variable d'environnement
```bash
docker run -p 8888:5555 -e LOG_SERVER_PORT=5555 easysave-logserver
```

### Changer le répertoire de logs

```yaml
services:
  easysave-logserver:
    environment:
      - LOG_SERVER_DIRECTORY=/app/logs
    volumes:
      - /mon/chemin/custom:/app/logs
```

### Déploiement sur un serveur distant

1. Sur le serveur, cloner le repo et démarrer le serveur :
```bash
git clone https://github.com/votre-repo/EasySave.git
cd EasySave
docker-compose up -d
```

2. Ouvrir le port dans le firewall :
```bash
# Ubuntu/Debian
sudo ufw allow 5555/tcp

# CentOS/RHEL
sudo firewall-cmd --permanent --add-port=5555/tcp
sudo firewall-cmd --reload
```

3. Noter l'IP du serveur :
```bash
hostname -I
# ou
ip addr show
```

4. Sur les clients, configurer l'IP du serveur dans EasySave

## 🧪 Tests

### Test de connexion depuis un client

#### Avec PowerShell (Windows)
```powershell
Test-NetConnection -ComputerName <server-ip> -Port 5555
```

#### Avec telnet
```bash
telnet <server-ip> 5555
```

#### Avec netcat
```bash
nc -zv <server-ip> 5555
```

### Envoyer un log de test

```bash
echo '{"timestamp":"2024-01-15T10:00:00Z","backupName":"Test","sourceFilePath":"test.txt","targetFilePath":"backup.txt","fileSize":1024,"transferTime":10,"encryptionTime":0,"type":"File","machineName":"TEST"}' | nc localhost 5555
```

## 📈 Monitoring et Maintenance

### Statistiques du serveur

```bash
# Nombre de connexions actives
docker exec easysave-logserver netstat -an | grep 5555 | grep ESTABLISHED | wc -l

# Utilisation des ressources
docker stats easysave-logserver

# Taille des logs
du -sh logs/
```

### Rotation des logs

Créer un script de rotation automatique :

```bash
# rotate-logs.sh
#!/bin/bash
find ./logs -name "*.json" -mtime +30 -delete
```

Ajouter au cron :
```bash
0 2 * * * /chemin/vers/rotate-logs.sh
```

### Backup des logs

```bash
# Backup quotidien
tar -czf logs-backup-$(date +%Y%m%d).tar.gz logs/

# Envoyer vers un stockage distant
rsync -avz logs/ user@backup-server:/backups/easysave-logs/
```

## 🔍 Troubleshooting

### Le serveur ne démarre pas

1. Vérifier que Docker est en cours d'exécution :
```bash
docker info
```

2. Vérifier que le port n'est pas déjà utilisé :
```bash
# Windows
netstat -ano | findstr :5555

# Linux/Mac
lsof -i :5555
```

3. Voir les logs d'erreur :
```bash
docker logs easysave-logserver
```

### Les clients ne peuvent pas se connecter

1. Vérifier le firewall du serveur
2. Vérifier que le port est bien exposé :
```bash
docker port easysave-logserver
```

3. Tester la connectivité réseau :
```bash
ping <server-ip>
telnet <server-ip> 5555
```

4. Vérifier la configuration client dans `appsettings.json`

### Les logs ne sont pas reçus

1. Vérifier que le client a bien activé le remote logging
2. Tester la connexion depuis l'interface EasySave
3. Vérifier les logs du serveur :
```bash
docker logs -f easysave-logserver
```

4. Vérifier que le SocketLogger est bien initialisé :
   - Redémarrer l'application EasySave après avoir modifié la config

## 🚀 Déploiement en Production

### Recommandations

1. **Sécurité**
   - Utiliser un VPN ou SSH tunnel pour les connexions distantes
   - Implémenter un système d'authentification (token)
   - Activer TLS/SSL pour chiffrer les communications

2. **Scalabilité**
   - Déployer plusieurs instances avec un load balancer
   - Utiliser une base de données pour stocker les logs (PostgreSQL, MongoDB)
   - Implémenter un système de queue (RabbitMQ, Kafka)

3. **Monitoring**
   - Utiliser Prometheus pour collecter les métriques
   - Utiliser Grafana pour visualiser les données
   - Configurer des alertes pour les problèmes critiques

4. **Backup**
   - Automatiser les backups quotidiens
   - Stocker les backups sur un système distant
   - Tester régulièrement la restauration

### Exemple de configuration production avec nginx (reverse proxy)

```nginx
upstream logserver {
    server localhost:5555;
}

server {
    listen 443 ssl;
    server_name logs.easysave.example.com;
    
    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    
    location / {
        proxy_pass http://logserver;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        
        # Whitelist d'IPs autorisées
        allow 192.168.1.0/24;
        deny all;
    }
}
```

## 📝 Évolutions futures

- [ ] Authentification avec token
- [ ] Support TLS/SSL
- [ ] Interface web pour visualiser les logs
- [ ] API REST pour interroger les logs
- [ ] Dashboard en temps réel
- [ ] Alertes par email/SMS
- [ ] Compression des logs anciens
- [ ] Export vers Elasticsearch/Splunk
- [ ] Support de multiples formats de log (XML, CSV)
- [ ] Rate limiting pour éviter les abus

## 🤝 Contribution

Pour contribuer au projet :

1. Fork le repository
2. Créer une branche feature (`git checkout -b feature/AmazingFeature`)
3. Commit les changements (`git commit -m 'Add some AmazingFeature'`)
4. Push vers la branche (`git push origin feature/AmazingFeature`)
5. Ouvrir une Pull Request

## 📄 License

Ce projet est sous licence MIT. Voir le fichier LICENSE pour plus de détails.

## 📧 Support

Pour toute question ou problème :
- Ouvrir une issue sur GitHub
- Consulter la documentation technique
- Contacter l'équipe de support

---

**Version:** 1.0  
**Dernière mise à jour:** Janvier 2024  
**Auteur:** Équipe EasySave
