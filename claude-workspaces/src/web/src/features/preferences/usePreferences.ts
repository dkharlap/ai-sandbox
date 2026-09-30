import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  addPreference,
  fetchCatalog,
  fetchPreferences,
  removePreference,
  replacePreferences,
} from '../../api/preferences'
import type { Preference } from '../../types'

const PREFERENCES_KEY = ['preferences']
const CATALOG_KEY = ['catalog']

export function useCatalog() {
  return useQuery({
    queryKey: CATALOG_KEY,
    queryFn: fetchCatalog,
    // The catalog is effectively static; no reason to refetch it during a session.
    staleTime: Infinity,
  })
}

export function usePreferences() {
  return useQuery({ queryKey: PREFERENCES_KEY, queryFn: fetchPreferences })
}

/**
 * Reorder writes the whole list. Optimistic so drag-and-drop feels immediate, with a
 * rollback to the previous server snapshot if the write fails.
 */
export function useReorder() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (modelIds: string[]) => replacePreferences(modelIds),
    onMutate: async (modelIds) => {
      await queryClient.cancelQueries({ queryKey: PREFERENCES_KEY })
      const previous = queryClient.getQueryData<Preference[]>(PREFERENCES_KEY)

      if (previous) {
        const byId = new Map(previous.map((p) => [p.modelId, p]))
        queryClient.setQueryData<Preference[]>(
          PREFERENCES_KEY,
          modelIds.flatMap((id, rank) => {
            const existing = byId.get(id)
            return existing ? [{ ...existing, rank }] : []
          }),
        )
      }

      return { previous }
    },
    onError: (_error, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(PREFERENCES_KEY, context.previous)
      }
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: PREFERENCES_KEY }),
  })
}

export function useAddPreference() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (modelId: string) => addPreference(modelId),
    onSuccess: (data) => queryClient.setQueryData(PREFERENCES_KEY, data),
  })
}

export function useRemovePreference() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (modelId: string) => removePreference(modelId),
    onSuccess: (data) => queryClient.setQueryData(PREFERENCES_KEY, data),
  })
}
