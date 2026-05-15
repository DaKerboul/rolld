const { Room } = require("@colyseus/core");
const { GameState, Player } = require("../schema/GameState");

class ArenaRoom extends Room {
  maxClients = 20;

  onCreate(options) {
    this.setState(new GameState());
    this.setPatchRate(16); // ~62.5 Hz state broadcast
    console.log(`[ArenaRoom] Room ${this.roomId} created (patchRate=16ms ~62Hz)`);

    // Handle position updates from clients
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

    // Handle chat messages (optional, for future)
    this.onMessage("chat", (client, data) => {
      this.broadcast("chat", {
        sender: client.sessionId,
        name: this.state.players.get(client.sessionId)?.name || "???",
        message: data.message,
      });
    });
  }

  onJoin(client, options) {
    console.log(`[ArenaRoom] ${client.sessionId} joined (name: ${options.name || "anonymous"})`);

    const player = new Player();
    player.name = options.name || "Joueur";
    player.colorR = options.colorR ?? 1;
    player.colorG = options.colorG ?? 0.4;
    player.colorB = options.colorB ?? 0.2;

    // Find a spawn position away from other players
    const spawnPos = this._findSpawnPosition();
    player.x = spawnPos.x;
    player.y = spawnPos.y;
    player.z = spawnPos.z;
    player.t = Date.now();

    this.state.players.set(client.sessionId, player);
  }

  onLeave(client, consented) {
    console.log(`[ArenaRoom] ${client.sessionId} left (consented: ${consented})`);
    this.state.players.delete(client.sessionId);
  }

  onDispose() {
    console.log(`[ArenaRoom] Room ${this.roomId} disposed`);
  }

  /**
   * Find a spawn position elevated and away from existing players.
   * Tries up to 10 random positions, picks the one farthest from others.
   * Falls back to random if no good spot found.
   */
  _findSpawnPosition() {
    const MIN_DIST = 3.0;
    const SPAWN_Y = 5; // elevated spawn — ball drops naturally
    const RANGE = 20;
    let bestPos = { x: 0, y: SPAWN_Y, z: 0 };
    let bestMinDist = 0;

    const existingPositions = [];
    this.state.players.forEach((p) => {
      existingPositions.push({ x: p.x, z: p.z });
    });

    // If no existing players, just random
    if (existingPositions.length === 0) {
      return {
        x: (Math.random() - 0.5) * RANGE,
        y: SPAWN_Y,
        z: (Math.random() - 0.5) * RANGE,
      };
    }

    for (let attempt = 0; attempt < 10; attempt++) {
      const cx = (Math.random() - 0.5) * RANGE;
      const cz = (Math.random() - 0.5) * RANGE;
      let minDist = Infinity;
      for (const p of existingPositions) {
        const dx = cx - p.x;
        const dz = cz - p.z;
        const d = Math.sqrt(dx * dx + dz * dz);
        if (d < minDist) minDist = d;
      }
      if (minDist >= MIN_DIST) {
        return { x: cx, y: SPAWN_Y, z: cz };
      }
      if (minDist > bestMinDist) {
        bestMinDist = minDist;
        bestPos = { x: cx, y: SPAWN_Y, z: cz };
      }
    }

    return bestPos;
  }
}

module.exports = { ArenaRoom };
