const cors = require('cors');
const { Server, matchMaker } = require('@colyseus/core');
const { WebSocketTransport } = require('@colyseus/ws-transport');
const { ArenaRoom } = require('./rooms/ArenaRoom');
const Stats = require('./stats/StatsManager');
const Chat = require('./chat/ChatManager');
const { z } = require('zod');

const PORT = process.env.PORT || 2567;

const statsUpdateSchema = z.object({
  name: z.string().min(1).max(32),
  stats: z.object({
    totalDistance:    z.number().optional(),
    totalJumps:       z.number().optional(),
    maxSpeed:         z.number().optional(),
    bestRaceTime:     z.number().optional(),
    racesPlayed:      z.number().optional(),
    qualifications:   z.number().optional(),
    eliminations:     z.number().optional(),
    checkpointsTotal: z.number().optional(),
    bumpsGiven:       z.number().optional(),
    totalPlaytime:    z.number().optional(),
  }),
});

const chatSendSchema = z.object({
  name: z.string().min(1).max(32),
  text: z.string().min(1).max(200),
});

let _gameServer;

const gameServer = new Server({
  transport: new WebSocketTransport(),
  express: (app) => {
    app.use(cors());
    app.use(require('express').json());

    app.get('/health', (_req, res) => {
      res.json({ service: 'game', status: 'ok', timestamp: new Date().toISOString() });
    });

    app.get('/', (_req, res) => res.send('🎮 Game server running'));

    // ── Stats ────────────────────────────────────────────────────────────
    app.get('/stats', (_req, res) => {
      res.json(Stats.getAll());
    });

    app.get('/stats/leaderboard/:key', (req, res) => {
      const board = Stats.getLeaderboard(req.params.key);
      if (!board) return res.status(400).json({ error: 'invalid key' });
      res.json(board);
    });

    app.post('/stats/update', (req, res) => {
      const parsed = statsUpdateSchema.safeParse(req.body);
      if (!parsed.success) return res.status(400).json({ error: parsed.error.issues });
      const ok = Stats.update(parsed.data.name, parsed.data.stats);
      res.json({ ok });
    });

    // ── Rooms ────────────────────────────────────────────────────────────
    app.get('/rooms', async (_req, res) => {
      try {
        const rooms = await matchMaker.query({ name: 'arena' });
        res.json(rooms.map(r => ({
          roomId:     r.roomId,
          clients:    r.clients,
          maxClients: r.maxClients,
          metadata:   r.metadata || {},
        })));
      } catch (_) {
        res.json([]);
      }
    });

    // ── Chat ─────────────────────────────────────────────────────────────
    app.get('/chat/history', (req, res) => {
      res.json(Chat.getHistory(req.query.since));
    });

    app.post('/chat/send', (req, res) => {
      const parsed = chatSendSchema.safeParse(req.body);
      if (!parsed.success) return res.status(400).json({ error: parsed.error.issues });
      const msg = Chat.push(parsed.data.name, parsed.data.text);
      if (!msg) return res.status(429).json({ error: 'empty or invalid message' });

      // Broadcast to all active Colyseus rooms
      if (_gameServer) {
        try {
          const rooms = _gameServer.matchMaker?.rooms;
          if (rooms) {
            for (const room of rooms.values()) {
              room.broadcast('chat', msg);
            }
          }
        } catch (_) {}
      }

      res.json(msg);
    });
  },
});

_gameServer = gameServer;

gameServer.define('arena', ArenaRoom);
console.log('✅ ArenaRoom registered');

gameServer.listen(PORT).then(() => {
  console.log(`🎮 Game server running on ws://localhost:${PORT}`);
});
