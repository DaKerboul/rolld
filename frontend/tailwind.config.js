/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        'rolld': {
          bg: '#0a0a0f',
          surface: '#12121a',
          border: '#1e1e2e',
          accent: '#6c5ce7',
          'accent-light': '#a29bfe',
          orange: '#f39c12',
          violet: '#9b59b6',
          blue: '#3498db',
          text: '#e8e8f0',
          muted: '#6b7280',
        }
      },
      fontFamily: {
        display: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
      },
      animation: {
        'float': 'float 6s ease-in-out infinite',
        'pulse-glow': 'pulseGlow 2s ease-in-out infinite',
        'slide-up': 'slideUp 0.8s ease-out',
      },
      keyframes: {
        float: {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-20px)' },
        },
        pulseGlow: {
          '0%, 100%': { boxShadow: '0 0 20px rgba(108, 92, 231, 0.3)' },
          '50%': { boxShadow: '0 0 40px rgba(108, 92, 231, 0.6)' },
        },
        slideUp: {
          '0%': { opacity: 0, transform: 'translateY(30px)' },
          '100%': { opacity: 1, transform: 'translateY(0)' },
        },
      },
    },
  },
  plugins: [],
}
