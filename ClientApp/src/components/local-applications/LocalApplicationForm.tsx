import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import {
  ArrowLeft,
  ArrowRight,
  UserCheck,
  Search,
  CheckCircle2,
  FileText,
  Save,
  Loader2,
  Calendar,
  Layers,
  Banknote,
  Hash,
} from 'lucide-react'
import { formatDate, formatCurrency } from '../../utils/formatters'
import { cn } from '@/lib/utils'
import PageHero from '../common/PageHero'
import ErrorAlert from '../common/ErrorAlert'
import LoadingSpinner from '../common/LoadingSpinner'
import {
  Button,
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui'
import {
  createLocalDrivingLicenseApplication,
  getLocalApplicationCreateInfo,
  updateLocalDrivingLicenseApplication,
} from '../../services/localDrivingLicenseApplicationService'
import { getPersonById, getPersonByNationalNo } from '../../services/peopleService'
import { getLicenseClasses } from '../../services/licenseClassService'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import type { PersonDetails } from '../../types/Person'
import type { LicenseClass } from '../../types/LicenseClass'

type Props = {
  accessToken: string
  mode: 'create' | 'edit'
  application?: LocalDrivingLicenseApplication
  onCancel: () => void
  onSaved: () => void
}

type Step = 'person' | 'application'

export default function LocalApplicationForm({
  accessToken,
  mode,
  application,
  onCancel,
  onSaved,
}: Props) {
  const isEdit = mode === 'edit'
  const [step, setStep] = useState<Step>(isEdit ? 'application' : 'person')
  const [searchBy, setSearchBy] = useState<'id' | 'nationalNo'>('id')
  const [searchValue, setSearchValue] = useState('')
  const [person, setPerson] = useState<PersonDetails | null>(null)
  const [classes, setClasses] = useState<LicenseClass[]>([])
  const [selectedClassId, setSelectedClassId] = useState(
    String(application?.licenseClassId ?? ''),
  )
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
          getLicenseClasses(accessToken),
          getLocalApplicationCreateInfo(accessToken),
        ])
        if (cancelled) return
        setClasses(licenseClasses)
        setApplicationFees(createInfo.applicationFees)
        if (!selectedClassId && licenseClasses.length) {
          setSelectedClassId(String(licenseClasses[0].licenseClassId))
        }
        if (application) {
          const found = await getPersonById(
            accessToken,
            application.applicantPersonId,
          )
          if (!cancelled) setPerson(found)
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Could not initialize the form.',
          )
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    void initialize()
    return () => {
      cancelled = true
    }
    // selectedClassId intentionally initializes from the first API result only once.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accessToken, application])

  const selectedClass = useMemo(
    () =>
      classes.find(
        (item) => item.licenseClassId === Number(selectedClassId),
      ),
    [classes, selectedClassId],
  )

  async function searchPerson(event?: FormEvent) {
    event?.preventDefault()
    if (searching) return
    const query = searchValue.trim()
    if (!query) {
      setError('Enter a Person ID or national number first.')
      return
    }
    if (searchBy === 'id' && (!/^\d+$/.test(query) || Number(query) <= 0)) {
      setError('Person ID must be a positive number.')
      return
    }
    setSearching(true)
    setError('')
    setNotice('')
    setPerson(null)
    try {
      const found =
        searchBy === 'id'
          ? await getPersonById(accessToken, Number(query))
          : await getPersonByNationalNo(accessToken, query)
      setPerson(found)
      setNotice('Applicant found. Review the details before continuing.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Person search failed.')
    } finally {
      setSearching(false)
    }
  }

  async function saveApplication(event: FormEvent) {
    event.preventDefault()
    if (saving) return
    setError('')
    setNotice('')
    if (!isEdit && !person) {
      setError('Search for and select a person first.')
      setStep('person')
      return
    }
    if (!selectedClassId) {
      setError('Select a license class.')
      return
    }
    setSaving(true)
    try {
      if (isEdit && application) {
        await updateLocalDrivingLicenseApplication(
          accessToken,
          application.localDrivingLicenseApplicationId,
          { licenseClassId: Number(selectedClassId) },
        )
      } else {
        const created = await createLocalDrivingLicenseApplication(
          accessToken,
          {
            applicantPersonId: person!.personId,
            licenseClassId: Number(selectedClassId),
          },
        )
        setNotice(
          `Application #${created.localDrivingLicenseApplicationId} was created successfully.`,
        )
      }
      onSaved()
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Could not save the application.',
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="page-content space-y-6">
      {/* Modernized Page Hero */}
      <PageHero
        pageId="Local Applications"
        title={isEdit ? 'Edit' : 'Create'}
        highlight="Application"
        description={
          isEdit
            ? 'Update the requested driving license class for this local application record.'
            : 'Complete applicant verification and configure driving license category details to submit a request.'
        }
      />

      <div className="flex items-center">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onCancel}
          className="h-9 gap-2 text-xs font-semibold text-white bg-[#0e1b33] hover:bg-[#142647] border border-[#263750] hover:border-blue-500/40 rounded-xl shadow-xs transition-all cursor-pointer"
        >
          <ArrowLeft className="size-4 text-cyan-400" />
          <span>Back to Applications</span>
        </Button>
      </div>

      {/* Two-Step Wizard Stepper (in Create Mode) */}
      {!isEdit && (
        <nav
          aria-label="Application progress steps"
          className="flex flex-wrap sm:flex-nowrap items-center gap-3 sm:gap-4 p-4 sm:p-5 rounded-2xl bg-[#0d182e]/90 border border-blue-500/30 shadow-[0_0_20px_rgba(0,140,255,0.06)]"
        >
          <div
            className={cn(
              'flex items-center gap-3 min-w-0 transition-opacity',
              step === 'person' ? 'opacity-100' : 'opacity-70',
            )}
          >
            <span
              className={cn(
                'size-9 rounded-xl flex items-center justify-center font-bold text-xs shrink-0 transition-colors',
                step === 'person' || person
                  ? 'bg-blue-600 text-white shadow-[0_0_12px_rgba(37,99,235,0.5)]'
                  : 'bg-[#13233f] text-slate-400 border border-[#263e68]',
              )}
            >
              01
            </span>
            <div className="min-w-0">
              <strong className="block text-xs font-bold text-white truncate">
                Applicant Identification
              </strong>
              <small className="block text-[11px] text-slate-400 truncate">
                Search and verify person
              </small>
            </div>
          </div>

          <div className="hidden sm:block h-px flex-1 bg-blue-500/20" />

          <div
            className={cn(
              'flex items-center gap-3 min-w-0 transition-opacity',
              step === 'application' ? 'opacity-100' : 'opacity-70',
            )}
          >
            <span
              className={cn(
                'size-9 rounded-xl flex items-center justify-center font-bold text-xs shrink-0 transition-colors',
                step === 'application'
                  ? 'bg-blue-600 text-white shadow-[0_0_12px_rgba(37,99,235,0.5)]'
                  : 'bg-[#13233f] text-slate-400 border border-[#263e68]',
              )}
            >
              02
            </span>
            <div className="min-w-0">
              <strong className="block text-xs font-bold text-white truncate">
                Application Details
              </strong>
              <small className="block text-[11px] text-slate-400 truncate">
                Select license class & fees
              </small>
            </div>
          </div>
        </nav>
      )}

      {/* Shared Alerts */}
      {error && (
        <ErrorAlert
          title="Application Notice"
          message={error}
          onDismiss={() => setError('')}
        />
      )}

      {notice && (
        <ErrorAlert
          variant="success"
          title="Status Update"
          message={notice}
          onDismiss={() => setNotice('')}
        />
      )}

      {/* Loading Placeholder */}
      {loading ? (
        <Card className="p-12 text-center">
          <LoadingSpinner
            message="Loading application configuration..."
            size="lg"
            layout="col"
          />
        </Card>
      ) : (
        <>
          {/* STEP 1: Applicant Identification */}
          {step === 'person' && !isEdit && (
            <Card className="p-6 sm:p-8 space-y-6">
              <CardHeader className="flex-row items-center gap-3 pb-5 border-b border-[#1e3458] mb-2 space-y-0">
                <div className="size-10 rounded-xl bg-blue-500/15 border border-blue-400/30 text-cyan-400 flex items-center justify-center shrink-0">
                  <UserCheck className="size-5" />
                </div>
                <div>
                  <CardTitle>Applicant Identification</CardTitle>
                  <CardDescription className="mt-0.5">
                    Find an existing registered person using their Person ID or national number.
                  </CardDescription>
                </div>
              </CardHeader>

              {/* Search Form Bar */}
              <form onSubmit={searchPerson} className="space-y-4">
                <div className="flex flex-col sm:flex-row items-stretch sm:items-end gap-3">
                  <div className="w-full sm:w-48 shrink-0">
                    <Label
                      htmlFor="search-by-select"
                      className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2"
                    >
                      Search By
                    </Label>
                    <Select
                      value={searchBy}
                      onValueChange={(val) =>
                        setSearchBy(val as 'id' | 'nationalNo')
                      }
                    >
                      <SelectTrigger
                        id="search-by-select"
                        className="w-full h-11 bg-[#0e1a33] border-[#263e68] text-white rounded-xl"
                      >
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="id">Person ID</SelectItem>
                        <SelectItem value="nationalNo">
                          National Number
                        </SelectItem>
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="flex-1">
                    <Label
                      htmlFor="search-query-input"
                      className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2"
                    >
                      {searchBy === 'id' ? 'Person ID' : 'National Number'}
                    </Label>
                    <Input
                      id="search-query-input"
                      value={searchValue}
                      onChange={(e) => setSearchValue(e.target.value)}
                      placeholder={
                        searchBy === 'id'
                          ? 'e.g. 1042'
                          : 'Enter 10-digit national number'
                      }
                      disabled={searching}
                    />
                  </div>

                  <Button
                    type="submit"
                    disabled={searching}
                    className="h-11 px-6 gap-2 bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-500 hover:to-cyan-500 text-white font-bold rounded-xl shadow-[0_0_15px_rgba(0,140,255,0.35)] shrink-0 cursor-pointer"
                  >
                    {searching ? (
                      <>
                        <Loader2 className="size-4 animate-spin" />
                        <span>Searching...</span>
                      </>
                    ) : (
                      <>
                        <Search className="size-4" />
                        <span>Find Person</span>
                      </>
                    )}
                  </Button>
                </div>
              </form>

              {/* Verified Person Display or Empty State */}
              {person ? (
                <div className="rounded-xl border border-emerald-500/30 bg-[#091a2e]/90 p-5 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 shadow-[0_0_15px_rgba(16,185,129,0.1)]">
                  <div className="flex items-center gap-4 min-w-0">
                    <div className="size-12 rounded-xl bg-gradient-to-br from-blue-500 to-cyan-500 text-white font-bold text-sm flex items-center justify-center shrink-0 shadow-sm">
                      {person.fullName
                        .trim()
                        .split(/\s+/)
                        .slice(0, 2)
                        .map((part) => part[0])
                        .join('')
                        .toUpperCase()}
                    </div>
                    <div className="min-w-0 space-y-1">
                      <div className="flex items-center gap-2">
                        <span className="text-[10px] font-extrabold uppercase tracking-wider text-emerald-400">
                          VERIFIED CITIZEN
                        </span>
                      </div>
                      <h4 className="text-base font-bold text-white truncate">
                        {person.fullName}
                      </h4>
                      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-300">
                        <span>
                          National No.{' '}
                          <strong className="text-white">
                            {person.nationalNo}
                          </strong>
                        </span>
                        <span>·</span>
                        <span>
                          Person ID{' '}
                          <strong className="text-white font-mono">
                            #{person.personId}
                          </strong>
                        </span>
                        <span>·</span>
                        <span>
                          Country{' '}
                          <strong className="text-white">
                            {person.countryName || '—'}
                          </strong>
                        </span>
                        <span>·</span>
                        <span>
                          Phone{' '}
                          <strong className="text-white">
                            {person.phone || '—'}
                          </strong>
                        </span>
                      </div>
                    </div>
                  </div>

                  <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-bold bg-emerald-500/15 border border-emerald-500/30 text-emerald-300 shrink-0">
                    <CheckCircle2 className="size-3.5" />
                    <span>Verified</span>
                  </span>
                </div>
              ) : (
                <div className="rounded-xl border border-dashed border-[#263e68] bg-[#091326] p-8 text-center text-slate-400 space-y-1.5">
                  <UserCheck className="size-8 text-slate-500 mx-auto mb-2" />
                  <p className="font-semibold text-slate-200 text-sm">
                    No applicant selected
                  </p>
                  <p className="text-xs text-slate-400">
                    Search for a citizen or resident above to proceed with the application.
                  </p>
                </div>
              )}

              {/* Step 1 Actions */}
              <div className="flex flex-col sm:flex-row items-center justify-end gap-3 pt-4 border-t border-[#1e3458]">
                <Button
                  type="button"
                  variant="outline"
                  onClick={onCancel}
                  className="w-full sm:w-auto h-11 px-5 rounded-xl border-[#263e68] text-slate-300 hover:bg-[#16274a] cursor-pointer"
                >
                  Cancel
                </Button>
                <Button
                  type="button"
                  disabled={!person}
                  onClick={() => {
                    setError('')
                    setStep('application')
                  }}
                  className="w-full sm:w-auto h-11 px-6 gap-2 bg-gradient-to-r from-blue-600 via-blue-500 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 text-white font-bold rounded-xl shadow-[0_0_20px_rgba(0,140,255,0.4)] disabled:opacity-50 cursor-pointer"
                >
                  <span>Continue to Application Details</span>
                  <ArrowRight className="size-4" />
                </Button>
              </div>
            </Card>
          )}

          {/* STEP 2: Application Information */}
          {(step === 'application' || isEdit) && (
            <form onSubmit={saveApplication} className="space-y-6">
              <Card className="p-6 sm:p-8 space-y-6">
                <CardHeader className="flex-row items-center gap-3 pb-5 border-b border-[#1e3458] mb-2 space-y-0">
                  <div className="size-10 rounded-xl bg-blue-500/15 border border-blue-400/30 text-cyan-400 flex items-center justify-center shrink-0">
                    <FileText className="size-5" />
                  </div>
                  <div>
                    <CardTitle>Application Information</CardTitle>
                    <CardDescription className="mt-0.5">
                      Confirm applicant credentials and select the requested driving license class.
                    </CardDescription>
                  </div>
                </CardHeader>

                {/* Selected Applicant Summary Bar */}
                {!isEdit && person && (
                  <div className="flex items-center justify-between gap-3 p-3.5 rounded-xl bg-[#09152b] border border-[#1b2f52]">
                    <div className="flex items-center gap-3 min-w-0">
                      <span className="size-8 rounded-lg bg-blue-500/20 text-cyan-400 font-bold text-xs flex items-center justify-center shrink-0">
                        {person.fullName
                          .trim()
                          .split(/\s+/)
                          .slice(0, 2)
                          .map((p) => p[0])
                          .join('')
                          .toUpperCase()}
                      </span>
                      <div className="min-w-0">
                        <span className="text-[10px] uppercase font-bold text-slate-400 block">
                          APPLICANT
                        </span>
                        <span className="text-xs font-bold text-white truncate block">
                          {person.fullName}{' '}
                          <span className="text-slate-400 font-normal">
                            ({person.nationalNo})
                          </span>
                        </span>
                      </div>
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => setStep('person')}
                      className="text-xs text-cyan-400 hover:text-cyan-300 hover:bg-cyan-950/30 cursor-pointer"
                    >
                      Change Person
                    </Button>
                  </div>
                )}

                {/* Details Grid */}
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
                  {/* Application ID */}
                  <div>
                    <Label className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
                      Application ID
                    </Label>
                    <div className="h-11 px-3.5 rounded-xl bg-[#091326] border border-[#1b2f52] text-slate-300 font-mono text-sm flex items-center gap-2">
                      <Hash className="size-4 text-slate-500" />
                      <span>
                        {isEdit
                          ? `#${application?.localDrivingLicenseApplicationId}`
                          : 'Assigned on save'}
                      </span>
                    </div>
                  </div>

                  {/* Application Date */}
                  <div>
                    <Label className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
                      Application Date
                    </Label>
                    <div className="h-11 px-3.5 rounded-xl bg-[#091326] border border-[#1b2f52] text-slate-300 text-sm flex items-center gap-2">
                      <Calendar className="size-4 text-slate-500" />
                      <span>
                        {formatDate(
                          isEdit && application
                            ? application.applicationDate
                            : new Date(),
                        )}
                      </span>
                    </div>
                  </div>

                  {/* License Class */}
                  <div>
                    <Label
                      htmlFor="license-class-select"
                      className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2"
                    >
                      License Class <span className="text-rose-400">*</span>
                    </Label>
                    <Select
                      value={selectedClassId || undefined}
                      onValueChange={setSelectedClassId}
                      disabled={saving}
                    >
                      <SelectTrigger
                        id="license-class-select"
                        aria-label="License class"
                        className="w-full h-11 bg-[#0e1a33] border-[#263e68] text-white rounded-xl hover:border-cyan-400/50 focus:border-cyan-400"
                      >
                        <SelectValue placeholder="Select license class" />
                      </SelectTrigger>
                      <SelectContent className="max-h-64">
                        {classes.map((licenseClass) => (
                          <SelectItem
                            key={licenseClass.licenseClassId}
                            value={String(licenseClass.licenseClassId)}
                          >
                            {licenseClass.licenseClassName}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  {/* Application Fees */}
                  <div>
                    <Label className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
                      Application Fees
                    </Label>
                    <div className="h-11 px-3.5 rounded-xl bg-[#091326] border border-[#1b2f52] text-emerald-400 font-bold text-sm flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Banknote className="size-4 text-emerald-500" />
                        <span>{formatCurrency(applicationFees, '')}</span>
                      </div>
                      <span className="text-xs text-slate-400 font-normal">
                        JOD
                      </span>
                    </div>
                  </div>
                </div>

                {/* Selected Class Summary Card */}
                {selectedClass && (
                  <div className="rounded-xl border border-blue-500/30 bg-[#08152e] p-4 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 shadow-[0_0_15px_rgba(0,140,255,0.08)]">
                    <div className="flex items-center gap-3">
                      <div className="size-9 rounded-lg bg-blue-500/15 border border-blue-400/30 text-cyan-400 flex items-center justify-center shrink-0">
                        <Layers className="size-4.5" />
                      </div>
                      <div>
                        <strong className="text-sm font-bold text-white block">
                          {selectedClass.licenseClassName}
                        </strong>
                        <p className="text-xs text-slate-400 mt-0.5">
                          {selectedClass.licenseClassDescription ||
                            'Standard driving license class'}
                        </p>
                      </div>
                    </div>
                    <div className="text-right self-end sm:self-auto">
                      <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                        LICENSE CLASS FEES
                      </span>
                      <strong className="text-sm font-bold text-emerald-400">
                        {formatCurrency(selectedClass.licenseClassFees, 'JOD')}
                      </strong>
                    </div>
                  </div>
                )}

                {/* Step 2 Actions */}
                <div className="flex flex-col sm:flex-row items-center justify-end gap-3 pt-4 border-t border-[#1e3458]">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => (isEdit ? onCancel() : setStep('person'))}
                    className="w-full sm:w-auto h-11 px-5 rounded-xl border-[#263e68] text-slate-300 hover:bg-[#16274a] cursor-pointer"
                  >
                    {isEdit ? 'Cancel' : 'Back'}
                  </Button>
                  <Button
                    type="submit"
                    disabled={saving || !selectedClassId}
                    className="w-full sm:w-auto h-11 px-6 gap-2 bg-gradient-to-r from-blue-600 via-blue-500 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 text-white font-bold rounded-xl shadow-[0_0_20px_rgba(0,140,255,0.4)] disabled:opacity-50 cursor-pointer"
                  >
                    {saving ? (
                      <>
                        <Loader2 className="size-4 animate-spin" />
                        <span>Saving...</span>
                      </>
                    ) : (
                      <>
                        <Save className="size-4" />
                        <span>
                          {isEdit ? 'Save Changes' : 'Create Application'}
                        </span>
                      </>
                    )}
                  </Button>
                </div>
              </Card>
            </form>
          )}
        </>
      )}
    </section>
  )
}
