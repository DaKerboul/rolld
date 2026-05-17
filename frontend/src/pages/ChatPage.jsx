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
  const lastTimestampRef = useRef(0)
  const bottomRef = useRef(null)
  const inputRef = useRef(null)

  const fetchMessages = useCallback(async () => {
    try {
      const res = await fetch(`${SERVER}/chat/history?since=${lastTimestampRef.current}`)
      if (!res.ok) return
      const data = await res.json()
      if (!Array.isArray(data) || data.length === 0) return
      setMessages(prev => {
        const updated = [...prev, ...data]
        // Deduplicate by id
        const seen = new Set()
        const deduped = updated.filter(m => {
          if (seen.has(m.id)) return false
          seen.add(m.id)
          return true
        })
        return deduped.slice(-200)
      })
      const maxTs = Math.max(...data.map(m => m.timestamp))
      if (maxTs > lastTimestampRef.current) lastTimestampRef.current = maxTs
    } catch {
      // silently ignore network errors
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
      // Immediately poll to get our own message back
      await fetchMessages()
    } catch {
      // ignore
    } finally {
      setSending(false)
      inputRef.current?.focus()
    }
  }

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      sendMessage()
    }
  }

  const formatTime = (ts) => {
    const d = new Date(ts)
    return d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
  }

  return (
    <div className="min-h-screen pt-20 px-4 flex flex-col">
      <div className="max-w-2xl mx-auto w-full flex flex-col flex-1" style={{ maxHeight: 'calc(100vh - 5rem)' }}>

        {/* Header */}
        <div className="mb-4 flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-black text-rolld-text">Chat général</h1>
            <p className="text-rolld-muted text-sm mt-1">Partagé entre le jeu et le site — rafraîchi toutes les 3s</p>
          </div>

          {/* Name badge */}
          {!editingName ? (
            <button
              onClick={() => { setNameInput(playerName); setEditingName(true) }}
              className="flex items-center gap-2 px-3 py-1.5 rounded-xl border border-rolld-border bg-rolld-surface text-sm hover:border-rolld-accent/40 transition-colors"
            >
              <span className="text-rolld-muted">Joueur :</span>
              <span className="font-bold" style={{ color: theme.accentLight }}>{playerName}</span>
              <span className="text-rolld-muted text-xs">✏️</span>
            </button>
          ) : (
            <div className="flex items-center gap-2">
              <input
                autoFocus
                value={nameInput}
                onChange={e => setNameInput(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && saveName()}
                placeholder="Ton pseudo"
                maxLength={24}
                className="w-36 px-3 py-1.5 rounded-xl border border-rolld-accent/60 bg-rolld-surface text-rolld-text text-sm outline-none focus:border-rolld-accent"
              />
              <button
                onClick={saveName}
                className="px-3 py-1.5 rounded-xl text-sm font-bold text-white"
                style={{ background: theme.accent }}
              >
                OK
              </button>
            </div>
          )}
        </div>

        {/* Messages */}
        <div className="flex-1 overflow-y-auto rounded-2xl border border-rolld-border bg-rolld-surface p-4 space-y-1 min-h-0">
          {messages.length === 0 ? (
            <div className="h-full flex items-center justify-center text-rolld-muted text-sm">
              Aucun message pour l'instant. Soyez le premier !
            </div>
          ) : (
            messages.map(msg => (
              <div key={msg.id} className="flex items-baseline gap-2 py-0.5 group">
                <span className="text-rolld-muted text-xs font-mono shrink-0 opacity-60 group-hover:opacity-100 transition-opacity w-11">
                  {formatTime(msg.timestamp)}
                </span>
                <span className="text-sm font-bold shrink-0" style={{ color: theme.accentLight }}>
                  {msg.name}
                </span>
                <span className="text-sm text-rolld-text break-words min-w-0">
                  {msg.text}
                </span>
              </div>
            ))
          )}
          <div ref={bottomRef} />
        </div>

        {/* Input */}
        <div className="mt-3 flex gap-2 pb-4">
          <input
            ref={inputRef}
            value={inputText}
            onChange={e => setInputText(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={playerName ? 'Écrire un message… (Entrée pour envoyer)' : 'Choisissez d\'abord un pseudo →'}
            maxLength={200}
            disabled={!playerName || editingName}
            className="flex-1 px-4 py-3 rounded-xl border border-rolld-border bg-rolld-surface text-rolld-text text-sm outline-none focus:border-rolld-accent/60 disabled:opacity-40 transition-colors"
          />
          <button
            onClick={sendMessage}
            disabled={!inputText.trim() || !playerName || sending || editingName}
            className="px-5 py-3 rounded-xl text-sm font-bold text-white transition-all duration-200 hover:scale-105 disabled:opacity-40 disabled:hover:scale-100"
            style={{ backgroundImage: `linear-gradient(to right, ${theme.accent}, ${theme.gradientTo})` }}
          >
            Envoyer
          </button>
        </div>
      </div>
    </div>
  )
}
