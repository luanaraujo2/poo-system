import { definePreset } from '@primeuix/themes'
import Aura from '@primeuix/themes/aura'

// Tema do PrimeVue com o azul do protótipo da Sprint 2 (#2457A6).
export const TemaSgt = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#eef4fb',
      100: '#d5e3f4',
      200: '#adc7ea',
      300: '#7fa6dc',
      400: '#4f82cb',
      500: '#2457a6',
      600: '#1f4b8f',
      700: '#1a3f78',
      800: '#153262',
      900: '#10264b',
      950: '#0a1830',
    },
  },
})
