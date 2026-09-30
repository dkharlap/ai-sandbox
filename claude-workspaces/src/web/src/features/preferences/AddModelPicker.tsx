import { useState } from 'react'
import { ErrorBanner, Spinner } from '../../components/Ui'
import { useAddPreference, useCatalog } from './usePreferences'

export function AddModelPicker({ chosen }: { chosen: string[] }) {
  const { data: catalog, isPending, error } = useCatalog()
  const add = useAddPreference()
  const [selected, setSelected] = useState('')

  if (isPending) return <Spinner label="Loading catalog…" />
  if (error) return <ErrorBanner message={error.message} />

  const available = (catalog ?? []).filter((model) => !chosen.includes(model.id))
  if (available.length === 0) {
    return <p className="muted">Every available model is already on your list.</p>
  }

  return (
    <form
      className="add-model"
      onSubmit={(event) => {
        event.preventDefault()
        if (!selected) return
        add.mutate(selected)
        setSelected('')
      }}
    >
      <label htmlFor="model-picker">Add a model</label>
      <select id="model-picker" value={selected} onChange={(e) => setSelected(e.target.value)}>
        <option value="">Choose…</option>
        {available.map((model) => (
          <option key={model.id} value={model.id}>
            {model.displayName} — {model.vendor}
          </option>
        ))}
      </select>
      <button type="submit" disabled={!selected || add.isPending}>
        Add
      </button>
      {add.isError && <ErrorBanner message={add.error.message} />}
    </form>
  )
}
