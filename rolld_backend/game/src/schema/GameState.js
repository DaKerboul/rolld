const { Schema, MapSchema, defineTypes } = require("@colyseus/schema");

class Player extends Schema {
  constructor() {
    super();
    this.x = 0; this.y = 5; this.z = 0;
    this.vx = 0; this.vy = 0; this.vz = 0;
    this.rx = 0; this.ry = 0; this.rz = 0; this.rw = 1;
    this.t = 0;
    this.name = "";
    this.colorR = 1; this.colorG = 1; this.colorB = 1;
    this.avx = 0; this.avy = 0; this.avz = 0;
    this.isEliminated = false;
    this.isQualified  = false;
    this.isReady      = false;
    this.checkpointIndex = 0;
  }
}

// Field order must match NetworkSchema.cs [Type(N)] indices exactly
defineTypes(Player, {
  x: "float32",  y: "float32",  z: "float32",   // 0-2
  vx: "float32", vy: "float32", vz: "float32",   // 3-5
  rx: "float32", ry: "float32", rz: "float32", rw: "float32", // 6-9
  t: "float64",                                   // 10
  name: "string",                                 // 11
  colorR: "float32", colorG: "float32", colorB: "float32",     // 12-14
  avx: "float32", avy: "float32", avz: "float32",              // 15-17
  isEliminated: "boolean",  // 18
  isQualified:  "boolean",  // 19
  isReady:      "boolean",  // 20
  checkpointIndex: "int8",  // 21
});

class GameState extends Schema {
  constructor() {
    super();
    this.players      = new MapSchema();
    this.phase        = "lobby";
    this.countdown    = 0;
    this.roundNumber  = 1;
    this.totalRounds  = 3;
    this.playersAlive = 0;
    this.gameMode     = "race";
    this.winnerName   = "";
  }
}

defineTypes(GameState, {
  players:      { map: Player }, // 0
  phase:        "string",        // 1
  countdown:    "float32",       // 2
  roundNumber:  "int8",          // 3
  totalRounds:  "int8",          // 4
  playersAlive: "int8",          // 5
  gameMode:     "string",        // 6
  winnerName:   "string",        // 7
});

module.exports = { GameState, Player };
