# EasySave Log Server - Docker Edition

## 🚀 Quick Start

### Prérequis
- Docker Desktop installé
- Docker Compose installé

### Lancement du serveur

#### Option 1 : Avec Docker Compose (Recommandé)
```bash
# À la racine du projet
docker-compose up -d
```

#### Option 2 : Build et run manuel
```bash
# Build l'image
docker build -f EasySave.LogServer/Dockerfile -t easysave-logserver:latest .

# Run le conteneur
docker run -d \
  --name easysave-logserver \
  -p 5555:5555 \
  -v $(pwd)/logs:/app/logs \
  -e LOG_SERVER_PORT=5555 \
  -e LOG_SERVER_DIRECTORY=/app/logs \
  easysave-logserver:latest
```

### Vérifier les logs du serveur
```bash
# Voir les logs en temps réel
docker logs -f easysave-logserver

# Voir les dernières 100 lignes
docker logs --tail 100 easysave-logserver
```

### Arrêter le serveur
```bash
# Avec docker-compose
docker-compose down

# Ou directement
docker stop easysave-logserver
docker rm easysave-logserver
```

## 📁 Structure des logs

Les logs sont organisés par client :
```
logs/
├── 192.168.1.100/
│   ├── 2024-01-15.json
│   └── 2024-01-16.json
├── 192.168.1.101/
│   ├── 2024-01-15.json
│   └── 2024-01-16.json
└── WORKSTATION-001/
    ├── 2024-01-15.json
    └── 2024-01-16.json
```

## ⚙️ Configuration

### Variables d'environnement

| Variable | Description | Défaut |
|----------|-------------|--------|
| `LOG_SERVER_PORT` | Port d'écoute du serveur | `5555` |
| `LOG_SERVER_DIRECTORY` | Répertoire de stockage des logs | `/app/logs` |

### Configuration réseau

Pour que les clients EasySave se connectent au serveur :

1. **Sur le serveur** : Noter l'IP du serveur
```bash
# Linux/Mac
hostname -I

# Windows
ipconfig
```

2. **Sur les clients** : Configurer l'adresse du serveur dans EasySave
   - Dans les paramètres, activer "Remote Logging"
   - Entrer l'IP du serveur et le port (ex: `192.168.1.50:5555`)

## 🔧 Maintenance

### Nettoyer les anciens logs
```bash
# Supprimer les logs de plus de 30 jours
find ./logs -name "*.json" -mtime +30 -delete
```

### Backup des logs
```bash
# Créer une archive des logs
tar -czf logs-backup-$(date +%Y%m%d).tar.gz logs/
```

### Mise à jour du serveur
```bash
# Rebuild et redéployer
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

## 🐛 Troubleshooting

### Le serveur ne démarre pas
1. Vérifier que le port 5555 n'est pas déjà utilisé :
```bash
# Windows
netstat -ano | findstr :5555

# Linux/Mac
lsof -i :5555
```

2. Vérifier les logs Docker :
```bash
docker logs easysave-logserver
```

### Les clients ne peuvent pas se connecter
1. Vérifier le firewall du serveur
2. Vérifier que le port est bien exposé :
```bash
docker ps
```
3. Tester la connectivité :
```bash
telnet <server-ip> 5555
```

## 📊 Monitoring

### Statistiques en temps réel
```bash
# Nombre de connexions actives
docker exec easysave-logserver dotnet --info

# Utilisation des ressources
docker stats easysave-logserver
```

### Analyse des logs
```bash
# Compter les logs reçus aujourd'hui
find ./logs -name "$(date +%Y-%m-%d).json" -exec wc -l {} +

# Voir les derniers logs
tail -f ./logs/*/$(date +%Y-%m-%d).json
```

## 🔐 Sécurité

### Recommandations pour la production

1. **Utiliser HTTPS/TLS** : Modifier le code pour utiliser `SslStream`
2. **Authentification** : Ajouter un token d'authentification
3. **Limiter les IPs** : Utiliser un reverse proxy (nginx) pour filtrer
4. **Rotation des logs** : Implémenter une rotation automatique

### Exemple de configuration sécurisée avec nginx

```nginx
server {
    listen 5555 ssl;
    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    
    location / {
        proxy_pass http://easysave-logserver:5555;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        
        # Whitelist d'IPs
        allow 192.168.1.0/24;
        deny all;
    }
}
```

## 📈 Scalabilité

Pour un grand nombre de clients, considérez :

1. **Load Balancer** : Déployer plusieurs instances du serveur
2. **Base de données** : Stocker les logs dans une DB (PostgreSQL, MongoDB)
3. **Message Queue** : Utiliser RabbitMQ ou Kafka pour gérer les logs
4. **Monitoring** : Prometheus + Grafana pour visualiser les métriques

## 🤝 Support

Pour des questions ou problèmes :
- Consulter la documentation technique
- Ouvrir une issue sur GitHub
- Contacter l'équipe de support
