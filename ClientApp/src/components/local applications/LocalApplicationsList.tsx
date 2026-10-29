import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import { PencilIcon } from '../common/Icons'

type Props = {
  items: LocalDrivingLicenseApplication[]
  loading: boolean
  onEdit: (application: LocalDrivingLicenseApplication) => void
}

function statusClass(status: string) {
  const value = status.toLowerCase()
  if (value === 'completed') return 'is-completed'
  if (value === 'cancelled' || value === 'canceled') return 'is-cancelled'
  return 'is-new'
}

export default function LocalApplicationsList({ items, loading, onEdit }: Props) {
  if (loading) return <div className="local-app-state"><span className="local-app-spinner" />Loading applications…</div>

  return (
    <div className="local-app-table-wrap">
      <table className="local-app-table">
        <thead><tr><th>Application ID</th><th>Driving class</th><th>National No.</th><th>Applicant</th><th>Application date</th><th>Tests passed</th><th>Status</th><th aria-label="Actions" /></tr></thead>
        <tbody>
          {items.map((item) => {
            const status = item.statusText || item.applicationStatus || 'New'
            return <tr key={item.localDrivingLicenseApplicationId}>
              <td><span className="local-app-id">#{item.localDrivingLicenseApplicationId}</span></td>
              <td><span className="local-app-class">{item.licenseClassName || `Class ${item.licenseClassId}`}</span><small className="local-app-cell-sub">Class ID {item.licenseClassId}</small></td>
              <td className="local-app-mono">{item.nationalNo || '—'}</td>
              <td><strong className="local-app-person-name">{item.fullName || 'Unknown applicant'}</strong></td>
              <td>{item.applicationDate ? new Date(item.applicationDate).toLocaleDateString() : '—'}</td>
              <td><span className={`local-app-test-count ${item.passedTest >= 3 ? 'all-passed' : ''}`}>{item.passedTest} <span>/ 3</span></span></td>
              <td><span className={`local-app-status ${statusClass(status)}`}><i />{status}</span></td>
              <td><button type="button" className="local-app-row-action" disabled={status.toLowerCase() !== 'new'} onClick={() => onEdit(item)} title="Edit application" aria-label={`Edit application ${item.localDrivingLicenseApplicationId}`}><PencilIcon size={14} /></button></td>
            </tr>
          })}
          {items.length === 0 && <tr><td colSpan={8} className="local-app-empty">{loading ? 'Loading…' : 'No applications match the selected filters.'}</td></tr>}
        </tbody>
      </table>
    </div>
  )
}
