# ROLL'D Backend

Monorepo backend pour le jeu ROLL'D — architecture microservices.

## Services

| Service | Port | Stack | Description |
|---------|------|-------|-------------|
| auth    | 3001 | Node.js + Express | Authentification & sessions |
| game    | 2567 | Node.js + Colyseus | Serveur de jeu temps réel |
| stats   | 8000 | Python + FastAPI | Statistiques & analytics |

## Infrastructure

| Service | Port | Description |
|---------|------|-------------|
| postgres | 5432 | Base de données |
| redis   | 6379 | Cache / PubSub |

## Quickstart

```bash
# Lancer tout l'environnement de dev
docker compose up --build

# Un seul service
docker compose up auth

# Dev sans Docker
cd auth && npm install && npm run dev
cd game && npm install && npm run dev
cd stats && pip install -r requirements.txt && uvicorn app.main:app --reload
```

## Structure

```
├── auth/           # Service d'authentification
│   ├── Dockerfile
│   ├── package.json
│   └── src/
├── game/           # Serveur Colyseus
│   ├── Dockerfile
│   ├── package.json
│   └── src/
├── stats/          # Service de statistiques
│   ├── Dockerfile
│   ├── requirements.txt
│   └── app/
└── docker-compose.yml
```
