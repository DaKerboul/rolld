import { useState, useEffect, useCallback, useRef } from 'react'
import { theme } from '../env'

const SERVER = 'https://game.rolld.kerboul.me'

const TABS = [
  { key: 'totalDistance', label: 'Distance',     icon: '📏', unit: 'm',   format: v => Math.round(v ?? 0).toLocaleString('fr-FR') },
  { key: 'maxSpeed',      label: 'Vitesse max',  icon: '⚡', unit: 'm/s', format: v => (v ?? 0).toFixed(1) },
  { key: 'totalJumps',    label: 'Sauts',        icon: '🦘', unit: '',    format: v => (v ?? 0).toLocaleString('fr-FR') },
  { key: 'bumpsGiven',    label: 'Bumps',        icon: '💥', unit: '',    format: v => (v ?? 0).toLocaleString('fr-FR') },
  { key: 'totalPlaytime', label: 'Temps de jeu', icon: '⏱️', unit: '',    format: v => {
    const t = Math.round(v ?? 0)
    const h = Math.floor(t / 3600), m = Math.floor((t % 3600) / 60), s = t % 60
    return h > 0 ? `${h}h ${m}m` : `${m}m ${s}s`
  }},
]

const MEDAL = [
  { icon: '🥇', bg: 'rgba(243,156,18,0.08)', border: 'rgba(243,156,18,0.3)', color: '#f39c12' },
  { icon: '🥈', bg: 'rgba(192,192,192,0.06)', border: 'rgba(192,192,192,0.25)', color: '#b0b0b0' },
  { icon: '🥉', bg: 'rgba(205,127,50,0.06)', border: 'rgba(205,127,50,0.25)', color: '#cd7f32' },
]

