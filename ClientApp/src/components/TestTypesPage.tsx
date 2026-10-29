
import { useEffect, useMemo, useState } from 'react'
import {
  getTestTypes,
  updateTestType,
} from '../services/testTypeService'
import type { TestType } from '../services/testTypeService'

type Props = {
  accessToken: string
}

type EditForm = {
  testTypeTitle: string
  testTypeDescription: string
  testTypeFees: string
}

export default function TestTypesPage({ accessToken }: Props) {
  const [items, setItems] = useState<TestType[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [saving, setSaving] = useState(false)
  const [refreshKey, setRefreshKey] = useState(0)
  const [form, setForm] = useState<EditForm>({
    testTypeTitle: '',
    testTypeDescription: '',
    testTypeFees: '',
  })

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')

      try {
        const result = await getTestTypes(accessToken)

        if (!cancelled) setItems(result)
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Failed to load test types.',
          )
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    void load()

    return () => {
      cancelled = true
    }
  }, [accessToken, refreshKey])

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase()

    if (!query) return items

    return items.filter(
      (item) =>
        item.testTypeTitle.toLowerCase().includes(query) ||
        item.testTypeDescription.toLowerCase().includes(query) ||
        String(item.testTypeId).includes(query),
    )
  }, [items, search])

  function startEditing(item: TestType) {
    setEditingId(item.testTypeId)
    setForm({
      testTypeTitle: item.testTypeTitle,
      testTypeDescription: item.testTypeDescription,
      testTypeFees: String(item.testTypeFees),
    })
    setError('')
    setSuccess('')
  }

  async function saveChanges() {
    if (editingId === null || saving) return

    const title = form.testTypeTitle.trim()
    const description = form.testTypeDescription.trim()
    const fees = Number(form.testTypeFees)

    if (!title) {
      setError('Test type title is required.')
      return
    }

    if (
      form.testTypeFees.trim() === '' ||
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
      await updateTestType(accessToken, editingId, {
        testTypeTitle: title,
        testTypeDescription: description,
        testTypeFees: fees,
      })

      setEditingId(null)
      setSuccess('Test type updated successfully.')
      setRefreshKey((key) => key + 1)
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Failed to update test type.',
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="page-content">
      <div>
        <p className="eyebrow">SYSTEM SETTINGS</p>
        <h2>Test Types</h2>
        <p>Manage test names, descriptions, and fees.</p>
      </div>

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>Test Types Directory</h2>
            <p>Tests configured in DVLD</p>
          </div>

          <span className="record-count">
            {loading ? 'Loading...' : `${items.length} Test Types`}
          </span>
        </div>

        <div className="table-toolbar">
          <input
            type="search"
            className="search-input"
            placeholder="Search by title, description, or ID..."
            aria-label="Search test types"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />

          <button
            type="button"
            className="secondary-button"
            disabled={loading}
            onClick={() => {
              setSuccess('')
              setRefreshKey((key) => key + 1)
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
          <p className="empty-state">Loading test types...</p>
        ) : (
          <div className="table-wrapper">
            <table className="people-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Test Type</th>
                  <th>Description</th>
                  <th>Fees</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {filteredItems.map((item) => {
                  const isEditing = editingId === item.testTypeId

                  return (
                    <tr key={item.testTypeId}>
                      <td>{item.testTypeId}</td>

                      <td>
                        {isEditing ? (
                          <input
                            className="search-input"
                            value={form.testTypeTitle}
                            maxLength={100}
                            aria-label="Test type title"
                            onChange={(event) =>
                              setForm((previous) => ({
                                ...previous,
                                testTypeTitle: event.target.value,
                              }))
                            }
                          />
                        ) : (
                          item.testTypeTitle
                        )}
                      </td>

                      <td>
                        {isEditing ? (
                          <input
                            className="search-input"
                            value={form.testTypeDescription}
                            maxLength={500}
                            aria-label="Test type description"
                            onChange={(event) =>
                              setForm((previous) => ({
                                ...previous,
                                testTypeDescription: event.target.value,
                              }))
                            }
                          />
                        ) : (
                          item.testTypeDescription
                        )}
                      </td>

                      <td>
                        {isEditing ? (
                          <input
                            className="search-input"
                            type="number"
                            min="0"
                            step="0.01"
                            value={form.testTypeFees}
                            aria-label="Test type fees"
                            onChange={(event) =>
                              setForm((previous) => ({
                                ...previous,
                                testTypeFees: event.target.value,
                              }))
                            }
                          />
                        ) : (
                          item.testTypeFees.toFixed(2)
                        )}
                      </td>

                      <td>
                        {isEditing ? (
                          <div style={{ display: 'flex', gap: 8 }}>
                            <button
                              type="button"
                              className="primary-button"
                              disabled={saving}
                              onClick={() => void saveChanges()}
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
                    <td colSpan={5} className="empty-state">
                      {items.length === 0
                        ? 'No test types found.'
                        : 'No test types match your search.'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {!loading && !error && (
          <div className="panel-footer">
            Showing {filteredItems.length} of {items.length} test types
          </div>
        )}
      </section>
    </section>
  )
}
