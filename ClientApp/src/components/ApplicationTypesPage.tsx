import { useEffect, useMemo, useState } from 'react'
import {
  getApplicationTypes,
  updateApplicationType,
} from '../services/applicationTypeService'
import type { ApplicationType } from '../services/applicationTypeService'
import PageHero from './common/PageHero'
import SearchInput from './common/SearchInput'
import ErrorAlert from './common/ErrorAlert'
import EmptyState from './common/EmptyState'
import ApplicationTypeRow from './applicationTypes/ApplicationTypeRow'
import {
  RefreshIcon,
  ApplicationsIcon,
  FilterIcon,
} from './common/Icons'

type ApplicationTypesPageProps = {
  accessToken: string
}

type EditForm = {
  applicationTypeTitle: string
  applicationTypeFees: string
}

type SearchBy = 'all' | 'title' | 'id'

export default function ApplicationTypesPage({
  accessToken,
}: ApplicationTypesPageProps) {
  const [items, setItems] = useState<ApplicationType[]>([])
  const [search, setSearch] = useState('')
  const [searchBy, setSearchBy] = useState<SearchBy>('all')
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

  function handleResetFilters() {
    setSearch('')
    setSearchBy('all')
  }

  const isFiltered = Boolean(search.trim() || searchBy !== 'all')

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase()

    if (!query) return items

    return items.filter((item) => {
      if (searchBy === 'title') {
        return item.applicationTypeTitle.toLowerCase().includes(query)
      }
      if (searchBy === 'id') {
        return String(item.applicationTypeId).includes(query)
      }

      // 'all'
      return (
        item.applicationTypeTitle.toLowerCase().includes(query) ||
        String(item.applicationTypeId).includes(query)
      )
    })
  }, [items, search, searchBy])

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case 'title':
        return 'Search by application type name...'
      case 'id':
        return 'Search by application type ID...'
      default:
        return 'Search by type name or ID...'
    }
  }, [searchBy])

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
    if (editingId === null || saving) return

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
      <PageHero pageId="Application Types" />

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>Application Types Directory</h2>
            <p>Application types registered in DVLD</p>
          </div>

          <span className="record-count">
            {loading
              ? 'Loading...'
              : filteredItems.length === items.length
                ? `${items.length} Types`
                : `${filteredItems.length} of ${items.length} Types`}
          </span>
        </div>

        <div className="table-toolbar">
          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 150 }}
            value={searchBy}
            onChange={(event) => setSearchBy(event.target.value as SearchBy)}
            aria-label="Filter criterion"
          >
            <option value="all">All Fields</option>
            <option value="title">Type Name</option>
            <option value="id">Type ID</option>
          </select>

          <SearchInput
            value={search}
            onChange={setSearch}
            placeholder={placeholderText}
            ariaLabel="Search application types"
          />

          {isFiltered && (
            <button
              type="button"
              className="secondary-button"
              onClick={handleResetFilters}
              title="Reset all filters"
            >
              <FilterIcon size={14} />
              <span>Reset</span>
            </button>
          )}

          <button
            type="button"
            className="secondary-button"
            disabled={loading}
            onClick={() => {
              setSuccess('')
              void loadItems()
            }}
          >
            <RefreshIcon size={14} />
            <span>{loading ? 'Loading...' : 'Refresh'}</span>
          </button>
        </div>

        {error && (
          <div style={{ margin: '0 25px 20px' }}>
            <ErrorAlert
              message={error}
              onRetry={() => {
                setSuccess('')
                void loadItems()
              }}
            />
          </div>
        )}

        {success && (
          <p role="status" style={{ padding: '0 25px 20px', margin: 0, color: '#059669', fontWeight: 600, fontSize: 13 }}>
            {success}
          </p>
        )}

        {loading ? (
          <p className="empty-state">Loading application types...</p>
        ) : (
          <div className="table-container">
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
                {filteredItems.map((item) => (
                  <ApplicationTypeRow
                    key={item.applicationTypeId}
                    item={item}
                    isEditing={editingId === item.applicationTypeId}
                    editForm={editForm}
                    saving={saving}
                    onStartEdit={startEditing}
                    onFormChange={setEditForm}
                    onSave={() => void handleSave()}
                    onCancel={() => {
                      setEditingId(null)
                      setError('')
                    }}
                  />
                ))}

                {filteredItems.length === 0 && (
                  <tr>
                    <td colSpan={4}>
                      <EmptyState
                        icon={<ApplicationsIcon size={36} />}
                        message={
                          items.length === 0
                            ? 'No application types found.'
                            : 'No application types match your search.'
                        }
                      />
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
