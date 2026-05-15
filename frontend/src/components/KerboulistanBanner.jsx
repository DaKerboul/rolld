export default function KerboulistanBanner() {
  return (
    <section className="relative py-20 px-4 overflow-hidden">
      {/* Transition gradient — ROLL'D dark to gov gold tint and back */}
      <div className="absolute inset-0 pointer-events-none">
        <div className="absolute inset-0 bg-gradient-to-b from-rolld-bg via-[#0d0c08] to-rolld-bg" />
        {/* Subtle gold fog */}
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[300px] rounded-full bg-amber-500/[0.04] blur-[100px]" />
        {/* Scanlines — bureaucratic CRT vibe */}
        <div
          className="absolute inset-0 opacity-[0.015]"
          style={{
            backgroundImage: 'repeating-linear-gradient(0deg, transparent, transparent 2px, rgba(217,175,78,0.4) 2px, rgba(217,175,78,0.4) 3px)',
          }}
        />
      </div>

      <div className="relative z-10 max-w-3xl mx-auto text-center">
        {/* Classification header */}
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded border border-amber-700/30 bg-amber-900/10 mb-8">
          <span className="w-1.5 h-1.5 rounded-full bg-amber-500/80 animate-pulse" />
          <span className="text-[10px] tracking-[0.2em] uppercase text-amber-500/60 font-mono">
            Directive n°2026-042 · Programme homologué
          </span>
        </div>

        {/* Government seal / emblem area */}
        <div className="flex items-center justify-center gap-4 mb-6">
          <div className="h-px flex-1 max-w-[80px] bg-gradient-to-r from-transparent to-amber-600/30" />
          <a
            href="https://gov.kerboul.me"
            target="_blank"
            rel="noopener noreferrer"
            className="group relative"
          >
            {/* Seal ring */}
            <div className="w-20 h-20 rounded-full border border-amber-600/30 group-hover:border-amber-500/50 flex items-center justify-center transition-all duration-500 group-hover:shadow-[0_0_30px_rgba(217,175,78,0.1)]">
              <div className="w-16 h-16 rounded-full border border-amber-700/20 flex items-center justify-center">
                <span className="text-2xl grayscale group-hover:grayscale-0 transition-all duration-500">🏛️</span>
              </div>
            </div>
            {/* Seal text ring (simulated) */}
            <div className="absolute -inset-2 rounded-full border border-dashed border-amber-800/15 group-hover:border-amber-700/25 transition-colors duration-500 animate-[spin_60s_linear_infinite]" />
          </a>
          <div className="h-px flex-1 max-w-[80px] bg-gradient-to-l from-transparent to-amber-600/30" />
        </div>

        {/* Title — gov style */}
        <h2 className="text-xs tracking-[0.3em] uppercase text-amber-500/50 font-medium mb-2">
          République Libre Populaire Démocratique
        </h2>
        <h3 className="text-2xl md:text-3xl font-bold tracking-tight mb-2">
          <span className="text-amber-100/80">du </span>
          <a
            href="https://gov.kerboul.me"
            target="_blank"
            rel="noopener noreferrer"
            className="text-transparent bg-clip-text bg-gradient-to-r from-amber-400 to-amber-600 hover:from-amber-300 hover:to-amber-500 transition-all duration-300"
          >
            Kerboulistan
          </a>
        </h3>

        {/* Divider */}
        <div className="flex items-center justify-center gap-3 my-6">
          <div className="h-px w-12 bg-amber-700/30" />
          <span className="text-amber-600/30 text-xs">✦</span>
          <div className="h-px w-12 bg-amber-700/30" />
        </div>

        {/* Body text — bureaucratic tone */}
        <p className="text-rolld-muted/70 text-sm leading-relaxed max-w-lg mx-auto mb-2">
          Ce programme vidéoludique a été développé sous l'autorité directe du{' '}
          <span className="text-amber-400/60">Couple Présidentiel</span> et financé par le{' '}
          <span className="text-amber-400/60">Ministère de la Défense</span> dans le cadre de
          la Directive de Divertissement Stratégique.
        </p>
        <p className="text-rolld-muted/40 text-xs leading-relaxed max-w-md mx-auto mb-8">
          Budget alloué : 35 Memes ($K) · Temps de développement : classifié ·
          Taux de satisfaction prévu : 420%
        </p>

        {/* Stamps / tags */}
        <div className="flex flex-wrap items-center justify-center gap-3 mb-8">
          {[
            { label: 'Homologué RLPDK', icon: '🛡️' },
            { label: 'Approuvé FBC', icon: '📡' },
            { label: 'MEMECON 5', icon: '🟢' },
          ].map((stamp) => (
            <div
              key={stamp.label}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded border border-amber-800/20 bg-amber-900/5 text-amber-500/40 text-[10px] tracking-[0.15em] uppercase font-mono"
            >
              <span className="text-xs">{stamp.icon}</span>
              {stamp.label}
            </div>
          ))}
        </div>

        {/* CTA — gov link */}
        <a
          href="https://gov.kerboul.me"
          target="_blank"
          rel="noopener noreferrer"
          className="group inline-flex items-center gap-2 px-5 py-2.5 rounded-lg border border-amber-700/25 hover:border-amber-600/40 bg-amber-900/5 hover:bg-amber-900/10 transition-all duration-300"
        >
          <span className="text-amber-500/50 group-hover:text-amber-400/70 text-xs tracking-[0.1em] uppercase transition-colors duration-300">
            Consulter le Gouvernement
          </span>
          <svg className="w-3 h-3 text-amber-600/40 group-hover:text-amber-500/60 group-hover:translate-x-0.5 transition-all duration-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
          </svg>
        </a>

        {/* Bottom motto */}
        <p className="mt-8 text-[9px] tracking-[0.25em] uppercase text-amber-700/25 font-mono">
          « Endgame, Combined Arms, et Zougoulag ! »
        </p>
      </div>
    </section>
  )
}
