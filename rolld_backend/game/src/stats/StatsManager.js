const fs = require("fs");
const path = require("path");

const DATA_FILE = path.join(__dirname, "../../data/stats.json");

const VALID_KEYS = [
  "totalDistance", "totalJumps", "maxSpeed", "bestRaceTime",
  "racesPlayed", "qualifications", "eliminations",
  "checkpointsTotal", "bumpsGiven", "totalPlaytime",
];

let _stats = {};
const _lastUpdate = new Map(); // name → timestamp, for rate-limiting

function _load() {
  try {
    if (fs.existsSync(DATA_FILE)) {
      _stats = JSON.parse(fs.readFileSync(DATA_FILE, "utf8"));
    }
  } catch (e) {
    console.warn("[Stats] Failed to load stats.json, starting fresh:", e.message);
    _stats = {};
  }
}

function _save() {
  try {
    fs.mkdirSync(path.dirname(DATA_FILE), { recursive: true });
    fs.writeFileSync(DATA_FILE, JSON.stringify(_stats, null, 2));
  } catch (e) {
    console.warn("[Stats] Failed to save stats.json:", e.message);
  }
}

function _defaults() {
  return {
    totalDistance: 0,
    totalJumps: 0,
    maxSpeed: 0,
    bestRaceTime: null,
    racesPlayed: 0,
    qualifications: 0,
    eliminations: 0,
    checkpointsTotal: 0,
    bumpsGiven: 0,
    totalPlaytime: 0,
  };
}

function update(name, delta) {
  if (!name || typeof name !== "string" || name.length > 32) return false;

  const now = Date.now();
  const last = _lastUpdate.get(name) || 0;
  if (now - last < 5000) return false; // rate-limit: 1 update per 5s per player
  _lastUpdate.set(name, now);

  if (!_stats[name]) _stats[name] = _defaults();
  const p = _stats[name];

  for (const key of VALID_KEYS) {
    if (delta[key] === undefined) continue;
    const val = Number(delta[key]);
    if (isNaN(val)) continue;

    if (key === "maxSpeed") {
      p.maxSpeed = Math.max(p.maxSpeed, val);
    } else if (key === "bestRaceTime") {
      if (val > 0 && (p.bestRaceTime === null || val < p.bestRaceTime)) {
        p.bestRaceTime = val;
      }
    } else {
      p[key] = (p[key] || 0) + val;
    }
  }

  _save();
  return true;
}

function getAll() {
  return Object.entries(_stats).map(([name, s]) => ({ name, ...s }));
}

function getLeaderboard(key) {
  if (!VALID_KEYS.includes(key)) return [];
  return Object.entries(_stats)
    .map(([name, s]) => ({ name, value: s[key] ?? 0 }))
    .filter((e) => e.value !== null && e.value > 0)
    .sort((a, b) => {
      // bestRaceTime: lower is better
      if (key === "bestRaceTime") return a.value - b.value;
      return b.value - a.value;
    })
    .slice(0, 10);
}

_load();
console.log(`[Stats] Loaded ${Object.keys(_stats).length} player(s)`);

module.exports = { update, getAll, getLeaderboard, VALID_KEYS };
