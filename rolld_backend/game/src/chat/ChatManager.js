const fs = require("fs");
const path = require("path");

const DATA_FILE = path.join(__dirname, "../../data/chat.json");
const MAX_MESSAGES = 200;

let _messages = [];
let _nextId = 1;

function _load() {
  try {
    if (fs.existsSync(DATA_FILE)) {
      const data = JSON.parse(fs.readFileSync(DATA_FILE, "utf8"));
      _messages = data.messages || [];
      _nextId = data.nextId || (_messages.length + 1);
    }
  } catch (e) {
    console.warn("[Chat] Failed to load chat.json:", e.message);
  }
}

function _save() {
  try {
    fs.mkdirSync(path.dirname(DATA_FILE), { recursive: true });
    fs.writeFileSync(DATA_FILE, JSON.stringify({ messages: _messages, nextId: _nextId }, null, 2));
  } catch (e) {
    console.warn("[Chat] Failed to save chat.json:", e.message);
  }
}

function push(name, text) {
  if (!name || !text || typeof name !== "string" || typeof text !== "string") return null;
  name = name.slice(0, 32);
  text = text.slice(0, 200);
  if (text.trim().length === 0) return null;

  const msg = { id: _nextId++, timestamp: Date.now(), name, text: text.trim() };
  _messages.push(msg);
  if (_messages.length > MAX_MESSAGES) _messages.splice(0, _messages.length - MAX_MESSAGES);
  _save();
  return msg;
}

function getHistory(since) {
  const ts = Number(since) || 0;
  return ts === 0 ? _messages.slice(-50) : _messages.filter((m) => m.timestamp > ts);
}

_load();
console.log(`[Chat] Loaded ${_messages.length} message(s)`);

module.exports = { push, getHistory };
