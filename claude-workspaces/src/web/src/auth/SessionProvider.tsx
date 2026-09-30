import { createContext, use, useEffect, useState, type ReactNode } from 'react'
import { fetchSession } from '../api/account'
import { UnauthorizedError } from '../api/client'
import type { Session } from '../types'

type SessionState =
  | { status: 'loading' }
  | { status: 'signedOut' }
  | { status: 'signedIn'; session: Session }
  | { status: 'error'; message: string }

const SessionContext = createContext<SessionState>({ status: 'loading' })

export function SessionProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<SessionState>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false

    // Probe once on boot. A 401 means signed out — it must render the signed-out shell
    // rather than navigating, or the app would loop through the login redirect forever.
    fetchSession()
      .then((session) => {
        if (!cancelled) setState({ status: 'signedIn', session })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        setState(
          error instanceof UnauthorizedError
            ? { status: 'signedOut' }
            : { status: 'error', message: error instanceof Error ? error.message : 'Unknown error' },
        )
      })

    return () => {
      cancelled = true
    }
  }, [])

  return <SessionContext value={state}>{children}</SessionContext>
}

export const useSession = () => use(SessionContext)
