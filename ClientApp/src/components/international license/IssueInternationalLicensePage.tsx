
import { useState } from 'react'
import type { FormEvent } from 'react'
import type {
  InternationalLicenseDetails,
  LocalLicenseInfo,
} from '../../types/InternationalLicense'
import {
  getLocalLicenseInfo,
  issueInternationalLicense,
} from '../../services/internationalLicenseService'
import { formatDate, formatCurrency } from '../../utils/formatters'
import PageHero from '../common/PageHero'
import './international-licenses.css'

type Props = {
  accessToken: string
  onBack: () => void
  onIssued: () => void
}

export default function IssueInternationalLicensePage({
  accessToken,
  onBack,
  onIssued,
}: Props) {
  const [licenseId, setLicenseId] = useState('')
  const [license, setLicense] = useState<LocalLicenseInfo | null>(null)
  const [issued, setIssued] = useState<InternationalLicenseDetails | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  function handleLicenseIdChange(value: string) {
    setLicenseId(value)
    setError('')
    setNotice('')
    setLicense(null)
    setIssued(null)
  }

  async function searchLicense(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (loading) return

    setError('')
    setNotice('')
    setLicense(null)
    setIssued(null)

    const id = Number(licenseId)

    if (!Number.isInteger(id) || id <= 0) {
      setError('Enter a valid positive local license ID.')
      return
    }

    setLoading(true)

    try {
      const result = await getLocalLicenseInfo(accessToken, id)

      setLicense(result)
      setNotice(
        'Local license information loaded. Review eligibility before issuing.',
      )
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Could not find local license information.',
      )
    } finally {
      setLoading(false)
    }
  }

  async function issue() {
    if (!license || saving || issued) return

    setSaving(true)
    setError('')
    setNotice('')

    try {
      const result = await issueInternationalLicense(
        accessToken,
        license.licenseId,
      )

      setIssued(result)
      setNotice(
        `International license #${result.internationalLicenseId} issued successfully.`,
      )
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Could not issue the international license.',
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="page-content intl-page">
      <div className="intl-breadcrumb">
        <button type="button" onClick={onBack}>
          ← International Licenses
        </button>
        <span>/</span>
        <span>Issue new</span>
      </div>

      <PageHero
        pageId="Issue International License"
        actions={
          <span className="intl-step-pill">
            <span>01</span> Find local license{' '}
            <span className="intl-step-arrow">→</span> <span>02</span> Issue
          </span>
        }
      />

      {error && (
        <div className="intl-error" role="alert">
          <strong>Something needs attention</strong>
          <p>{error}</p>
        </div>
      )}

      {notice && (
        <div
          className={`intl-notice ${issued ? 'success' : ''}`}
          role="status"
        >
          {notice}
        </div>
      )}

      <section className="intl-panel intl-form-panel">
        <div className="intl-panel-heading">
          <div>
            <h3>Eligible Local License</h3>
            <p>
              Search using the local license ID, as in the original WPF
              workflow.
            </p>
          </div>
          <span className="intl-section-number">01</span>
        </div>

        <form className="intl-license-search" onSubmit={searchLicense}>
          <label htmlFor="local-license-id">Local License ID</label>

          <div className="intl-search-row">
            <input
              id="local-license-id"
              type="number"
              min="1"
              value={licenseId}
              onChange={(event) =>
                handleLicenseIdChange(event.target.value)
              }
              placeholder="e.g. 1042"
              required
            />

            <button
              className="intl-primary-button"
              type="submit"
              disabled={loading || saving}
            >
              {loading ? 'Searching…' : 'Search license'}
            </button>
          </div>

          <small>
            Enter the ID of the local license to retrieve its current details.
          </small>
        </form>

        {license && (
          <div className="intl-license-details">
            <div className="intl-person-banner">
              <div className="intl-person-avatar">
                {license.fullName?.trim().charAt(0) || 'P'}
              </div>

              <div>
                <span>License holder</span>
                <h3>{license.fullName}</h3>
                <p>
                  National No. {license.nationalNo} · Driver ID{' '}
                  {license.driverId}
                </p>
              </div>

              <span
                className={`intl-status ${
                  license.isActive && !license.isDetained
                    ? 'is-active'
                    : 'is-inactive'
                }`}
              >
                <i />
                {license.isDetained
                  ? 'Detained'
                  : license.isActive
                    ? 'Active'
                    : 'Inactive'}
              </span>
            </div>

            <div className="intl-detail-grid">
              <div>
                <span>Local License ID</span>
                <strong>#{license.licenseId}</strong>
              </div>
              <div>
                <span>License Class</span>
                <strong>{license.licenseClass}</strong>
              </div>
              <div>
                <span>Issue Date</span>
                <strong>{formatDate(license.issueDate)}</strong>
              </div>
              <div>
                <span>Expiration Date</span>
                <strong>{formatDate(license.expirationDate)}</strong>
              </div>
              <div>
                <span>Date of Birth</span>
                <strong>{formatDate(license.dateOfBirth)}</strong>
              </div>
              <div>
                <span>Gender</span>
                <strong>{license.gender || '—'}</strong>
              </div>
            </div>

            <div className="intl-eligibility-note">
              <span>ⓘ</span>
              <p>
                The API is responsible for final eligibility validation. If
                the local license is not eligible, the backend will return the
                reason.
              </p>
            </div>

            <div className="intl-form-actions">
              {issued && (
                <button
                  className="intl-secondary-button"
                  type="button"
                  onClick={onIssued}
                >
                  Back to licenses
                </button>
              )}

              <button
                className="intl-primary-button"
                type="button"
                onClick={() => void issue()}
                disabled={
                  saving ||
                  Boolean(issued) ||
                  !license.isActive ||
                  license.isDetained
                }
              >
                {saving
                  ? 'Issuing…'
                  : issued
                    ? 'License issued'
                    : 'Issue international license'}
              </button>
            </div>
          </div>
        )}
      </section>

      {issued && (
        <section className="intl-panel intl-issued-panel">
          <div className="intl-panel-heading">
            <div>
              <h3>International License Issued</h3>
              <p>The API confirmed the issue operation.</p>
            </div>

            <span className="intl-status is-active">
              <i />
              Issued
            </span>
          </div>

          <div className="intl-detail-grid">
            <div>
              <span>International License ID</span>
              <strong>#{issued.internationalLicenseId}</strong>
            </div>
            <div>
              <span>Application ID</span>
              <strong>{issued.applicationId}</strong>
            </div>
            <div>
              <span>Issue Date</span>
              <strong>{formatDate(issued.issueDate)}</strong>
            </div>
            <div>
              <span>Expiration Date</span>
              <strong>{formatDate(issued.expirationDate)}</strong>
            </div>
            <div>
              <span>Fees</span>
              <strong>{formatCurrency(issued.fees, '')}</strong>
            </div>
            <div>
              <span>Created By</span>
              <strong>
                {issued.createdByUserName || issued.createdByUserId}
              </strong>
            </div>
          </div>

          <div className="intl-form-actions">
            <button
              className="intl-primary-button"
              type="button"
              onClick={onIssued}
            >
              Return to licenses
            </button>
          </div>
        </section>
      )}

      <div className="intl-bottom-actions">
        <button
          className="intl-secondary-button"
          type="button"
          onClick={onBack}
        >
          Cancel and return
        </button>
      </div>
    </section>
  )
}
