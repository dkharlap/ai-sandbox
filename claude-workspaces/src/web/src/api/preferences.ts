import { request } from './client'
import type { Model, Preference } from '../types'

export const fetchCatalog = () => request<Model[]>('/v1/catalog')

export const fetchPreferences = () => request<Preference[]>('/v1/preferences/me')

export const replacePreferences = (modelIds: string[]) =>
  request<Preference[]>('/v1/preferences/me', { method: 'PUT', body: { modelIds } })

export const addPreference = (modelId: string) =>
  request<Preference[]>('/v1/preferences/me/items', { method: 'POST', body: { modelId } })

export const removePreference = (modelId: string) =>
  request<Preference[]>(`/v1/preferences/me/items/${encodeURIComponent(modelId)}`, {
    method: 'DELETE',
  })
