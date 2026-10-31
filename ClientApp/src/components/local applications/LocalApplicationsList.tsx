import { useCallback } from 'react'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import { Pencil as PencilIcon } from 'lucide-react'
import { formatDate } from '../../utils/formatters'
import DataTable, {
  type DataTableColumn,
  type DataTableRowAction,
} from '../common/DataTable'
import StatusBadge, { type StatusVariant } from '../common/StatusBadge'

type Props = {
  items: LocalDrivingLicenseApplication[]
  loading: boolean
  onEdit: (application: LocalDrivingLicenseApplication) => void
}

function getApplicationStatusVariant(status: string): StatusVariant {
  switch (status.trim().toLowerCase()) {
    case 'completed':
      return 'success'
    case 'cancelled':
    case 'canceled':
      return 'danger'
    case 'new':
      return 'warning'
    default:
      return 'neutral'
  }
}

export default function LocalApplicationsList({ items, loading, onEdit }: Props) {
  const columns: DataTableColumn<LocalDrivingLicenseApplication>[] = [
    {
      key: 'id',
      header: 'Application ID',
      render: (item) => <span className="local-app-id">#{item.localDrivingLicenseApplicationId}</span>,
    },
    {
      key: 'class',
      header: 'Driving class',
      render: (item) => (
        <>
          <span className="local-app-class">{item.licenseClassName || `Class ${item.licenseClassId}`}</span>
          <small className="local-app-cell-sub">Class ID {item.licenseClassId}</small>
        </>
      ),
    },
    {
      key: 'nationalNo',
      header: 'National No.',
      className: 'local-app-mono',
      accessor: (item) => item.nationalNo || '—',
    },
    {
      key: 'applicant',
      header: 'Applicant',
      render: (item) => <strong className="local-app-person-name">{item.fullName || 'Unknown applicant'}</strong>,
    },
    {
      key: 'date',
      header: 'Application date',
      render: (item) => formatDate(item.applicationDate),
    },
    {
      key: 'testsPassed',
      header: 'Tests passed',
      render: (item) => (
        <span className={`local-app-test-count ${item.passedTest >= 3 ? 'all-passed' : ''}`}>
          {item.passedTest} <span>/ 3</span>
        </span>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      render: (item) => {
        const status = item.statusText || item.applicationStatus || 'New'
        return (
          <StatusBadge
            variant={getApplicationStatusVariant(status)}
            label={status}
          />
        )
      },
    },
    {
      key: 'actions',
      header: '',
      headerClassName: 'w-12',
      render: (item) => {
        const status = item.statusText || item.applicationStatus || 'New'
        return (
          <button
            type="button"
            className="local-app-row-action"
            disabled={status.toLowerCase() !== 'new'}
            onClick={() => onEdit(item)}
            title="Edit application"
            aria-label={`Edit application #${item.localDrivingLicenseApplicationId}`}
          >
            <PencilIcon size={14} />
          </button>
        )
      },
    },
  ]

  const getRowContextMenu = useCallback(
    (
      item: LocalDrivingLicenseApplication,
    ): DataTableRowAction<LocalDrivingLicenseApplication>[] => {
      const status = (item.statusText || item.applicationStatus || 'New')
        .trim()
        .toLowerCase()

      return [
        {
          key: 'edit',
          label: 'Edit Application',
          icon: <PencilIcon className="size-4" />,
          disabled: status !== 'new',
          onClick: (application) => onEdit(application),
        },
      ]
    },
    [onEdit],
  )

  return (
    <DataTable<LocalDrivingLicenseApplication>
      data={items}
      columns={columns}
      getRowKey={(item) => item.localDrivingLicenseApplicationId}
      loading={loading}
      loadingComponent={
        <div className="local-app-state">
          <span className="local-app-spinner" />
          Loading applications…
        </div>
      }
      emptyMessage="No applications match the selected filters."
      tableClassName="local-app-table"
      containerClassName="local-app-table-wrap"
      minWidth={920}
      rowContextMenu={getRowContextMenu}
    />
  )
}
