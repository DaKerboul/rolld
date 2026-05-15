const { Schema, MapSchema, defineTypes } = require("@colyseus/schema");

class Player extends Schema {
  constructor() {
    super();
    this.x = 0;
    this.y = 5;
    this.z = 0;
    this.vx = 0;
    this.vy = 0;
    this.vz = 0;
    this.rx = 0;
    this.ry = 0;
    this.rz = 0;
    this.rw = 1;
    this.t = 0;
    this.name = "";
    this.colorR = 1;
    this.colorG = 1;
    this.colorB = 1;
    this.avx = 0;
    this.avy = 0;
    this.avz = 0;
  }
}

defineTypes(Player, {
  x: "float32",
  y: "float32",
  z: "float32",
  vx: "float32",
  vy: "float32",
  vz: "float32",
  rx: "float32",
  ry: "float32",
  rz: "float32",
  rw: "float32",
  t: "float64",
  name: "string",
  colorR: "float32",
  colorG: "float32",
  colorB: "float32",
  avx: "float32",
  avy: "float32",
  avz: "float32",
});

class GameState extends Schema {
  constructor() {
    super();
    this.players = new MapSchema();
  }
}

defineTypes(GameState, {
  players: { map: Player },
});

module.exports = { GameState, Player };
