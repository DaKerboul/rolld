<div align="center">

<h1>ROLL'D</h1>

<p><strong>Browser-based marble MMO — multiplayer physics, real-time leaderboards, playable directly in your browser.</strong></p>

<p>
  <img src="https://img.shields.io/badge/Unity-6000.0-black?style=for-the-badge&logo=unity&logoColor=white" alt="Unity 6" />
  <img src="https://img.shields.io/badge/WebGL-build-E34F26?style=for-the-badge&logo=webgl&logoColor=white" alt="WebGL" />
  <img src="https://img.shields.io/badge/Colyseus-0.17-6C47FF?style=for-the-badge&logo=node.js&logoColor=white" alt="Colyseus" />
  <img src="https://img.shields.io/badge/React-19-61DAFB?style=for-the-badge&logo=react&logoColor=black" alt="React" />
  <img src="https://img.shields.io/badge/Vite-5-646CFF?style=for-the-badge&logo=vite&logoColor=white" alt="Vite" />
  <img src="https://img.shields.io/badge/Tailwind_CSS-3-38BDF8?style=for-the-badge&logo=tailwindcss&logoColor=white" alt="Tailwind" />
</p>

<p>
  <a href="https://rolld.kerboul.me"><img src="https://img.shields.io/badge/Play_Now-rolld.kerboul.me-22c55e?style=for-the-badge&logo=googlechrome&logoColor=white" alt="Play Now" /></a>
</p>

</div>

---

## What is ROLL'D?

ROLL'D is a multiplayer marble game that runs entirely in the browser via Unity WebGL. Players control a physics-based ball in a shared 3D arena, competing for distance, speed, and style. The game features real-time synchronisation at 60 Hz, in-game chat, and persistent leaderboards.

No install. No account. Just open the page and roll.

---

## Features

- **Real-time multiplayer** - up to 20 players per room, 60 Hz state sync via Colyseus WebSockets
- **Physics-based gameplay** - Unity Rigidbody, jump charge, gel pads (speed boosts), ball-to-ball bumps
- **Room lobby** - browse open rooms, create your own, choose your colour and name
- **In-game chat** - accessible in-game (T key) and on the dedicated website chat page
- **Live leaderboards** - distance, max speed, jumps, bumps, playtime, updated every 30 seconds
- **Spectator camera** - orbiting camera while in lobby or after disconnecting
- **WebGL-native** - no plugins, no downloads, runs in Chrome/Firefox/Edge

---

## Architecture

```
rolld/
├── game/                  # Unity 6 project (WebGL build)
│   └── Assets/Scripts/
│       ├── Network/       # Colyseus SDK integration, schema, lobby UI
│       ├── Stats/         # StatsTracker - periodic HTTP upload
│       └── UI/            # IMGUI in-game HUD, chat, keybinds
│
├── rolld_backend/game/    # Colyseus 0.17 game server (Node.js)
│   └── src/
│       ├── rooms/         # ArenaRoom - game state machine
│       ├── schema/        # Colyseus schema (Player + GameState)
│       ├── stats/         # StatsManager - JSON persistence
│       └── chat/          # ChatManager - in-memory history
│
└── frontend/              # React + Vite + Tailwind SPA
    └── src/
        ├── pages/         # Home, Stats leaderboard, Chat
        └── components/    # NavBar, GameCanvas (Unity embed)
```

### Network flow

```
Browser
  └── Unity WebGL (GameCanvas iframe)
        └── Colyseus SDK (WebSocket wss://)
              └── ArenaRoom (Node.js)
                    └── Broadcast state @60 Hz

Browser
  └── React SPA
        └── REST API (HTTPS)
              ├── GET  /stats/leaderboard/:key
              ├── GET  /chat/history?since=
              └── POST /stats/update  (from Unity every 30s)
```

---

## Tech stack

| Layer | Technology |
|---|---|
| Game engine | Unity 6 LTS, C# |
| Multiplayer | Colyseus 0.17 (Node.js + WebSocket) |
| Frontend | React 19, Vite 5, Tailwind CSS 3 |
| Deployment | Docker, Coolify, nginx |
| Self-hosted | Proxmox LXC, Gitea, Traefik reverse proxy |

---

## Running locally

### Prerequisites

- Node.js 20+
- Unity 6000.x (for game builds only)

### Game server

```bash
cd rolld_backend/game
npm install
npm run dev
```

Server starts on `ws://localhost:2567`.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. The frontend points to the production game server by default - edit `src/pages/StatsPage.jsx` and `src/components/GameCanvas.jsx` to switch to localhost.

### Unity (optional)

Open `game/` in Unity 6. The server URL is hardcoded in `Assets/Scripts/Network/NetworkManager.cs`. Switch to `wss://game.rolld.kerboul.me` for prod or `ws://localhost:2567` for local testing.

---

## Controls

| Key | Action |
|---|---|
| WASD / Arrow keys | Move |
| Space (hold) | Charge jump |
| Space (release) | Jump |
| T | Open chat |
| Escape | Close chat |
| Tab | Show keybindings |
| Backtick (`) | Debug network info |

---

## Live deployment

<p>
  <img src="https://img.shields.io/badge/Frontend-rolld.kerboul.me-22c55e?style=flat-square&logo=nginx&logoColor=white" alt="Frontend" />
  <img src="https://img.shields.io/badge/Game_server-game.rolld.kerboul.me-6C47FF?style=flat-square&logo=node.js&logoColor=white" alt="Game server" />
  <img src="https://img.shields.io/badge/Self_hosted-Proxmox_homelab-E57000?style=flat-square&logo=proxmox&logoColor=white" alt="Self-hosted" />
</p>

The stack runs on a self-hosted Proxmox homelab cluster. Coolify handles container orchestration and auto-deployment on git push. Traefik manages HTTPS termination.

---

## Licence

MIT - do whatever you want with it.
