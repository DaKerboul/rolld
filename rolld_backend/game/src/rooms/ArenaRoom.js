const { Room } = require("@colyseus/core");
const { GameState, Player } = require("../schema/GameState");
const Chat = require("../chat/ChatManager");

const LOBBY_TIMEOUT = 30;
const COUNTDOWN_DURATION = 3;
const ROUND_END_DURATION = 5;

const QUALIFY_RATIO = 0.6;

class ArenaRoom extends Room {
  maxClients = 20;

  onCreate(options) {
    this.setState(new GameState());
    this.setPatchRate(16); // ~62.5 Hz
    this.setMetadata({ name: options?.roomName || ('Salle #' + this.roomId.substring(0, 6)) });

    this._phaseTimer = null;
    this._lobbyTimer = null;

    console.log(`[ArenaRoom] Room ${this.roomId} created`);

    this.onMessage("position", (client, data) => {
      const player = this.state.players.get(client.sessionId);
      if (!player || player.isEliminated) return;
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

    this.onMessage("ready", (client) => {
      const player = this.state.players.get(client.sessionId);
      if (!player || this.state.phase !== "lobby") return;
      player.isReady = true;
      console.log(`[ArenaRoom] ${client.sessionId} ready`);
      this._checkAllReady();
    });

    this.onMessage("chat", (client, data) => {
      const player = this.state.players.get(client.sessionId);
      if (!player || !data.text) return;
      const msg = Chat.push(player.name, data.text);
      if (msg) this.broadcast("chat", msg);
    });

    this.onMessage("checkpointReached", (client, data) => {
      if (this.state.phase !== "playing") return;
      const player = this.state.players.get(client.sessionId);
      if (!player || player.isEliminated || player.isQualified) return;
      const expected = player.checkpointIndex;
      if (data.index !== expected) return;
      player.checkpointIndex = data.index + 1;
      const TOTAL_CHECKPOINTS = 5;
      if (player.checkpointIndex >= TOTAL_CHECKPOINTS) {
        this._qualifyPlayer(client.sessionId, "finish");
      }
    });
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
    this._updatePlayersAlive();

    if (this.state.players.size === 1 && this.state.phase === "lobby") {
      this._startLobbyTimer();
    }
  }

  onLeave(client, consented) {
    console.log(`[ArenaRoom] ${client.sessionId} left`);
    this.state.players.delete(client.sessionId);
    this._updatePlayersAlive();
    if (this.state.phase === "playing") {
      this._checkRoundEndCondition();
    }
  }

  onDispose() {
    this._clearAllTimers();
    console.log(`[ArenaRoom] Room ${this.roomId} disposed`);
  }

  // ─── Phase transitions ──────────────────────────────────────────────

  _startLobbyTimer() {
    if (this._lobbyTimer) return;
    this._lobbyTimer = setTimeout(() => this._startCountdown(), LOBBY_TIMEOUT * 1000);
    console.log(`[ArenaRoom] Lobby timer started (${LOBBY_TIMEOUT}s)`);
  }

  _checkAllReady() {
    if (this.state.players.size < 2) return;
    let allReady = true;
    this.state.players.forEach((p) => { if (!p.isReady) allReady = false; });
    if (allReady) {
      clearTimeout(this._lobbyTimer);
      this._lobbyTimer = null;
      this._startCountdown();
    }
  }

  _startCountdown() {
    if (this.state.phase !== "lobby") return;
    this.state.phase = "countdown";
    this.state.countdown = COUNTDOWN_DURATION;
    console.log(`[ArenaRoom] Countdown started`);

    const tick = () => {
      this.state.countdown -= 1;
      if (this.state.countdown <= 0) {
        this._startPlaying();
      } else {
        this._phaseTimer = setTimeout(tick, 1000);
      }
    };
    this._phaseTimer = setTimeout(tick, 1000);
  }

  _startPlaying() {
    this.state.gameMode = "race";
    this.state.phase = "playing";
    this.state.countdown = 0;

    this.state.players.forEach((p) => {
      p.isEliminated = false;
      p.isQualified = false;
      p.isReady = false;
      p.checkpointIndex = 0;
    });

    this._updatePlayersAlive();

    this.broadcast("roundStart", {
      round: this.state.roundNumber,
      mode: this.state.gameMode,
      totalRounds: this.state.totalRounds,
    });

    console.log(`[ArenaRoom] Round ${this.state.roundNumber} started (race)`);

  }

  _endRound() {
    if (this.state.phase !== "playing") return;
    this._clearAllTimers();
    this.state.phase = "roundEnd";
    this.broadcast("roundEnd", { round: this.state.roundNumber });
    console.log(`[ArenaRoom] Round ${this.state.roundNumber} ended`);

    if (this.state.roundNumber >= this.state.totalRounds) {
      this._phaseTimer = setTimeout(() => this._endGame(), ROUND_END_DURATION * 1000);
    } else {
      this._phaseTimer = setTimeout(() => this._nextRound(), ROUND_END_DURATION * 1000);
    }
  }

  _nextRound() {
    this.state.roundNumber += 1;
    this.state.phase = "lobby";
    this.state.players.forEach((p) => {
      p.isReady = false;
      const spawn = this._findSpawnPosition();
      p.x = spawn.x; p.y = spawn.y; p.z = spawn.z;
    });
    this._updatePlayersAlive();
    this._lobbyTimer = null;
    this._startLobbyTimer();
    console.log(`[ArenaRoom] Lobby for round ${this.state.roundNumber}`);
  }

  _endGame() {
    this.state.phase = "gameEnd";
    let winner = "";
    let best = -1;
    this.state.players.forEach((p) => {
      const score = p.isQualified ? 1000 : p.checkpointIndex;
      if (score > best) { best = score; winner = p.name; }
    });
    this.state.winnerName = winner;
    this.broadcast("gameEnd", { winner });
    console.log(`[ArenaRoom] Game over — winner: ${winner}`);
  }

  // ─── Elimination helpers ─────────────────────────────────────────────

  _eliminatePlayer(sessionId, reason) {
    const player = this.state.players.get(sessionId);
    if (!player || player.isEliminated || player.isQualified) return;
    player.isEliminated = true;
    this._updatePlayersAlive();
    this.broadcast("eliminated", { sessionId, name: player.name, reason });
    console.log(`[ArenaRoom] ${player.name} (${sessionId}) eliminated: ${reason}`);
    this._checkRoundEndCondition();
  }

  _qualifyPlayer(sessionId, reason) {
    const player = this.state.players.get(sessionId);
    if (!player || player.isQualified || player.isEliminated) return;
    player.isQualified = true;
    this._updatePlayersAlive();
    this.broadcast("qualified", { sessionId, name: player.name });
    console.log(`[ArenaRoom] ${player.name} (${sessionId}) qualified: ${reason}`);

    const totalActive = this._getActiveCount();
    const qualifiedCount = this._getQualifiedCount();
    const toQualify = Math.ceil(totalActive * QUALIFY_RATIO);
    if (qualifiedCount >= toQualify) {
      this.state.players.forEach((p, id) => {
        if (!p.isQualified && !p.isEliminated) {
          this._eliminatePlayer(id, "too_slow");
        }
      });
      this._endRound();
    }
  }

  _checkRoundEndCondition() {
    if (this.state.phase !== "playing") return;
    const alive = this._getAliveCount();
    if (alive === 0) this._endRound();
  }

  _getAliveCount() {
    let n = 0;
    this.state.players.forEach((p) => { if (!p.isEliminated && !p.isQualified) n++; });
    return n;
  }

  _getQualifiedCount() {
    let n = 0;
    this.state.players.forEach((p) => { if (p.isQualified) n++; });
    return n;
  }

  _getActiveCount() {
    let n = 0;
    this.state.players.forEach((p) => { if (!p.isEliminated) n++; });
    return n;
  }

  _updatePlayersAlive() {
    this.state.playersAlive = this._getAliveCount();
  }

  _clearAllTimers() {
    if (this._phaseTimer) { clearTimeout(this._phaseTimer); this._phaseTimer = null; }
    if (this._lobbyTimer) { clearTimeout(this._lobbyTimer); this._lobbyTimer = null; }
  }

  // ─── Spawn helper ────────────────────────────────────────────────────

  _findSpawnPosition() {
    const MIN_DIST = 3.0;
    const SPAWN_Y = 5;
    const RANGE = 20;
    const existing = [];
    this.state.players.forEach((p) => existing.push({ x: p.x, z: p.z }));

    if (existing.length === 0) {
      return { x: (Math.random() - 0.5) * RANGE, y: SPAWN_Y, z: (Math.random() - 0.5) * RANGE };
    }

    let best = { x: 0, y: SPAWN_Y, z: 0 };
    let bestDist = 0;
    for (let i = 0; i < 10; i++) {
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
