import type { InternationalLicense } from '../../types/InternationalLicense'
import { formatDate } from '../../utils/formatters'
import DataTable, { type DataTableColumn } from '../common/DataTable'
import StatusBadge from '../common/StatusBadge'
import { getActiveStatusProps } from '../../utils/status'

type Props = {
  items: InternationalLicense[]
  onOpenDetails?: (id: number) => void
}

export default function InternationalLicenseTable({ items, onOpenDetails }: Props) {
  const columns: DataTableColumn<InternationalLicense>[] = [
    {
      key: 'internationalLicenseId',
      header: 'International ID',
      render: (item) => <strong className="intl-id">#{item.internationalLicenseId}</strong>,
    },
    {
      key: 'applicationId',
      header: 'Application ID',
      accessor: (item) => item.applicationId,
    },
    {
      key: 'driverId',
      header: 'Driver ID',
      accessor: (item) => item.driverId,
    },
    {
      key: 'issuedUsingLocalLicenseId',
      header: 'Local License ID',
      accessor: (item) => item.issuedUsingLocalLicenseId,
    },
    {
      key: 'issueDate',
      header: 'Issue Date',
      render: (item) => formatDate(item.issueDate),
    },
    {
      key: 'expirationDate',
      header: 'Expiration Date',
      render: (item) => formatDate(item.expirationDate),
    },
    {
      key: 'status',
      header: 'Status',
      render: (item) => <StatusBadge {...getActiveStatusProps(item.isActive)} />,
    },
    {
      key: 'actions',
      header: '',
      render: (item) =>
        onOpenDetails ? (
          <button
            className="intl-row-action"
            type="button"
            onClick={() => onOpenDetails(item.internationalLicenseId)}
            aria-label={`View details for international license #${item.internationalLicenseId}`}
          >
            View ↗
          </button>
        ) : null,
    },
  ]

  return (
    <DataTable<InternationalLicense>
      data={items}
      columns={columns}
      getRowKey={(item) => item.internationalLicenseId}
      tableClassName="intl-table"
      containerClassName="intl-table-wrap"
      minWidth={880}
      emptyMessage="No international licenses match your filters."
    />
  )
}
