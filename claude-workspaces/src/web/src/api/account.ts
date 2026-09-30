import { request } from './client'
import type { AuthConfig, Session, User } from '../types'

/** A 401 here is the normal signed-out answer, so it must not navigate. */
export const fetchSession = () =>
  request<Session>('/bff/me', { redirectOn401: false })

/** Anonymous: tells the signed-out view whether dev sign-in is standing in for Google. */
export const fetchAuthConfig = () =>
  request<AuthConfig>('/bff/config', { redirectOn401: false })

export const fetchUser = () => request<User>('/v1/users/me')

export const logout = () => request<void>('/bff/logout', { method: 'POST' })
