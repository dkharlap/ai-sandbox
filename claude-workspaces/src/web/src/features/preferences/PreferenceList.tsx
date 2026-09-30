import { useMemo } from 'react'
import { Card, ErrorBanner, Spinner } from '../../components/Ui'
import { AddModelPicker } from './AddModelPicker'
import { usePreferences, useRemovePreference, useReorder } from './usePreferences'

export function PreferenceList() {
  const { data: preferences, isPending, error } = usePreferences()
  const reorder = useReorder()
  const remove = useRemovePreference()

  const ids = useMemo(() => preferences?.map((p) => p.modelId) ?? [], [preferences])

  function move(from: number, to: number) {
    if (to < 0 || to >= ids.length) return
    const next = [...ids]
    const [moved] = next.splice(from, 1)
    next.splice(to, 0, moved)
    reorder.mutate(next)
  }

  return (
    <Card title="Preferred coding models">
      {isPending && <Spinner />}
      {error && <ErrorBanner message={error.message} />}

      {preferences && preferences.length === 0 && (
        <p className="muted">No models chosen yet. Add one below.</p>
      )}

      {preferences && preferences.length > 0 && (
        <ol className="prefs">
          {preferences.map((preference, index) => (
            <li key={preference.modelId}>
              <span className="rank">{index + 1}</span>
              <span className="name">
                {preference.displayName}
                <small>{preference.vendor}</small>
              </span>
              <span className="actions">
                <button onClick={() => move(index, index - 1)} disabled={index === 0} aria-label={`Move ${preference.displayName} up`}>
                  ↑
                </button>
                <button
                  onClick={() => move(index, index + 1)}
                  disabled={index === preferences.length - 1}
                  aria-label={`Move ${preference.displayName} down`}
                >
                  ↓
                </button>
                <button onClick={() => remove.mutate(preference.modelId)} aria-label={`Remove ${preference.displayName}`}>
                  Remove
                </button>
              </span>
            </li>
          ))}
        </ol>
      )}

      {reorder.isError && <ErrorBanner message={reorder.error.message} />}
      {remove.isError && <ErrorBanner message={remove.error.message} />}

      <AddModelPicker chosen={ids} />
    </Card>
  )
}
