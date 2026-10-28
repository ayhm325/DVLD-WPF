
import { useEffect, useMemo, useState } from 'react'
import {
  getApplicationTypes,
  updateApplicationType,
} from '../services/applicationTypeService'
import type { ApplicationType } from '../services/applicationTypeService'

type ApplicationTypesPageProps = {
  accessToken: string
}

type EditForm = {
  applicationTypeTitle: string
  applicationTypeFees: string
}

export default function ApplicationTypesPage({
  accessToken,
}: ApplicationTypesPageProps) {
  const [items, setItems] = useState<ApplicationType[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editForm, setEditForm] = useState<EditForm>({
    applicationTypeTitle: '',
    applicationTypeFees: '',
  })
  const [saving, setSaving] = useState(false)

  async function loadItems() {
    setLoading(true)
    setError('')

    try {
      const result = await getApplicationTypes(accessToken)
      setItems(result)
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Failed to load application types.',
      )
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')

      try {
        const result = await getApplicationTypes(accessToken)

        if (!cancelled) {
          setItems(result)
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Failed to load application types.',
          )
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void load()

    return () => {
      cancelled = true
    }
  }, [accessToken])

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase()

    if (!query) return items

    return items.filter(
      (item) =>
        item.applicationTypeTitle.toLowerCase().includes(query) ||
        String(item.applicationTypeId).includes(query),
    )
  }, [items, search])

  function startEditing(item: ApplicationType) {
    setEditingId(item.applicationTypeId)
    setEditForm({
      applicationTypeTitle: item.applicationTypeTitle,
      applicationTypeFees: String(item.applicationTypeFees),
    })
    setError('')
    setSuccess('')
  }

  async function handleSave() {
    if (editingId === null) return

    const title = editForm.applicationTypeTitle.trim()
    const fees = Number(editForm.applicationTypeFees)

    if (!title) {
      setError('Application type name is required.')
      return
    }

    if (
      editForm.applicationTypeFees.trim() === '' ||
      !Number.isFinite(fees) ||
      fees < 0
    ) {
      setError('Enter a valid fee greater than or equal to zero.')
      return
    }

    setSaving(true)
    setError('')
    setSuccess('')

    try {
      await updateApplicationType(accessToken, editingId, {
        applicationTypeTitle: title,
        applicationTypeFees: fees,
      })

      await loadItems()
      setEditingId(null)
      setSuccess('Application type updated successfully.')
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Failed to update application type.',
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="page-content">
      <div>
        <p className="eyebrow">SYSTEM SETTINGS</p>
        <h2>Application Types</h2>
        <p>View and manage application type names and fees.</p>
      </div>

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>Application Types Directory</h2>
            <p>Application types registered in DVLD</p>
          </div>

          <span className="record-count">
            {loading ? 'Loading...' : `${items.length} Types`}
          </span>
        </div>

        <div className="table-toolbar">
          <input
            type="search"
            className="search-input"
            placeholder="Search by type name or ID..."
            aria-label="Search application types"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />

          <button
            type="button"
            className="secondary-button"
            disabled={loading}
            onClick={() => {
              setSuccess('')
              void loadItems()
            }}
          >
            Refresh
          </button>
        </div>

        {error && (
          <p className="login-error" role="alert">
            {error}
          </p>
        )}

        {success && (
          <p role="status" style={{ padding: '0 25px', color: '#21865a' }}>
            {success}
          </p>
        )}

        {loading ? (
          <p className="empty-state">Loading application types...</p>
        ) : (
          <div className="table-wrapper">
            <table className="people-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Application Type</th>
                  <th>Fees</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {filteredItems.map((item) => {
                  const isEditing = editingId === item.applicationTypeId

                  return (
                    <tr key={item.applicationTypeId}>
                      <td>{item.applicationTypeId}</td>

                      <td>
                        {isEditing ? (
                          <input
                            className="search-input"
                            value={editForm.applicationTypeTitle}
                            maxLength={100}
                            aria-label="Application type name"
                            onChange={(event) =>
                              setEditForm((previous) => ({
                                ...previous,
                                applicationTypeTitle: event.target.value,
                              }))
                            }
                          />
                        ) : (
                          item.applicationTypeTitle
                        )}
                      </td>

                      <td>
                        {isEditing ? (
                          <input
                            className="search-input"
                            type="number"
                            min="0"
                            step="0.01"
                            value={editForm.applicationTypeFees}
                            aria-label="Application type fees"
                            onChange={(event) =>
                              setEditForm((previous) => ({
                                ...previous,
                                applicationTypeFees: event.target.value,
                              }))
                            }
                          />
                        ) : (
                          item.applicationTypeFees.toFixed(2)
                        )}
                      </td>

                      <td>
                        {isEditing ? (
                          <div style={{ display: 'flex', gap: 8 }}>
                            <button
                              type="button"
                              className="primary-button"
                              disabled={saving}
                              onClick={() => void handleSave()}
                            >
                              {saving ? 'Saving...' : 'Save'}
                            </button>

                            <button
                              type="button"
                              className="secondary-button"
                              disabled={saving}
                              onClick={() => {
                                setEditingId(null)
                                setError('')
                              }}
                            >
                              Cancel
                            </button>
                          </div>
                        ) : (
                          <button
                            type="button"
                            className="secondary-button"
                            onClick={() => startEditing(item)}
                          >
                            Edit
                          </button>
                        )}
                      </td>
                    </tr>
                  )
                })}

                {filteredItems.length === 0 && (
                  <tr>
                    <td colSpan={4} className="empty-state">
                      {items.length === 0
                        ? 'No application types found.'
                        : 'No application types match your search.'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {!loading && !error && (
          <div className="panel-footer">
            Showing {filteredItems.length} of {items.length} application types
          </div>
        )}
      </section>
    </section>
  )
}
