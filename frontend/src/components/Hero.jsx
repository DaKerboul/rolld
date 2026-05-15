import { IS_DEV, theme } from '../env'

export default function Hero({ onPlay }) {
  return (
    <section className="relative min-h-screen flex flex-col items-center justify-center px-4 overflow-hidden">
      {/* Background effects */}
      <div className="absolute inset-0 pointer-events-none">
        {/* Gradient orbs */}
        <div
          className="absolute top-1/4 left-1/4 w-[500px] h-[500px] rounded-full blur-[120px] animate-float"
          style={{ background: `rgba(${theme.accentRgb}, 0.1)` }}
        />
        <div className="absolute bottom-1/4 right-1/4 w-[400px] h-[400px] rounded-full bg-rolld-orange/8 blur-[100px] animate-float" style={{ animationDelay: '-3s' }} />
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[300px] h-[300px] rounded-full bg-rolld-violet/8 blur-[80px]" />
        
        {/* Grid */}
        <div className="absolute inset-0 opacity-[0.03]"
          style={{
            backgroundImage: `linear-gradient(rgba(${theme.accentRgb},0.5) 1px, transparent 1px), linear-gradient(90deg, rgba(${theme.accentRgb},0.5) 1px, transparent 1px)`,
            backgroundSize: '60px 60px',
          }}
        />
      </div>

      {/* Content */}
      <div className="relative z-10 text-center max-w-4xl mx-auto animate-slide-up">
        {/* Badge */}
        <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full glass text-sm mb-8" style={{ color: theme.accentLight }}>
          <span className="w-2 h-2 rounded-full animate-pulse" style={{ background: theme.accent }} />
          {IS_DEV ? '🚧 DEV · ' : ''}Marble MMO · Multijoueur temps réel
        </div>

        {/* Title */}
        <h1 className="text-6xl md:text-8xl font-black tracking-tight mb-6">
          <span className="text-rolld-text">ROLL</span>
          <span
            className="text-transparent bg-clip-text"
            style={{ backgroundImage: `linear-gradient(to right, ${theme.accent}, ${theme.gradientTo}, ${IS_DEV ? '#e67e22' : '#f39c12'})` }}
          >'D</span>
        </h1>

        {/* Subtitle */}
        <p className="text-lg md:text-xl text-rolld-muted max-w-2xl mx-auto mb-12 leading-relaxed">
          Un monde de billes, de gels et de physique. Roulez sur des surfaces qui boostent, 
          collent ou font rebondir — et affrontez d'autres joueurs en temps réel.
        </p>

        {/* CTA Buttons */}
        <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
          <button
            onClick={onPlay}
            className="group relative px-8 py-4 rounded-2xl text-white font-bold text-lg transition-all duration-300 hover:scale-105"
            style={{
              backgroundImage: `linear-gradient(to right, ${theme.accent}, ${theme.gradientTo})`,
              boxShadow: `0 0 30px rgba(${theme.accentRgb}, 0.4)`,
            }}
          >
            <span className="relative z-10 flex items-center gap-3">
              <svg className="w-6 h-6 group-hover:translate-x-0.5 transition-transform" fill="currentColor" viewBox="0 0 20 20">
                <path d="M6.3 2.841A1.5 1.5 0 004 4.11V15.89a1.5 1.5 0 002.3 1.269l9.344-5.89a1.5 1.5 0 000-2.538L6.3 2.84z" />
              </svg>
              Jouer maintenant
            </span>
          </button>

          <a
            href="#gels"
            className="px-6 py-3 rounded-xl border border-rolld-border text-rolld-muted hover:text-rolld-text hover:border-rolld-accent/40 transition-all duration-300"
          >
            Découvrir les mécaniques ↓
          </a>
        </div>
      </div>

      {/* Scroll indicator */}
      <div className="absolute bottom-8 left-1/2 -translate-x-1/2 animate-bounce">
        <div className="w-5 h-8 rounded-full border-2 border-rolld-muted/30 flex items-start justify-center p-1">
          <div className="w-1 h-2 rounded-full bg-rolld-muted/50" />
        </div>
      </div>
    </section>
  )
}
