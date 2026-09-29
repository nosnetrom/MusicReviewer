import process from 'node:process'
import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue(), vueDevTools()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    // Forward API calls to the ASP.NET Core "http" launch profile during local development.
    // Set API_PROXY_TARGET to point at an API running elsewhere.
    proxy: {
      '/api': process.env.API_PROXY_TARGET ?? 'http://localhost:5080',
    },
  },
})
