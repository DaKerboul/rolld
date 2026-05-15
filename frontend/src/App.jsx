import { useState } from 'react'
import { IS_DEV } from './env'
import DevBanner from './components/DevBanner'
import Hero from './components/Hero'
import GelShowcase from './components/GelShowcase'
import KerboulistanBanner from './components/KerboulistanBanner'
import GameCanvas from './components/GameCanvas'
import Footer from './components/Footer'

function App() {
  const [isPlaying, setIsPlaying] = useState(false)

  if (isPlaying) {
    return <GameCanvas onBack={() => setIsPlaying(false)} />
  }

  return (
    <div className="min-h-screen">
      <DevBanner />
      {/* Offset content when dev banner is visible */}
      {IS_DEV && <div className="h-8" />}
      <Hero onPlay={() => setIsPlaying(true)} />
      <GelShowcase />
      <KerboulistanBanner />
      <Footer />
    </div>
  )
}

export default App
