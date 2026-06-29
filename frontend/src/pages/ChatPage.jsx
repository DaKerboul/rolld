import { useState, useEffect, useRef, useCallback } from 'react'
import { theme } from '../env'

const SERVER = 'https://game.rolld.kerboul.me'
const POLL_INTERVAL = 3000

export default function ChatPage() {
  const [messages, setMessages] = useState([])
  const [inputText, setInputText] = useState('')
  const [playerName, setPlayerName] = useState(() => localStorage.getItem('rolld_chat_name') || '')
  const [editingName, setEditingName] = useState(!localStorage.getItem('rolld_chat_name'))
  const [nameInput, setNameInput] = useState(playerName)
  const [sending, setSending] = useState(false)
  const [newIds, setNewIds] = useState(new Set())
  const [online, setOnline] = useState(true)
  const lastTimestampRef = useRef(0)
  const seenIdsRef = useRef(new Set())
  const bottomRef = useRef(null)
  const inputRef = useRef(null)
  const containerRef = useRef(null)

  const fetchMessages = useCallback(async () => {
    try {
      const res = await fetch(`${SERVER}/chat/history?since=${lastTimestampRef.current}`)
      if (!res.ok) { setOnline(false); return }
      setOnline(true)
      const data = await res.json()
      if (!Array.isArray(data) || data.length === 0) return

      const freshIds = data.filter(m => !seenIdsRef.current.has(m.id)).map(m => m.id)
      freshIds.forEach(id => seenIdsRef.current.add(id))

      setMessages(prev => {
        const merged = [...prev, ...data]
        const seen = new Set()
        return merged.filter(m => { if (seen.has(m.id)) return false; seen.add(m.id); return true }).slice(-200)
      })

      const maxTs = Math.max(...data.map(m => m.timestamp))
      if (maxTs > lastTimestampRef.current) lastTimestampRef.current = maxTs

      if (freshIds.length > 0) {
        setNewIds(new Set(freshIds))
        setTimeout(() => setNewIds(new Set()), 800)
      }
    } catch {
      setOnline(false)
    }
  }, [])

  useEffect(() => {
    fetchMessages()
    const id = setInterval(fetchMessages, POLL_INTERVAL)
    return () => clearInterval(id)
  }, [fetchMessages])

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const saveName = () => {
    const n = nameInput.trim()
    if (!n) return
    setPlayerName(n)
    localStorage.setItem('rolld_chat_name', n)
    setEditingName(false)
    setTimeout(() => inputRef.current?.focus(), 50)
  }

  const sendMessage = async () => {
    const text = inputText.trim()
    if (!text || !playerName || sending) return
    setSending(true)
    setInputText('')
    try {
      await fetch(`${SERVER}/chat/send`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: playerName, text }),
      })
      await fetchMessages()
    } catch { /* ignore */ } finally {
      setSending(false)
      inputRef.current?.focus()
    }
  }

  const formatTime = ts => new Date(ts).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })

  const nameColor = (name) => {
    let h = 0
    for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) & 0xffff
    const hue = (h % 280) + 160
    return `hsl(${hue},70%,68%)`
  }

  return (
    <div className="min-h-screen pt-20 px-4 flex flex-col">
      <style>{`
        @keyframes msgSlide {
          from { opacity: 0; transform: translateX(-10px); }
          to   { opacity: 1; transform: translateX(0); }
        }
        @keyframes msgGlow {
          0%,100% { background: transparent; }
          30%     { background: rgba(${theme.accentRgb ?? '99,102,241'},0.07); }
        }
        @keyframes sendPulse {
          0%   { box-shadow: 0 0 0 0 rgba(${theme.accentRgb ?? '99,102,241'},0.5); }
          70%  { box-shadow: 0 0 0 8px rgba(${theme.accentRgb ?? '99,102,241'},0); }
          100% { box-shadow: 0 0 0 0 rgba(${theme.accentRgb ?? '99,102,241'},0); }
        }
        @keyframes livePulse {
          0%,100% { opacity: 1; }
          50%     { opacity: 0.3; }
        }
        @keyframes scanLine {
          0%   { top: 0%; opacity: 0.4; }
          100% { top: 100%; opacity: 0; }
        }
      `}</style>

      <div className="max-w-2xl mx-auto w-full flex flex-col flex-1" style={{ maxHeight: 'calc(100vh - 5rem)' }}>

        {/* Header */}
        <div className="mb-4 relative rounded-2xl overflow-hidden border border-rolld-border p-5"
          style={{ background: `linear-gradient(135deg, var(--rolld-surface,#1a1a2e), rgba(${theme.accentRgb},0.04))` }}>

          {/* Scan line effect */}
          <div className="absolute left-0 right-0 h-px pointer-events-none"
            style={{
              background: `linear-gradient(90deg, transparent, rgba(${theme.accentRgb},0.4), transparent)`,
              animation: 'scanLine 4s linear infinite',
            }} />

          <div className="flex items-start justify-between">
            <div>
              <div className="flex items-center gap-2 mb-1">
                <h1 className="text-2xl font-black text-rolld-text">Chat général</h1>
                <div className="flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs"
                  style={{ background: online ? 'rgba(34,197,94,0.12)' : 'rgba(239,68,68,0.12)',
                           color: online ? '#4ade80' : '#f87171',
                           border: `1px solid ${online ? 'rgba(34,197,94,0.2)' : 'rgba(239,68,68,0.2)'}` }}>
                  <span style={{ animation: online ? 'livePulse 2s ease-in-out infinite' : 'none',
                                 width: '6px', height: '6px', borderRadius: '50%',
                                 background: online ? '#4ade80' : '#f87171', display: 'inline-block' }} />
                  {online ? 'LIVE' : 'Hors ligne'}
                </div>
              </div>
              <p className="text-rolld-muted text-xs">Partagé entre le jeu et le site · rafraîchi toutes les 3s</p>
            </div>

            {/* Name badge */}
            {!editingName ? (
              <button onClick={() => { setNameInput(playerName); setEditingName(true) }}
                className="flex items-center gap-2 px-3 py-2 rounded-xl border transition-all duration-200 hover:border-rolld-accent/50 text-sm shrink-0"
                style={{ background: 'var(--rolld-bg,#0d0d1a)', borderColor: 'var(--rolld-border,#2a2a3e)' }}>
                <span className="font-bold text-xs" style={{ color: nameColor(playerName) }}>
                  {playerName}
                </span>
                <span className="text-rolld-muted text-xs">✏️</span>
              </button>
            ) : (
              <div className="flex items-center gap-2 shrink-0">
                <input autoFocus value={nameInput} onChange={e => setNameInput(e.target.value)}
                  onKeyDown={e => e.key === 'Enter' && saveName()}
                  placeholder="Pseudo" maxLength={24}
                  className="w-28 px-3 py-1.5 rounded-xl text-sm outline-none border text-rolld-text"
                  style={{ background: 'var(--rolld-bg,#0d0d1a)', borderColor: theme.accent }} />
                <button onClick={saveName} className="px-3 py-1.5 rounded-xl text-sm font-bold text-white"
                  style={{ background: theme.accent }}>OK</button>
              </div>
            )}
          </div>
        </div>

        {/* Messages */}
        <div ref={containerRef}
          className="relative flex-1 overflow-y-auto rounded-2xl border border-rolld-border p-4 min-h-0"
          style={{ background: 'var(--rolld-surface,#1a1a2e)' }}>

          {/* Bottom fade */}
          <div className="sticky bottom-0 left-0 right-0 h-8 pointer-events-none z-10"
            style={{ background: 'linear-gradient(to top, var(--rolld-surface,#1a1a2e), transparent)', marginTop: '-2rem' }} />

          {messages.length === 0 ? (
            <div className="h-full flex flex-col items-center justify-center gap-3 text-center py-12">
              <div className="text-4xl animate-bounce">💬</div>
              <p className="text-rolld-muted text-sm">Aucun message pour l'instant.</p>
              <p className="text-rolld-muted/50 text-xs">Soyez le premier à écrire !</p>
            </div>
          ) : (
            <div className="space-y-0.5 pb-4">
              {messages.map((msg) => {
                const isNew = newIds.has(msg.id)
                const isMe = msg.name === playerName
                return (
                  <div key={msg.id}
                    className="flex items-baseline gap-2 py-1 px-2 rounded-lg group transition-colors"
                    style={{
                      animation: isNew ? 'msgSlide 0.3s ease, msgGlow 0.8s ease' : 'none',
                      background: isMe ? `rgba(${theme.accentRgb},0.04)` : 'transparent',
                    }}>
                    <span className="text-rolld-muted/40 text-xs font-mono shrink-0 w-10 group-hover:text-rolld-muted/70 transition-colors">
                      {formatTime(msg.timestamp)}
                    </span>
                    <span className="text-sm font-bold shrink-0 cursor-default select-none"
                      style={{ color: nameColor(msg.name) }}
                      title={msg.name}>
                      {msg.name}
                    </span>
                    <span className="text-sm break-words min-w-0"
                      style={{ color: isMe ? 'var(--rolld-text,#e2e8f0)' : 'var(--rolld-muted,#9ca3af)' }}>
                      {msg.text}
                    </span>
                  </div>
                )
              })}
              <div ref={bottomRef} />
            </div>
          )}
        </div>

        {/* Input */}
        <div className="mt-3 pb-4 flex gap-2">
          <input ref={inputRef} value={inputText} onChange={e => setInputText(e.target.value)}
            onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage() } }}
            placeholder={playerName ? 'Écrire un message… (Entrée pour envoyer)' : 'Choisissez d\'abord un pseudo →'}
            maxLength={200} disabled={!playerName || editingName}
            className="flex-1 px-4 py-3 rounded-xl border text-sm outline-none transition-all duration-200 disabled:opacity-40 text-rolld-text"
            style={{
              background: 'var(--rolld-surface,#1a1a2e)',
              borderColor: inputText ? `rgba(${theme.accentRgb},0.5)` : 'var(--rolld-border,#2a2a3e)',
            }} />
          <button onClick={sendMessage}
            disabled={!inputText.trim() || !playerName || sending || editingName}
            className="px-5 py-3 rounded-xl text-sm font-bold text-white transition-all duration-200 hover:scale-105 disabled:opacity-40 disabled:hover:scale-100 disabled:cursor-not-allowed"
            style={{
              backgroundImage: `linear-gradient(135deg, ${theme.accent}, ${theme.gradientTo})`,
              animation: sending ? 'sendPulse 0.6s ease' : 'none',
            }}>
            {sending ? (
              <span className="inline-block w-4 h-4 border-2 rounded-full animate-spin"
                style={{ borderColor: 'rgba(255,255,255,0.8) transparent transparent transparent' }} />
            ) : '↑'}
          </button>
        </div>
      </div>
    </div>
  )
}
