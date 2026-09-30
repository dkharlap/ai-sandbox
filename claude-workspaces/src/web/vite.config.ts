import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The gateway proxies to this dev server, so it only needs to listen on the port Aspire
// assigns and accept the gateway's forwarded requests.
export default defineConfig({
  plugins: [react()],
  server: {
    port: Number(process.env.PORT) || 5173,
    strictPort: true,
    host: true,
  },
})
