import { useCallback, useMemo, useState } from 'react'
import {
  getApplicationTypes,
  updateApplicationType,
} from '../services/applicationTypeService'
import type { ApplicationType } from '../types/ApplicationType'
import { useAsyncData, useTableFilters } from '../hooks'
import PageHero from './common/PageHero'
import TableToolbar from './common/TableToolbar'
import ErrorAlert from './common/ErrorAlert'
import EmptyState from './common/EmptyState'
import DataTableContainer from './common/DataTableContainer'
import ApplicationTypeRow from './applicationTypes/ApplicationTypeRow'
import { FileText as ApplicationsIcon } from 'lucide-react'

type ApplicationTypesPageProps = {
  accessToken: string
}

type EditForm = {
  applicationTypeTitle: string
  applicationTypeFees: string
}

type SearchBy = 'all' | 'title' | 'id'

const SEARCH_OPTIONS: { value: SearchBy; label: string }[] = [
  { value: 'all', label: 'All Fields' },
  { value: 'title', label: 'Type Name' },
  { value: 'id', label: 'Type ID' },
]

export default function ApplicationTypesPage({
  accessToken,
}: ApplicationTypesPageProps) {
  const [success, setSuccess] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editForm, setEditForm] = useState<EditForm>({
    applicationTypeTitle: '',
    applicationTypeFees: '',
  })
  const [saving, setSaving] = useState(false)

  const fetcher = useCallback(() => getApplicationTypes(accessToken), [accessToken])

  const {
    data: items,
    loading,
    error,
    refresh,
    setError,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    initialData: [],
    defaultErrorMessage: 'Failed to load application types.',
  })

  const {
    search,
    setSearch,
    searchField: searchBy,
    setSearchField: setSearchBy,
    filteredData: filteredItems,
    isFiltered,
    resetFilters: handleResetFilters,
  } = useTableFilters<ApplicationType, SearchBy>({
    data: items,
    initialSearchField: 'all',
    filterItem: (item, query, field) => {
      if (!query) return true

      if (field === 'title') {
        return item.applicationTypeTitle.toLowerCase().includes(query)
      }
      if (field === 'id') {
        return String(item.applicationTypeId).includes(query)
      }

      // 'all'
      return (
        item.applicationTypeTitle.toLowerCase().includes(query) ||
        String(item.applicationTypeId).includes(query)
      )
    },
  })

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

      await refresh()
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

        <TableToolbar<SearchBy>
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={placeholderText}
          searchField={searchBy}
          searchFieldOptions={SEARCH_OPTIONS}
          onSearchFieldChange={setSearchBy}
          onReset={handleResetFilters}
          isFiltered={isFiltered}
          onRefresh={() => {
            setSuccess('')
            void refresh()
          }}
          refreshing={loading}
        />

        {error && (
          <div style={{ margin: '0 25px 20px' }}>
            <ErrorAlert
              message={error}
              onRetry={() => {
                setSuccess('')
                void refresh()
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
          <DataTableContainer minWidth={680}>
            <table className="people-table">
              <thead>
                <tr>
                  <th className="w-16 col-compact">ID</th>
                  <th>Application Type</th>
                  <th className="w-28 whitespace-nowrap">Fees</th>
                  <th className="w-32 whitespace-nowrap">Actions</th>
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
          </DataTableContainer>
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
