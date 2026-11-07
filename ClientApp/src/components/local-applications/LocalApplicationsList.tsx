import { useCallback } from 'react'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import {
  FileText,
  Pencil,
  Trash2,
  XCircle,
  CalendarClock,
  Eye,
  FileSignature,
  Car,
  Award,
  IdCard,
  History,
} from 'lucide-react'
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
  onShowDetails: (application: LocalDrivingLicenseApplication) => void
  onDelete: (application: LocalDrivingLicenseApplication) => void
  onCancel: (application: LocalDrivingLicenseApplication) => void
  onScheduleTest: (application: LocalDrivingLicenseApplication, testTypeId: 1 | 2 | 3) => void
  onManageTests?: (application: LocalDrivingLicenseApplication, testTypeId?: 1 | 2 | 3) => void
  onIssueLicense: (application: LocalDrivingLicenseApplication) => void
  onShowLicense: (application: LocalDrivingLicenseApplication) => void
  onShowPersonLicenseHistory: (personId: number) => void
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

export default function LocalApplicationsList({
  items,
  loading,
  onEdit,
  onShowDetails,
  onDelete,
  onCancel,
  onScheduleTest,
  onManageTests,
  onIssueLicense,
  onShowLicense,
  onShowPersonLicenseHistory,
}: Props) {
  const columns: DataTableColumn<LocalDrivingLicenseApplication>[] = [
    {
      key: 'id',
      header: 'Application ID',
      render: (item) => (
        <span className="local-app-id">#{item.localDrivingLicenseApplicationId}</span>
      ),
    },
    {
      key: 'class',
      header: 'Driving class',
      render: (item) => (
        <>
          <span className="local-app-class">
            {item.licenseClassName || `Class ${item.licenseClassId}`}
          </span>
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
      render: (item) => (
        <strong className="local-app-person-name">
          {item.fullName || 'Unknown applicant'}
        </strong>
      ),
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
  ]

  const getRowContextMenu = useCallback(
    (
      item: LocalDrivingLicenseApplication,
    ): DataTableRowAction<LocalDrivingLicenseApplication>[] => {
      const status = (item.statusText || item.applicationStatus || 'New')
        .trim()
        .toLowerCase()
      const isNew = status === 'new'
      const hasValidApplicant = Boolean(item.applicantPersonId && item.applicantPersonId > 0)

      return [
        {
          key: 'show-details',
          label: 'Show Application Details',
          icon: <FileText className="size-4 text-cyan-400" />,
          onClick: () => onShowDetails(item),
        },
        {
          key: 'edit',
          label: 'Edit Application',
          icon: <Pencil className="size-4 text-amber-400" />,
          disabled: !isNew,
          onClick: () => onEdit(item),
        },
        {
          key: 'delete',
          label: 'Delete Application',
          icon: <Trash2 className="size-4 text-rose-400" />,
          danger: true,
          disabled: !isNew,
          onClick: () => onDelete(item),
        },
        {
          key: 'cancel',
          label: 'Cancel Application',
          icon: <XCircle className="size-4 text-rose-400" />,
          danger: true,
          separatorAfter: true,
          disabled: !isNew,
          onClick: () => onCancel(item),
        },
        {
          key: 'manage-tests',
          label: 'Manage Test Appointments',
          icon: <CalendarClock className="size-4 text-cyan-400" />,
          onClick: () => {
            if (onManageTests) {
              onManageTests(item)
            }
          },
        },
        {
          key: 'schedule-tests',
          label: 'Schedule Tests',
          icon: <CalendarClock className="size-4 text-sky-400" />,
          separatorAfter: true,
          disabled: !isNew || item.passedTest >= 3,
          onClick: () => {},
          children: [
            {
              key: 'schedule-vision',
              label: 'Schedule Vision Test',
              icon: <Eye className="size-4 text-cyan-400" />,
              disabled: !isNew || item.passedTest !== 0,
              onClick: () => onScheduleTest(item, 1),
            },
            {
              key: 'schedule-written',
              label: 'Schedule Written Test',
              icon: <FileSignature className="size-4 text-indigo-400" />,
              disabled: !isNew || item.passedTest !== 1,
              onClick: () => onScheduleTest(item, 2),
            },
            {
              key: 'schedule-street',
              label: 'Schedule Street Test',
              icon: <Car className="size-4 text-emerald-400" />,
              disabled: !isNew || item.passedTest !== 2,
              onClick: () => onScheduleTest(item, 3),
            },
          ],
        },
        {
          key: 'issue-license',
          label: 'Issue Driving License',
          icon: <Award className="size-4 text-emerald-400" />,
          disabled: !isNew || item.passedTest !== 3 || item.hasLicense,
          onClick: () => onIssueLicense(item),
        },
        {
          key: 'show-license',
          label: 'Show License',
          icon: <IdCard className="size-4 text-cyan-400" />,
          disabled: !item.hasLicense,
          onClick: () => onShowLicense(item),
        },
        {
          key: 'show-history',
          label: 'Show Person License History',
          icon: <History className="size-4 text-indigo-400" />,
          disabled: !hasValidApplicant,
          onClick: () => onShowPersonLicenseHistory(item.applicantPersonId),
        },
      ]
    },
    [
      onShowDetails,
      onEdit,
      onDelete,
      onCancel,
      onScheduleTest,
      onManageTests,
      onIssueLicense,
      onShowLicense,
      onShowPersonLicenseHistory,
    ],
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
