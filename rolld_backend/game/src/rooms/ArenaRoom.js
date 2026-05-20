const { Room } = require("@colyseus/core");
const { GameState, Player } = require("../schema/GameState");
const Chat = require("../chat/ChatManager");

// Free-roam: no rounds, no phases, no checkpoints. Players connect, move around, leave.
// Schema fields (phase, countdown, roundNumber, etc.) are kept to preserve the
// handshake — but state.phase is pinned to "playing" forever and other fields are
// left at their default values. To fully drop them, regenerate the C# schema and
// rebuild WebGL.

class ArenaRoom extends Room {
  maxClients = 20;

  onCreate(options) {
    this.setState(new GameState());
    this.state.phase = "playing"; // pinned: free-roam, no state machine
    this.state.gameMode = "free";
    this.setPatchRate(16); // ~62.5 Hz
    this.setMetadata({ name: options?.roomName || ('Salle #' + this.roomId.substring(0, 6)) });

    console.log(`[ArenaRoom] Room ${this.roomId} created (free-roam)`);

    this.onMessage("position", (client, data) => {
      const player = this.state.players.get(client.sessionId);
      if (!player) return;
      player.x = data.x ?? player.x;
      player.y = data.y ?? player.y;
      player.z = data.z ?? player.z;
      player.vx = data.vx ?? player.vx;
      player.vy = data.vy ?? player.vy;
      player.vz = data.vz ?? player.vz;
      player.rx = data.rx ?? player.rx;
      player.ry = data.ry ?? player.ry;
      player.rz = data.rz ?? player.rz;
      player.rw = data.rw ?? player.rw;
      player.avx = data.avx ?? player.avx;
      player.avy = data.avy ?? player.avy;
      player.avz = data.avz ?? player.avz;
      player.t = Date.now();
    });

    this.onMessage("chat", (client, data) => {
      const player = this.state.players.get(client.sessionId);
      if (!player || !data.text) return;
      const msg = Chat.push(player.name, data.text);
      if (msg) this.broadcast("chat", msg);
    });

    // Accept legacy "ready" / "checkpointReached" silently so old clients don't error.
    this.onMessage("ready", () => {});
    this.onMessage("checkpointReached", () => {});
  }

  onJoin(client, options) {
    console.log(`[ArenaRoom] ${client.sessionId} joined (${options.name || "anonymous"})`);
    const player = new Player();
    player.name = options.name || "Joueur";
    player.colorR = options.colorR ?? 1;
    player.colorG = options.colorG ?? 0.4;
    player.colorB = options.colorB ?? 0.2;
    const spawn = this._findSpawnPosition();
    player.x = spawn.x;
    player.y = spawn.y;
    player.z = spawn.z;
    player.t = Date.now();
    this.state.players.set(client.sessionId, player);
  }

  onLeave(client, consented) {
    console.log(`[ArenaRoom] ${client.sessionId} left`);
    this.state.players.delete(client.sessionId);
  }

  onDispose() {
    console.log(`[ArenaRoom] Room ${this.roomId} disposed`);
  }

  // ─── Spawn helper ────────────────────────────────────────────────────

  _findSpawnPosition() {
    const MIN_DIST = 5.0;
    const SPAWN_Y = 1.5;
    const RANGE = 20;
    const existing = [];
    this.state.players.forEach((p) => existing.push({ x: p.x, z: p.z }));

    if (existing.length === 0) {
      return { x: (Math.random() - 0.5) * RANGE, y: SPAWN_Y, z: (Math.random() - 0.5) * RANGE };
    }

    let best = { x: 0, y: SPAWN_Y, z: 0 };
    let bestDist = 0;
    for (let i = 0; i < 20; i++) {
      const cx = (Math.random() - 0.5) * RANGE;
      const cz = (Math.random() - 0.5) * RANGE;
      let minD = Infinity;
      for (const p of existing) {
        const d = Math.sqrt((cx - p.x) ** 2 + (cz - p.z) ** 2);
        if (d < minD) minD = d;
      }
      if (minD >= MIN_DIST) return { x: cx, y: SPAWN_Y, z: cz };
      if (minD > bestDist) { bestDist = minD; best = { x: cx, y: SPAWN_Y, z: cz }; }
    }
    return best;
  }
}

module.exports = { ArenaRoom };
