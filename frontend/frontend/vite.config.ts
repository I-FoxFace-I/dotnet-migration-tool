import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      // Proxy gRPC-Web requests to backend
      '/migration.MigrationService': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})
