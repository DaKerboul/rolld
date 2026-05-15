# ROLL'D — Frontend

Client web pour le jeu ROLL'D. Héberge le build Unity WebGL dans une interface moderne.

## Stack

- **React 18** + **Vite 5** — build rapide, HMR
- **Tailwind CSS 3** — styling utility-first
- **Unity WebGL Loader** — intégration du build Unity

## Quickstart

```bash
npm install
npm run dev      # http://localhost:5173
npm run build    # production build → dist/
```

## Unity WebGL Build

Placer le build Unity dans `public/unity-build/` :
```
public/unity-build/
├── Build/
│   ├── build.data.gz
│   ├── build.framework.js.gz
│   ├── build.loader.js
│   └── build.wasm.gz
└── TemplateData/  (optionnel)
```

## Docker

```bash
docker build -t rolld-frontend .
docker run -p 80:80 rolld-frontend
```

## Structure

```
├── public/
│   ├── unity-build/     # Build WebGL (non versionné)
│   └── favicon.svg
├── src/
│   ├── components/      # Composants React
│   ├── assets/          # Images, fonts
│   ├── App.jsx
│   └── main.jsx
├── index.html
├── tailwind.config.js
├── vite.config.js
└── Dockerfile
```
