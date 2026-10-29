import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import {
  createLocalDrivingLicenseApplication,
  getLocalApplicationCreateInfo,
  getLocalLicenseClasses,
  getPersonById,
  getPersonByNationalNo,
  updateLocalDrivingLicenseApplication,
} from '../../services/localDrivingLicenseApplicationService'
import type {
  LocalApplicationPerson,
  LocalDrivingLicenseApplication,
  LocalLicenseClass,
} from '../../types/LocalDrivingLicenseApplication'

type Props = {
  accessToken: string
  mode: 'create' | 'edit'
  application?: LocalDrivingLicenseApplication
  onCancel: () => void
  onSaved: () => void
}

type Step = 'person' | 'application'

export default function LocalApplicationForm({ accessToken, mode, application, onCancel, onSaved }: Props) {
  const isEdit = mode === 'edit'
  const [step, setStep] = useState<Step>(isEdit ? 'application' : 'person')
  const [searchBy, setSearchBy] = useState<'id' | 'nationalNo'>('id')
  const [searchValue, setSearchValue] = useState('')
  const [person, setPerson] = useState<LocalApplicationPerson | null>(null)
  const [classes, setClasses] = useState<LocalLicenseClass[]>([])
  const [selectedClassId, setSelectedClassId] = useState(String(application?.licenseClassId ?? ''))
  const [applicationFees, setApplicationFees] = useState(0)
  const [loading, setLoading] = useState(true)
  const [searching, setSearching] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  useEffect(() => {
    let cancelled = false
    async function initialize() {
      setLoading(true)
      setError('')
      try {
        const [licenseClasses, createInfo] = await Promise.all([
          getLocalLicenseClasses(accessToken),
          getLocalApplicationCreateInfo(accessToken),
        ])
        if (cancelled) return
        setClasses(licenseClasses)
        setApplicationFees(createInfo.applicationFees)
        if (!selectedClassId && licenseClasses.length) setSelectedClassId(String(licenseClasses[0].licenseClassId))
        if (application) {
          const found = await getPersonById(accessToken, application.applicantPersonId)
          if (!cancelled) setPerson(found)
        }
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Could not initialize the form.')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    void initialize()
    return () => { cancelled = true }
  // selectedClassId intentionally initializes from the first API result only once.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accessToken, application])

  const selectedClass = useMemo(() => classes.find((item) => item.licenseClassId === Number(selectedClassId)), [classes, selectedClassId])

  async function searchPerson(event?: FormEvent) {
    event?.preventDefault()
    if (searching) return
    const query = searchValue.trim()
    if (!query) { setError('Enter a Person ID or national number first.'); return }
    if (searchBy === 'id' && (!/^\d+$/.test(query) || Number(query) <= 0)) { setError('Person ID must be a positive number.'); return }
    setSearching(true); setError(''); setNotice(''); setPerson(null)
    try {
      const found = searchBy === 'id'
        ? await getPersonById(accessToken, Number(query))
        : await getPersonByNationalNo(accessToken, query)
      setPerson(found)
      setNotice('Applicant found. Review the details before continuing.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Person search failed.')
    } finally { setSearching(false) }
  }

  async function saveApplication(event: FormEvent) {
    event.preventDefault()
    if (saving) return
    setError(''); setNotice('')
    if (!isEdit && !person) { setError('Search for and select a person first.'); setStep('person'); return }
    if (!selectedClassId) { setError('Select a license class.'); return }
    setSaving(true)
    try {
      if (isEdit && application) {
        await updateLocalDrivingLicenseApplication(accessToken, application.localDrivingLicenseApplicationId, { licenseClassId: Number(selectedClassId) })
      } else {
        const created = await createLocalDrivingLicenseApplication(accessToken, { applicantPersonId: person!.personId, licenseClassId: Number(selectedClassId) })
        setNotice(`Application #${created.localDrivingLicenseApplicationId} was created successfully.`)
      }
      onSaved()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not save the application.')
    } finally { setSaving(false) }
  }

  return <div className="local-app-form-page">
    <div className="local-app-form-topline"><button type="button" className="local-app-back" onClick={onCancel}>← Back to applications</button><span>{isEdit ? 'EDIT RECORD' : 'NEW REQUEST'}</span></div>
    <div className="local-app-form-heading"><div><p className="eyebrow">LOCAL DRIVING LICENSE</p><h2>{isEdit ? 'Edit application' : 'Create a new application'}</h2><p>{isEdit ? 'Update the license class for this application.' : 'Complete the applicant details and application information to submit a request.'}</p></div><div className="local-app-form-number">{isEdit ? `#${application?.localDrivingLicenseApplicationId}` : '01'}<small>{isEdit ? 'APPLICATION ID' : 'APPLICATION SETUP'}</small></div></div>

    {!isEdit && <div className="local-app-stepper" aria-label="Application steps"><div className={`local-app-step ${step === 'person' ? 'active' : 'complete'}`}><span>01</span><div><strong>Applicant</strong><small>Identify the person</small></div></div><div className="local-app-step-line" /><div className={`local-app-step ${step === 'application' ? 'active' : ''}`}><span>02</span><div><strong>Application details</strong><small>Choose license class</small></div></div></div>}

    {error && <div className="local-app-alert" role="alert"><div><strong>Something needs your attention</strong><p>{error}</p></div></div>}
    {notice && <div className="local-app-notice" role="status">{notice}</div>}
    {loading ? <div className="local-app-state"><span className="local-app-spinner" />Loading application data…</div> : <>
      {step === 'person' && !isEdit && <section className="local-app-form-card"><div className="local-app-form-card-title"><span className="local-app-section-icon">01</span><div><h3>Applicant identification</h3><p>Find an existing person using their Person ID or national number.</p></div></div>
        <form onSubmit={searchPerson} className="local-app-person-search"><label>Search using<select value={searchBy} onChange={(event) => setSearchBy(event.target.value as 'id' | 'nationalNo')}><option value="id">Person ID</option><option value="nationalNo">National number</option></select></label><label className="local-app-person-query">{searchBy === 'id' ? 'Person ID' : 'National number'}<input value={searchValue} onChange={(event) => setSearchValue(event.target.value)} placeholder={searchBy === 'id' ? 'e.g. 1042' : 'Enter national number'} /></label><button className="local-app-primary" type="submit" disabled={searching}>{searching ? 'Searching…' : 'Find person'}</button></form>
        {person ? <div className="local-app-person-card"><div className="local-app-person-avatar">{person.fullName.trim().split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase()}</div><div className="local-app-person-main"><span className="local-app-kicker">PERSON VERIFIED</span><h4>{person.fullName}</h4><p>National No. <strong>{person.nationalNo}</strong></p><div className="local-app-person-meta"><span>Person ID <strong>{person.personId}</strong></span><span>Country <strong>{person.countryName || '—'}</strong></span><span>Phone <strong>{person.phone || '—'}</strong></span></div></div><span className="local-app-verified">✓ Verified</span></div> : <div className="local-app-person-empty"><span>◎</span><div><strong>No applicant selected</strong><p>Search for a person above. The application cannot continue until a match is selected.</p></div></div>}
        <div className="local-app-form-actions"><button type="button" className="local-app-secondary" onClick={onCancel}>Cancel</button><button type="button" className="local-app-primary" disabled={!person} onClick={() => { setError(''); setStep('application') }}>Continue to application details <span>→</span></button></div>
      </section>}

      {step === 'application' && <form onSubmit={saveApplication}><section className="local-app-form-card"><div className="local-app-form-card-title"><span className="local-app-section-icon">02</span><div><h3>Application information</h3><p>Confirm the application record and select the requested license class.</p></div></div>
        {!isEdit && person && <div className="local-app-selected-person"><span className="local-app-person-avatar small">{person.fullName.trim().split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase()}</span><div><small>APPLICANT</small><strong>{person.fullName}</strong><span>{person.nationalNo}</span></div><button type="button" className="local-app-text-button" onClick={() => setStep('person')}>Change person</button></div>}
        <div className="local-app-details-grid"><label>Application ID<div className="local-app-readonly">{isEdit ? `#${application?.localDrivingLicenseApplicationId}` : 'Assigned on save'}</div></label><label>Application date<div className="local-app-readonly">{isEdit && application ? new Date(application.applicationDate).toLocaleDateString() : new Date().toLocaleDateString()}</div></label><label className="local-app-class-select">License class<select value={selectedClassId} onChange={(event) => setSelectedClassId(event.target.value)} required><option value="" disabled>Select a license class</option>{classes.map((licenseClass) => <option key={licenseClass.licenseClassId} value={licenseClass.licenseClassId}>{licenseClass.licenseClassName}</option>)}</select>{selectedClass?.licenseClassDescription && <small>{selectedClass.licenseClassDescription}</small>}</label><label>Application fees<div className="local-app-fee-value">{applicationFees.toFixed(2)} <span>JOD</span></div></label></div>
        {selectedClass && <div className="local-app-class-summary"><span className="local-app-class-summary-icon">▤</span><div><strong>{selectedClass.licenseClassName}</strong><p>{selectedClass.licenseClassDescription || 'Driving license class'}</p></div><div className="local-app-class-fee"><small>LICENSE CLASS FEES</small><strong>{selectedClass.licenseClassFees.toFixed(2)} JOD</strong></div></div>}
        <div className="local-app-form-actions"><button type="button" className="local-app-secondary" onClick={() => isEdit ? onCancel() : setStep('person')}>{isEdit ? 'Cancel' : 'Back'}</button><button type="submit" className="local-app-primary" disabled={saving || !selectedClassId}>{saving ? 'Saving…' : isEdit ? 'Save changes' : 'Create application'} <span>→</span></button></div>
      </section></form>}
    </>}
  </div>
}
