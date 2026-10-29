import type { InternationalLicense } from '../../types/InternationalLicense'

type Props = {
  items: InternationalLicense[]
  onOpenDetails?: (id: number) => void
}

function formatDate(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString()
}

export default function InternationalLicenseTable({ items, onOpenDetails }: Props) {
  return (
    <div className="intl-table-wrap">
      <table className="intl-table">
        <thead><tr>
          <th>International ID</th><th>Application ID</th><th>Driver ID</th>
          <th>Local License ID</th><th>Issue Date</th><th>Expiration Date</th><th>Status</th><th />
        </tr></thead>
        <tbody>
          {items.map((item) => <tr key={item.internationalLicenseId}>
            <td><strong className="intl-id">#{item.internationalLicenseId}</strong></td>
            <td>{item.applicationId}</td>
            <td>{item.driverId}</td>
            <td>{item.issuedUsingLocalLicenseId}</td>
            <td>{formatDate(item.issueDate)}</td>
            <td>{formatDate(item.expirationDate)}</td>
            <td><span className={`intl-status ${item.isActive ? 'is-active' : 'is-inactive'}`}><i />{item.isActive ? 'Active' : 'Inactive'}</span></td>
            <td>{onOpenDetails && <button className="intl-row-action" type="button" onClick={() => onOpenDetails(item.internationalLicenseId)} aria-label={`View license ${item.internationalLicenseId}`}>View ↗</button>}</td>
          </tr>)}
          {items.length === 0 && <tr><td colSpan={8} className="intl-empty">No international licenses match your filters.</td></tr>}
        </tbody>
      </table>
    </div>
  )
}
