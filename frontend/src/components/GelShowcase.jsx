const gels = [
  {
    name: 'Gel Orange',
    emoji: '🟠',
    description: 'Surface de boost — multiplie votre vitesse. Prenez de l\'élan et catapultez-vous vers de nouveaux sommets.',
    gradient: 'from-orange-500 to-amber-600',
    borderColor: 'border-orange-500/30',
    glowColor: 'hover:shadow-orange-500/20',
    bgGlow: 'bg-orange-500/5',
  },
  {
    name: 'Gel Violet',
    emoji: '🟣',
    description: 'Surface sticky — collez aux murs et plafonds. La gravité s\'inverse pour vous plaquer contre la surface.',
    gradient: 'from-purple-500 to-violet-600',
    borderColor: 'border-purple-500/30',
    glowColor: 'hover:shadow-purple-500/20',
    bgGlow: 'bg-purple-500/5',
  },
  {
    name: 'Gel Bleu',
    emoji: '🔵',
    description: 'Surface rebondissante — transformez votre bille en super balle. Plus vous arrivez vite, plus vous rebondissez haut.',
    gradient: 'from-blue-500 to-cyan-600',
    borderColor: 'border-blue-500/30',
    glowColor: 'hover:shadow-blue-500/20',
    bgGlow: 'bg-blue-500/5',
  },
]

export default function GelShowcase() {
  return (
    <section id="gels" className="py-24 px-4">
      <div className="max-w-6xl mx-auto">
        {/* Section header */}
        <div className="text-center mb-16">
          <h2 className="text-3xl md:text-5xl font-bold text-rolld-text mb-4">
            Trois gels, infinies possibilités
          </h2>
          <p className="text-rolld-muted text-lg max-w-xl mx-auto">
            Chaque surface change radicalement votre physique. Maîtrisez-les pour dominer l'arène.
          </p>
        </div>

        {/* Gel cards */}
        <div className="grid md:grid-cols-3 gap-6">
          {gels.map((gel) => (
            <div
              key={gel.name}
              className={`group relative rounded-2xl border ${gel.borderColor} bg-rolld-surface p-8 transition-all duration-500 hover:scale-[1.02] hover:shadow-2xl ${gel.glowColor}`}
            >
              {/* Glow effect */}
              <div className={`absolute inset-0 rounded-2xl ${gel.bgGlow} opacity-0 group-hover:opacity-100 transition-opacity duration-500`} />
              
              <div className="relative z-10">
                {/* Icon */}
                <div className="text-5xl mb-4">{gel.emoji}</div>
                
                {/* Title */}
                <h3 className={`text-xl font-bold mb-3 bg-gradient-to-r ${gel.gradient} bg-clip-text text-transparent`}>
                  {gel.name}
                </h3>
                
                {/* Description */}
                <p className="text-rolld-muted leading-relaxed text-sm">
                  {gel.description}
                </p>
              </div>
            </div>
          ))}
        </div>

        {/* Controls hint */}
        <div className="mt-16 glass rounded-2xl p-8 text-center">
          <h3 className="text-xl font-semibold text-rolld-text mb-6">Contrôles</h3>
          <div className="flex flex-wrap justify-center gap-6">
            {[
              { keys: 'ZQSD', label: 'Mouvement' },
              { keys: 'ESPACE', label: 'Sauter (maintenir = +force)' },
              { keys: 'SOURIS', label: 'Caméra' },
              { keys: 'CLIC DROIT', label: 'Libérer le curseur' },
            ].map((control) => (
              <div key={control.keys} className="flex flex-col items-center gap-2">
                <kbd className="px-3 py-1.5 rounded-lg bg-rolld-bg border border-rolld-border text-rolld-accent-light font-mono text-sm">
                  {control.keys}
                </kbd>
                <span className="text-rolld-muted text-xs">{control.label}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}
