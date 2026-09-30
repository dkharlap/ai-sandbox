import { useQuery } from '@tanstack/react-query'
import { fetchAuthConfig } from './api/account'
import { AccountCard } from './features/account/AccountCard'
import { PreferenceList } from './features/preferences/PreferenceList'
import { useSession } from './auth/SessionProvider'
import { ErrorBanner, Spinner } from './components/Ui'

export function App() {
  const state = useSession()
  const { data: authConfig } = useQuery({
    queryKey: ['auth-config'],
    queryFn: fetchAuthConfig,
    staleTime: Infinity,
  })

  return (
    <main>
      <header>
        <h1>Model Preferences</h1>
      </header>

      {state.status === 'loading' && <Spinner label="Checking your session…" />}

      {state.status === 'error' && <ErrorBanner message={state.message} />}

      {state.status === 'signedOut' && (
        <section className="card signed-out">
          <p>Sign in to manage your preferred coding models.</p>
          {/* A plain link, never fetch(): starting sign-in is a browser navigation. */}
          <a className="button" href="/bff/login">
            {authConfig?.devMode ? 'Sign in (dev mode)' : 'Sign in with Google'}
          </a>
          {authConfig?.devMode && (
            <p className="muted">
              No Google credentials configured — you'll pick from a list of demo users.
            </p>
          )}
        </section>
      )}

      {state.status === 'signedIn' && (
        <>
          <AccountCard />
          <PreferenceList />
        </>
      )}
    </main>
  )
}
