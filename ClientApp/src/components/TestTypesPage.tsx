import { useEffect, useMemo, useState } from 'react'
import {
  getTestTypes,
  updateTestType,
} from '../services/testTypeService'
import type { TestType } from '../services/testTypeService'
import PageHero from './common/PageHero'
import SearchInput from './common/SearchInput'
import ErrorAlert from './common/ErrorAlert'
import EmptyState from './common/EmptyState'
import TestTypeRow from './testTypes/TestTypeRow'
import {
  RefreshIcon,
  TestTypeIcon,
  FilterIcon,
} from './common/Icons'

type Props = {
  accessToken: string
}

type EditForm = {
  testTypeTitle: string
  testTypeDescription: string
  testTypeFees: string
}

type SearchBy = 'all' | 'title' | 'id' | 'desc'

export default function TestTypesPage({ accessToken }: Props) {
  const [items, setItems] = useState<TestType[]>([])
  const [search, setSearch] = useState('')
  const [searchBy, setSearchBy] = useState<SearchBy>('all')
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
        return item.testTypeTitle.toLowerCase().includes(query)
      }
      if (searchBy === 'id') {
        return String(item.testTypeId).includes(query)
      }
      if (searchBy === 'desc') {
        return item.testTypeDescription.toLowerCase().includes(query)
      }

      // 'all'
      return (
        item.testTypeTitle.toLowerCase().includes(query) ||
        item.testTypeDescription.toLowerCase().includes(query) ||
        String(item.testTypeId).includes(query)
      )
    })
  }, [items, search, searchBy])

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case 'title':
        return 'Search by test type title...'
      case 'id':
        return 'Search by test type ID...'
      case 'desc':
        return 'Search by description...'
      default:
        return 'Search by title, description, or ID...'
    }
  }, [searchBy])

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
      <PageHero pageId="Test Types" />

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>Test Types Directory</h2>
            <p>Tests configured in DVLD</p>
          </div>

          <span className="record-count">
            {loading
              ? 'Loading...'
              : filteredItems.length === items.length
                ? `${items.length} Test Types`
                : `${filteredItems.length} of ${items.length} Test Types`}
          </span>
        </div>

        <div className="table-toolbar">
          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 140 }}
            value={searchBy}
            onChange={(event) => setSearchBy(event.target.value as SearchBy)}
            aria-label="Filter criterion"
          >
            <option value="all">All Fields</option>
            <option value="title">Test Title</option>
            <option value="id">Test ID</option>
            <option value="desc">Description</option>
          </select>

          <SearchInput
            value={search}
            onChange={setSearch}
            placeholder={placeholderText}
            ariaLabel="Search test types"
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
              setRefreshKey((key) => key + 1)
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
                setRefreshKey((key) => key + 1)
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
          <p className="empty-state">Loading test types...</p>
        ) : (
          <div className="table-container">
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
                {filteredItems.map((item) => (
                  <TestTypeRow
                    key={item.testTypeId}
                    item={item}
                    isEditing={editingId === item.testTypeId}
                    editForm={form}
                    saving={saving}
                    onStartEdit={startEditing}
                    onFormChange={setForm}
                    onSave={() => void saveChanges()}
                    onCancel={() => {
                      setEditingId(null)
                      setError('')
                    }}
                  />
                ))}

                {filteredItems.length === 0 && (
                  <tr>
                    <td colSpan={5}>
                      <EmptyState
                        icon={<TestTypeIcon size={36} />}
                        message={
                          items.length === 0
                            ? 'No test types found.'
                            : 'No test types match your search.'
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
            Showing {filteredItems.length} of {items.length} test types
          </div>
        )}
      </section>
    </section>
  )
}
