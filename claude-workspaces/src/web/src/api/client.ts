export class UnauthorizedError extends Error {
  constructor() {
    super('Not authenticated')
  }
}

let redirecting = false

/**
 * Sends the viewer to Google sign-in. Single-flight: several concurrent 401s during app
 * boot must not each trigger a navigation.
 */
export function redirectToLogin(): void {
  if (redirecting) return
  redirecting = true
  const returnUrl = encodeURIComponent(window.location.pathname + window.location.search)
  window.location.assign(`/bff/login?returnUrl=${returnUrl}`)
}

type RequestOptions = {
  method?: string
  body?: unknown
  /**
   * When false, a 401 throws UnauthorizedError instead of navigating. Used by the session
   * probe, where a 401 is the expected signed-out answer rather than an error.
   */
  redirectOn401?: boolean
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, redirectOn401 = true } = options

  const response = await fetch(path, {
    method,
    credentials: 'same-origin',
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (response.status === 401) {
    if (redirectOn401) redirectToLogin()
    throw new UnauthorizedError()
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new Error(problem?.detail ?? problem?.title ?? `Request failed (${response.status})`)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}
