import { useState, useEffect, useCallback } from 'react'

// Check if Unity build files exist
const UNITY_BUILD_PATH = '/unity-build/Build'
// Cache-busting version — update this after each Unity build
const UNITY_BUILD_VERSION = '20260520d'
const BUILD_PREFIX = 'build_ball'
const LOADER_URL = `${UNITY_BUILD_PATH}/${BUILD_PREFIX}.loader.js?v=${UNITY_BUILD_VERSION}`


export default function GameCanvas({ onBack }) {
  const [loadingProgress, setLoadingProgress] = useState(0)
  const [isLoaded, setIsLoaded] = useState(false)
  const [hasUnityBuild, setHasUnityBuild] = useState(null) // null = checking, true/false = result
  const [error, setError] = useState(null)

  useEffect(() => {
    // Check if Unity build exists
    fetch(LOADER_URL, { method: 'HEAD' })
      .then((res) => {
        if (res.ok) {
          setHasUnityBuild(true)
          loadUnity()
        } else {
          setHasUnityBuild(false)
        }
      })
      .catch(() => {
        setHasUnityBuild(false)
      })
  }, [])

  const loadUnity = useCallback(() => {
    // Dynamically load Unity loader
    const script = document.createElement('script')
    script.src = LOADER_URL
    script.onload = () => {
      if (typeof window.createUnityInstance === 'function') {
        const canvas = document.getElementById('unity-canvas')
        window.createUnityInstance(canvas, {
          dataUrl: `${UNITY_BUILD_PATH}/${BUILD_PREFIX}.data?v=${UNITY_BUILD_VERSION}`,
          frameworkUrl: `${UNITY_BUILD_PATH}/${BUILD_PREFIX}.framework.js?v=${UNITY_BUILD_VERSION}`,
          codeUrl: `${UNITY_BUILD_PATH}/${BUILD_PREFIX}.wasm?v=${UNITY_BUILD_VERSION}`,
          streamingAssetsUrl: '/unity-build/StreamingAssets',
          companyName: 'ROLLD',
          productName: 'ROLLD',
          productVersion: '0.1',
        }, (progress) => {
          setLoadingProgress(Math.round(progress * 100))
        }).then((instance) => {
          setIsLoaded(true)
          window.__unityInstance = instance

          // Patch requestPointerLock to catch SecurityError (user exits lock before promise resolves)
          const canvas = document.getElementById('unity-canvas')
          if (canvas) {
            const origLock = canvas.requestPointerLock.bind(canvas)
            canvas.requestPointerLock = function (...args) {
              try {
                const result = origLock(...args)
                if (result && typeof result.catch === 'function') {
                  return result.catch((e) => {
                    if (e.name === 'SecurityError') {
                      console.warn('[ROLLD] Pointer lock interrupted (SecurityError) — ignored')
                    } else {
                      throw e
                    }
                  })
                }
                return result
              } catch (e) {
                if (e.name === 'SecurityError') {
                  console.warn('[ROLLD] Pointer lock sync error — ignored')
                } else {
                  throw e
                }
              }
            }
          }

          console.log('[ROLLD] Unity loaded')
        }).catch((err) => {
          setError(err.message)
        })
      }
    }
    script.onerror = () => setError('Failed to load Unity loader')
    document.body.appendChild(script)
  }, [])

  return (
    <div className="fixed inset-0 bg-rolld-bg z-50 flex flex-col">
      {/* Top bar */}
      <div className="flex items-center justify-between px-4 py-2 bg-rolld-surface/80 backdrop-blur border-b border-rolld-border">
        <button
          onClick={onBack}
          className="flex items-center gap-2 text-rolld-muted hover:text-rolld-text transition-colors text-sm"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          Retour
        </button>
        <span className="text-rolld-accent font-bold tracking-wide text-sm">ROLL'D</span>
        <div className="w-16" /> {/* Spacer */}
      </div>

      {/* Game area */}
      <div className="flex-1 relative flex items-center justify-center">
        {/* Unity Canvas (hidden until build exists) */}
        <canvas
          id="unity-canvas"
          className={`w-full h-full ${hasUnityBuild && isLoaded ? 'block' : 'hidden'}`}
          tabIndex={-1}
        />

        {/* Loading state */}
        {hasUnityBuild === true && !isLoaded && !error && (
          <div className="absolute inset-0 flex flex-col items-center justify-center gap-6">
            <div className="text-4xl font-black">
              <span className="text-rolld-text">ROLL</span>
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-rolld-accent to-rolld-violet">'D</span>
            </div>
            <div className="w-64">
              <div className="h-2 rounded-full bg-rolld-surface overflow-hidden">
                <div
                  className="h-full rounded-full bg-gradient-to-r from-rolld-accent to-rolld-violet transition-all duration-300"
                  style={{ width: `${loadingProgress}%` }}
                />
              </div>
              <p className="text-rolld-muted text-sm text-center mt-2">Chargement… {loadingProgress}%</p>
            </div>
          </div>
        )}

        {/* No build placeholder */}
        {hasUnityBuild === false && (
          <div className="flex flex-col items-center justify-center gap-6 text-center px-4">
            <div className="w-24 h-24 rounded-3xl bg-rolld-surface border border-rolld-border flex items-center justify-center text-5xl animate-float">
              🎮
            </div>
            <div>
              <h2 className="text-2xl font-bold text-rolld-text mb-2">Build Unity en attente</h2>
              <p className="text-rolld-muted max-w-md leading-relaxed">
                Le build WebGL n'est pas encore disponible. Placez les fichiers dans{' '}
                <code className="px-2 py-0.5 rounded bg-rolld-surface border border-rolld-border text-rolld-accent-light text-sm font-mono">
                  public/unity-build/Build/
                </code>
              </p>
            </div>
            <div className="glass rounded-xl p-4 text-left text-sm text-rolld-muted font-mono max-w-sm w-full">
              <p className="text-rolld-accent-light mb-2">Fichiers requis :</p>
              <p>├── build_mai.loader.js</p>
              <p>├── build_mai.data</p>
              <p>├── build_mai.framework.js</p>
              <p>└── build_mai.wasm</p>
            </div>
          </div>
        )}

        {/* Checking state */}
        {hasUnityBuild === null && (
          <div className="flex flex-col items-center gap-4">
            <div className="w-8 h-8 border-2 border-rolld-accent border-t-transparent rounded-full animate-spin" />
            <p className="text-rolld-muted text-sm">Vérification du build…</p>
          </div>
        )}

        {/* Error state */}
        {error && (
          <div className="flex flex-col items-center gap-4 text-center px-4">
            <div className="text-5xl">⚠️</div>
            <h2 className="text-xl font-bold text-red-400">Erreur de chargement</h2>
            <p className="text-rolld-muted max-w-md text-sm">{error}</p>
            <button
              onClick={onBack}
              className="px-4 py-2 rounded-lg bg-rolld-surface border border-rolld-border text-rolld-text hover:border-rolld-accent/40 transition-colors text-sm"
            >
              Retour à l'accueil
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