export default function StatsPage() {
  const [activeTab, setActiveTab] = useState(0)
  const [rows, setRows] = useState([])
  const [visibleRows, setVisibleRows] = useState([])
  const [loading, setLoading] = useState(false)
  const [lastRefresh, setLastRefresh] = useState(null)
  const timers = useRef([])

  const fetchLeaderboard = useCallback(async (key) => {
    setLoading(true)
    try {
      const res = await fetch(`${SERVER}/stats/leaderboard/${key}`)
      if (!res.ok) throw new Error()
      setRows(await res.json())
      setLastRefresh(new Date())
    } catch {
      setRows([])
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    setRows([])
    setVisibleRows([])
    fetchLeaderboard(TABS[activeTab].key)
    const id = setInterval(() => fetchLeaderboard(TABS[activeTab].key), 30_000)
    return () => clearInterval(id)
  }, [activeTab, fetchLeaderboard])

  useEffect(() => {
    timers.current.forEach(clearTimeout)
    timers.current = []
    setVisibleRows([])
    rows.forEach((_, i) => {
      const t = setTimeout(() => setVisibleRows(prev => [...prev, i]), 50 + i * 65)
      timers.current.push(t)
    })
    return () => timers.current.forEach(clearTimeout)
  }, [rows])

  const tab = TABS[activeTab]
  const maxVal = rows[0]?.value || 1

  return (
    <div className="min-h-screen pt-20 pb-16 px-4">
      <style>{`
        @keyframes glowGold {
          0%,100% { box-shadow: 0 0 10px rgba(243,156,18,0.15), inset 0 0 20px rgba(243,156,18,0.03); }
          50%      { box-shadow: 0 0 25px rgba(243,156,18,0.35), inset 0 0 30px rgba(243,156,18,0.07); }
        }
        @keyframes shimmerBar {
          0%   { background-position: -200% center; }
          100% { background-position:  200% center; }
        }
        @keyframes countBadge {
          from { opacity:0; transform: scale(0.7) translateY(-4px); }
          to   { opacity:1; transform: scale(1)   translateY(0);    }
        }
        @keyframes tabIn {
          from { opacity:0; transform: scale(0.9); }
          to   { opacity:1; transform: scale(1);   }
        }
      `}</style>

      <div className="max-w-3xl mx-auto">

        {/* Header */}
        <div className="text-center mb-10">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-xs mb-5"
            style={{ background: `rgba(${theme.accentRgb},0.1)`, color: theme.accentLight, border: `1px solid rgba(${theme.accentRgb},0.2)` }}>
            <span className="w-1.5 h-1.5 rounded-full animate-pulse" style={{ background: theme.accent }} />
            Classements en temps réel
          </div>
          <h1 className="text-5xl font-black mb-2" style={{
            backgroundImage: `linear-gradient(135deg, #fff 20%, ${theme.accent} 60%, ${theme.gradientTo})`,
            WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent', backgroundClip: 'text',
          }}>
            Classements
          </h1>
          <p className="text-rolld-muted text-sm">Top 10 joueurs par catégorie · rafraîchi toutes les 30s</p>
        </div>

        {/* Tabs */}
        <div className="flex flex-wrap gap-2 justify-center mb-8">
          {TABS.map((t, i) => (
            <button key={t.key} onClick={() => setActiveTab(i)}
              className="relative px-4 py-2.5 rounded-xl text-sm font-medium transition-all duration-300"
              style={activeTab === i ? {
                background: `linear-gradient(135deg, ${theme.accent}, ${theme.gradientTo})`,
                color: '#fff',
                boxShadow: `0 4px 20px rgba(${theme.accentRgb},0.4)`,
                transform: 'scale(1.06)',
                animation: 'tabIn 0.2s ease',
              } : {
                background: 'var(--rolld-surface,#1a1a2e)',
                border: '1px solid var(--rolld-border,#2a2a3e)',
                color: 'var(--rolld-muted,#6b7280)',
              }}>
              <span className="mr-1.5">{t.icon}</span>{t.label}
            </button>
          ))}
        </div>

        {/* Board */}
        <div className="relative rounded-2xl overflow-hidden border border-rolld-border"
          style={{ background: 'var(--rolld-surface,#1a1a2e)' }}>

          {/* Top bar */}
          <div className="px-6 py-4 flex items-center justify-between border-b border-rolld-border"
            style={{ background: `linear-gradient(90deg, rgba(${theme.accentRgb},0.07) 0%, transparent 100%)` }}>
            <div className="flex items-center gap-2">
              <span className="text-xl">{tab.icon}</span>
              <span className="font-bold text-rolld-text">{tab.label}</span>
              {rows.length > 0 && (
                <span className="text-xs px-2 py-0.5 rounded-full font-mono"
                  style={{ background: `rgba(${theme.accentRgb},0.12)`, color: theme.accentLight, animation: 'countBadge 0.3s ease' }}>
                  {rows.length}
                </span>
              )}
            </div>
            <div className="flex items-center gap-3">
              {lastRefresh && (
                <span className="text-rolld-muted text-xs opacity-50">
                  {lastRefresh.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
                </span>
              )}
              <button onClick={() => fetchLeaderboard(tab.key)} disabled={loading}
                className="text-xs transition-all duration-200 hover:scale-110 disabled:opacity-30"
                style={{ color: theme.accentLight }}>
                {loading
                  ? <span className="inline-block w-3.5 h-3.5 border-2 rounded-full animate-spin"
                      style={{ borderColor: `${theme.accent} transparent transparent transparent` }} />
                  : '↻'}
              </button>
            </div>
          </div>

          {/* Content */}
          {loading && rows.length === 0 ? (
            <div className="py-20 flex flex-col items-center gap-4">
              <div className="relative w-12 h-12">
                <div className="absolute inset-0 rounded-full border-2 border-t-transparent animate-spin"
                  style={{ borderColor: `${theme.accent} transparent transparent transparent` }} />
                <div className="absolute inset-2 rounded-full border-2 border-b-transparent animate-spin"
                  style={{ borderColor: `${theme.gradientTo} transparent transparent transparent`, animationDirection: 'reverse', animationDuration: '0.7s' }} />
              </div>
              <span className="text-rolld-muted text-sm">Chargement…</span>
            </div>
          ) : rows.length === 0 ? (
            <div className="py-20 text-center">
              <div className="text-5xl mb-4 animate-bounce">🎮</div>
              <p className="text-rolld-text font-semibold mb-1">Aucune donnée pour l'instant</p>
              <p className="text-rolld-muted text-sm">Jouez une partie pour apparaître ici !</p>
            </div>
          ) : (
            <div>
              {rows.map((row, i) => {
                const visible = visibleRows.includes(i)
                const m = MEDAL[i] || null
                const pct = Math.max(3, Math.round((row.value / maxVal) * 100))

                return (
                  <div key={row.name}
                    className="relative overflow-hidden border-b border-rolld-border/30 last:border-0 group"
                    style={{
                      opacity: visible ? 1 : 0,
                      transform: visible ? 'translateX(0)' : 'translateX(-16px)',
                      transition: `opacity 0.4s ease ${i * 45}ms, transform 0.4s ease ${i * 45}ms`,
                      animation: i === 0 && visible ? 'glowGold 2.5s ease-in-out infinite' : 'none',
                    }}>

                    {/* Progress bar fill */}
                    <div className="absolute inset-y-0 left-0 transition-all ease-out"
                      style={{
                        width: visible ? `${pct}%` : '0%',
                        transitionDuration: '900ms',
                        transitionDelay: `${i * 45 + 150}ms`,
                        background: i === 0
                          ? `linear-gradient(90deg, rgba(243,156,18,0.12), transparent)`
                          : i < 3
                          ? `linear-gradient(90deg, ${m.bg}, transparent)`
                          : `linear-gradient(90deg, rgba(${theme.accentRgb},0.04), transparent)`,
                      }} />

                    {/* Shimmer on hover */}
                    <div className="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-300"
                      style={{ background: 'linear-gradient(90deg, transparent, rgba(255,255,255,0.02), transparent)' }} />

                    <div className="relative flex items-center gap-4 px-5 py-3.5">
                      {/* Rank */}
                      <div className="w-9 flex justify-center shrink-0">
                        {i < 3
                          ? <span className="text-xl leading-none">{m.icon}</span>
                          : <span className="text-sm font-bold text-rolld-muted/60">#{i + 1}</span>}
                      </div>

                      {/* Name + badge */}
                      <div className="flex-1 flex items-center gap-2 min-w-0">
                        <span className="font-semibold truncate" style={{ color: m ? m.color : 'var(--rolld-text,#e2e8f0)' }}>
                          {row.name}
                        </span>
                        {i === 0 && (
                          <span className="text-xs px-1.5 py-0.5 rounded-md shrink-0 font-medium"
                            style={{ background: 'rgba(243,156,18,0.18)', color: '#f39c12' }}>
                            champion
                          </span>
                        )}
                      </div>

                      {/* Value */}
                      <div className="shrink-0 text-right">
                        <span className="font-mono font-bold"
                          style={{ color: m ? m.color : theme.accentLight, fontSize: i === 0 ? '1.05rem' : '0.875rem' }}>
                          {tab.format(row.value)}
                        </span>
                        {tab.unit && <span className="text-rolld-muted text-xs ml-1">{tab.unit}</span>}
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </div>

        <p className="text-center text-rolld-muted/40 text-xs mt-4">
          Statistiques cumulées · identifiées par pseudo
        </p>
      </div>
    </div>
  )
}
