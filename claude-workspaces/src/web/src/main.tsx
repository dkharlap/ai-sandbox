import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { App } from './App'
import { SessionProvider } from './auth/SessionProvider'
import { UnauthorizedError } from './api/client'
import './styles.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // client.ts already navigates to login on a 401; retrying would just race it.
      retry: (failureCount, error) => !(error instanceof UnauthorizedError) && failureCount < 2,
      refetchOnWindowFocus: false,
    },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <SessionProvider>
        <App />
      </SessionProvider>
    </QueryClientProvider>
  </StrictMode>,
)
