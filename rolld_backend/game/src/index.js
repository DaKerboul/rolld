const cors = require('cors');
const { Server } = require('@colyseus/core');
const { WebSocketTransport } = require('@colyseus/ws-transport');
const { ArenaRoom } = require('./rooms/ArenaRoom');

const PORT = process.env.PORT || 2567;

// Colyseus 0.17 – express callback receives the transport's internal Express app
const gameServer = new Server({
  transport: new WebSocketTransport(),
  express: (app) => {
    app.use(cors());

    app.get('/health', (_req, res) => {
      res.json({ service: 'game', status: 'ok', timestamp: new Date().toISOString() });
    });

    app.get('/', (_req, res) => {
      res.send('🎮 Game server running');
    });
  },
});

// Define rooms
gameServer.define('arena', ArenaRoom);
console.log('✅ ArenaRoom registered');

gameServer.listen(PORT).then(() => {
  console.log(`🎮 Game server running on ws://localhost:${PORT}`);
});
