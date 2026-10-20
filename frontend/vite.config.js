import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// O front chama "/api/..." e o Vite repassa para a API .NET.
// Usa 127.0.0.1 (e não "localhost") porque em alguns PCs com Windows o "localhost" não conecta.
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://127.0.0.1:5224',
    },
  },
})
