export default function Footer() {
  return (
    <footer className="py-8 px-4 border-t border-rolld-border/50">
      <div className="max-w-6xl mx-auto flex flex-col md:flex-row items-center justify-between gap-4 text-sm text-rolld-muted">
        <div className="flex items-center gap-2">
          <span className="font-bold text-rolld-text">ROLL'D</span>
          <span>·</span>
          <span>Marble MMO</span>
        </div>
        <div className="flex items-center gap-4 text-xs">
          <span>Unity · React · Colyseus</span>
          <span>·</span>
          <span className="text-rolld-accent/60">v0.1.2-dev</span>
          <span>·</span>
          <a
            href="https://git.kerboul.me/kerboul/rolld_frontend"
            target="_blank"
            rel="noopener noreferrer"
            className="hover:text-rolld-accent-light transition-colors"
          >
            Source
          </a>
        </div>
      </div>
    </footer>
  )
}
