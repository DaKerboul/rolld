import { useState, useEffect, useCallback } from 'react'
import { theme } from '../env'

const SERVER = 'https://game.rolld.kerboul.me'

const TABS = [
  { key: 'totalDistance', label: 'Distance',    unit: 'm',   format: v => Math.round(v ?? 0).toLocaleString('fr-FR') },
  { key: 'maxSpeed',      label: 'Vitesse max', unit: 'm/s', format: v => (v ?? 0).toFixed(1) },
  { key: 'totalJumps',   label: 'Sauts',       unit: '',    format: v => (v ?? 0).toLocaleString('fr-FR') },
  { key: 'bumpsGiven',   label: 'Bumps',       unit: '',    format: v => (v ?? 0).toLocaleString('fr-FR') },
  { key: 'totalPlaytime',label: 'Temps de jeu',unit: '',    format: v => {
    const total = Math.round(v ?? 0)
    const h = Math.floor(total / 3600)
    const m = Math.floor((total % 3600) / 60)
    const s = total % 60
    return h > 0 ? `${h}h ${m}m` : `${m}m ${s}s`
  }},
]

export default function StatsPage() {
  const [activeTab, setActiveTab] = useState(TABS[0].key)
  const [rows, setRows] = useState([])
  const [loading, setLoading] = useState(false)
  const [lastRefresh, setLastRefresh] = useState(null)

  const fetchLeaderboard = useCallback(async (key) => {
    setLoading(true)
    try {
      const res = await fetch(`${SERVER}/stats/leaderboard/${key}`)
      if (!res.ok) throw new Error(res.statusText)
      const data = await res.json()
      setRows(data)
      setLastRefresh(new Date())
    } catch {
      setRows([])
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    setRows([])
    fetchLeaderboard(activeTab)
    const id = setInterval(() => fetchLeaderboard(activeTab), 30_000)
    return () => clearInterval(id)
  }, [activeTab, fetchLeaderboard])

  const currentTab = TABS.find(t => t.key === activeTab)

  const medalColor = (i) => {
    if (i === 0) return '#f39c12'
    if (i === 1) return '#9b9b9b'
    if (i === 2) return '#cd7f32'
    return theme.accentLight
  }

  return (
    <div className="min-h-screen pt-20 px-4">
      <div className="max-w-3xl mx-auto">
        {/* Header */}
        <div className="mb-8 text-center">
          <h1 className="text-4xl font-black text-rolld-text mb-2">Classements</h1>
          <p className="text-rolld-muted text-sm">Top 10 joueurs par catégorie — mis à jour en temps réel</p>
        </div>

        {/* Tabs */}
        <div className="flex flex-wrap gap-2 justify-center mb-8">
          {TABS.map(tab => (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              className={`px-4 py-2 rounded-xl text-sm font-medium transition-all duration-200 ${
                activeTab === tab.key
                  ? 'text-white'
                  : 'text-rolld-muted hover:text-rolld-text bg-rolld-surface border border-rolld-border'
              }`}
              style={activeTab === tab.key ? {
                background: `linear-gradient(to right, ${theme.accent}, ${theme.gradientTo})`,
              } : {}}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Table */}
        <div className="rounded-2xl overflow-hidden border border-rolld-border bg-rolld-surface">
          <div className="px-6 py-4 border-b border-rolld-border flex items-center justify-between">
            <span className="text-rolld-text font-semibold">{currentTab.label}</span>
            <div className="flex items-center gap-3">
              {lastRefresh && (
                <span className="text-rolld-muted text-xs">
                  {lastRefresh.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
                </span>
              )}
              <button
                onClick={() => fetchLeaderboard(activeTab)}
                disabled={loading}
                className="text-xs text-rolld-accent hover:text-rolld-accent-light transition-colors disabled:opacity-40"
              >
                {loading ? '...' : 'Actualiser'}
              </button>
            </div>
          </div>

          {loading && rows.length === 0 ? (
            <div className="py-16 text-center text-rolld-muted text-sm">Chargement…</div>
          ) : rows.length === 0 ? (
            <div className="py-16 text-center text-rolld-muted text-sm">Aucune donnée pour l'instant.</div>
          ) : (
            <table className="w-full">
              <tbody>
                {rows.map((row, i) => (
                  <tr
                    key={row.name}
                    className={`border-b border-rolld-border/50 last:border-0 transition-colors hover:bg-rolld-border/20 ${
                      i === 0 ? 'bg-rolld-border/10' : ''
                    }`}
                  >
                    <td className="px-6 py-4 w-12">
                      <span className="text-sm font-bold" style={{ color: medalColor(i) }}>
                        {i < 3 ? ['🥇', '🥈', '🥉'][i] : `#${i + 1}`}
                      </span>
                    </td>
                    <td className="px-2 py-4 flex-1 text-rolld-text font-medium">
                      {row.name}
                    </td>
                    <td className="px-6 py-4 text-right font-mono text-sm" style={{ color: theme.accentLight }}>
                      {currentTab.format(row.value)}
                      {currentTab.unit && <span className="text-rolld-muted ml-1">{currentTab.unit}</span>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  )
}
