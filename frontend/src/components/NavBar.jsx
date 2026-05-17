import { theme } from '../env'

const LINKS = [
  { id: 'home', label: 'Accueil' },
  { id: 'stats', label: 'Stats' },
  { id: 'chat', label: 'Chat' },
]

export default function NavBar({ page, setPage }) {
  return (
    <nav className="fixed top-0 left-0 right-0 z-50 glass border-b border-rolld-border/60">
      <div className="max-w-6xl mx-auto px-4 h-14 flex items-center justify-between">
        <button
          onClick={() => setPage('home')}
          className="font-black text-lg tracking-tight"
          style={{ color: theme.accent }}
        >
          ROLL'D
        </button>

        <div className="flex items-center gap-1">
          {LINKS.map(link => (
            <button
              key={link.id}
              onClick={() => setPage(link.id)}
              className={`px-4 py-2 rounded-xl text-sm font-medium transition-all duration-200 ${
                page === link.id
                  ? 'text-white'
                  : 'text-rolld-muted hover:text-rolld-text'
              }`}
              style={page === link.id ? { background: `rgba(${theme.accentRgb}, 0.2)`, color: theme.accentLight } : {}}
            >
              {link.label}
            </button>
          ))}

          <button
            onClick={() => setPage('play')}
            className="ml-3 px-4 py-2 rounded-xl text-sm font-bold text-white transition-all duration-200 hover:scale-105"
            style={{
              backgroundImage: `linear-gradient(to right, ${theme.accent}, ${theme.gradientTo})`,
            }}
          >
            Jouer
          </button>
        </div>
      </div>
    </nav>
  )
}
