// Environment configuration
// Set VITE_ENV=dev in Coolify build args for the dev application
// Defaults to 'prod' if not set

export const ENV = import.meta.env.VITE_ENV || 'prod'
export const IS_DEV = ENV === 'dev'
export const IS_PROD = ENV === 'prod'

// Theme overrides per environment
export const envTheme = {
  dev: {
    accent: '#f39c12',        // Orange
    accentLight: '#f1c40f',   // Yellow-orange
    accentRgb: '243, 156, 18',
    gradientFrom: '#f39c12',
    gradientTo: '#e67e22',
    label: 'DEV',
    badge: '🚧',
  },
  prod: {
    accent: '#6c5ce7',        // Purple (default)
    accentLight: '#a29bfe',
    accentRgb: '108, 92, 231',
    gradientFrom: '#6c5ce7',
    gradientTo: '#9b59b6',
    label: 'PROD',
    badge: '🚀',
  },
}

export const theme = envTheme[ENV] || envTheme.prod
