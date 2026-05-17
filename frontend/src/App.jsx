import { useState } from 'react'
import { IS_DEV } from './env'
import DevBanner from './components/DevBanner'
import NavBar from './components/NavBar'
import Hero from './components/Hero'
import GelShowcase from './components/GelShowcase'
import KerboulistanBanner from './components/KerboulistanBanner'
import GameCanvas from './components/GameCanvas'
import Footer from './components/Footer'
import StatsPage from './pages/StatsPage'
import ChatPage from './pages/ChatPage'

function App() {
  const [page, setPage] = useState('home')

  if (page === 'play') {
    return <GameCanvas onBack={() => setPage('home')} />
  }

  return (
    <div className="min-h-screen">
      <DevBanner />
      <NavBar page={page} setPage={setPage} />

      {page === 'home' && (
        <>
          {IS_DEV && <div className="h-8" />}
          <div className="pt-14">
            <Hero onPlay={() => setPage('play')} />
            <GelShowcase />
            <KerboulistanBanner />
            <Footer />
          </div>
        </>
      )}

      {page === 'stats' && <StatsPage />}
      {page === 'chat' && <ChatPage />}
    </div>
  )
}

export default App
