import { IS_DEV, theme } from '../env'

export default function DevBanner() {
  if (!IS_DEV) return null

  return (
    <div
      className="fixed top-0 left-0 right-0 z-[100] flex items-center justify-center gap-2 py-1.5 text-xs font-mono tracking-wider text-black/80 select-none"
      style={{
        background: `repeating-linear-gradient(
          -45deg,
          ${theme.accent},
          ${theme.accent} 10px,
          ${theme.gradientTo} 10px,
          ${theme.gradientTo} 20px
        )`,
      }}
    >
      <span>{theme.badge}</span>
      <span className="font-bold uppercase">Environnement de développement</span>
      <span>—</span>
      <span className="opacity-70">Ne pas utiliser en conditions réelles</span>
      <span>{theme.badge}</span>
    </div>
  )
}
