import { useState, useCallback, useMemo } from 'react'
import {
  ArrowLeft,
  Calendar,
  CalendarDays,
  Clock,
  CheckCircle2,
  AlertCircle,
  Eye,
  FileSignature,
  Car,
  DollarSign,
  ShieldAlert,
  Award,
  Check,
  Search,
  ChevronDown,
} from 'lucide-react'
import PageHero from '../common/PageHero'
import StatusBadge from '../common/StatusBadge'
import LoadingSpinner from '../common/LoadingSpinner'
import ErrorAlert from '../common/ErrorAlert'
import EmptyState from '../common/EmptyState'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '../ui/card'
import { Button } from '../ui/button'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import type {
  AppointmentAvailabilityResponse,
  ScheduleTestPreparation,
  TestAppointment,
} from '../../types/TestAppointment'
import {
  getLocalDrivingLicenseApplicationById,
  getLocalDrivingLicenseApplications,
} from '../../services/localDrivingLicenseApplicationService'
import {
  getSchedulePreparation,
  getAppointmentAvailability,
  scheduleTestAppointment,
  getAppointmentsByLocalAppId,
} from '../../services/testAppointmentService'
import {
  formatAppointmentSubmission,
  formatJordanAppointmentDateTime,
  getJordanDateString,
  formatAvailableDateLabel,
  APPROVED_APPOINTMENT_SLOTS,
} from '../../utils/jordanTime'
import { isAppointmentActive } from '../../utils/status'
import { useAsyncData } from '../../hooks'

export interface ScheduleAppointmentPageProps {
  accessToken: string
  initialLocalAppId?: number | null
  initialTestTypeId?: 1 | 2 | 3
  returnPage?: string
  onBack: () => void
  onNavigate?: (page: string) => void
  onShowAppointmentDetails?: (appointmentId: number) => void
}

const STAGE_CONFIG: Record<
  1 | 2 | 3,
  {
    id: 1 | 2 | 3
    name: string
    shortName: string
    icon: typeof Eye
    requiredPasses: number
    color: string
    bgColor: string
    borderColor: string
  }
> = {
  1: {
    id: 1,
    name: 'Vision Test',
    shortName: 'Vision',
    icon: Eye,
    requiredPasses: 0,
    color: 'text-cyan-400',
    bgColor: 'bg-cyan-500/10',
    borderColor: 'border-cyan-500/30',
  },
  2: {
    id: 2,
    name: 'Written (Theory) Test',
    shortName: 'Written',
    icon: FileSignature,
    requiredPasses: 1,
    color: 'text-indigo-400',
    bgColor: 'bg-indigo-500/10',
    borderColor: 'border-indigo-500/30',
  },
  3: {
    id: 3,
    name: 'Practical (Street) Test',
    shortName: 'Street',
    icon: Car,
    requiredPasses: 2,
    color: 'text-emerald-400',
    bgColor: 'bg-emerald-500/10',
    borderColor: 'border-emerald-500/30',
  },
}

export default function ScheduleAppointmentPage({
  accessToken,
  initialLocalAppId,
  initialTestTypeId = 1,
  returnPage = 'Local Applications',
  onBack,
  onNavigate,
  onShowAppointmentDetails,
}: ScheduleAppointmentPageProps) {
  // Navigation / Target Context
  const [selectedAppId, setSelectedAppId] = useState<number | null>(
    initialLocalAppId ?? null,
  )
  const [selectedTestTypeId, setSelectedTestTypeId] = useState<1 | 2 | 3>(
    initialTestTypeId ?? 1,
  )

  // Candidate Pagination & Search State (Scenario B)
  const [candidateSearchQuery, setCandidateSearchQuery] = useState('')
  const [candidatePage, setCandidatePage] = useState(1)
  const [hasMoreCandidates, setHasMoreCandidates] = useState(false)
  const [loadingMoreCandidates, setLoadingMoreCandidates] = useState(false)
  const [directLookupId, setDirectLookupId] = useState('')
  const [directLookupError, setDirectLookupError] = useState<string | null>(null)
  const [directLookupLoading, setDirectLookupLoading] = useState(false)
  const [loadedCandidates, setLoadedCandidates] = useState<
    LocalDrivingLicenseApplication[]
  >([])

  // Availability State
  const [selectedDate, setSelectedDate] = useState<string>('')
  const [selectedSlot, setSelectedSlot] = useState<string>('')

  // Submission State
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitSuccess, setSubmitSuccess] = useState(false)
  const [bookedDetails, setBookedDetails] = useState<{
    date: string
    time: string
    fee: number
    testName: string
    appId: number
    candidateName: string
  } | null>(null)

  // 1. Initial Candidates Fetch for Scenario B (Page 1)
  const appsListFetcher = useCallback(async (): Promise<LocalDrivingLicenseApplication[]> => {
    const res = await getLocalDrivingLicenseApplications(accessToken, 1, 50)
    setHasMoreCandidates(Boolean(res.hasNextPage))
    setCandidatePage(1)
    setLoadedCandidates(res.items ?? [])
    return res.items ?? []
  }, [accessToken])

  useAsyncData(appsListFetcher, {
    enabled: Boolean(accessToken && initialLocalAppId === undefined),
    initialData: [],
  })

  // Load more candidates (DEF-02 Pagination)
  const handleLoadMoreCandidates = async () => {
    if (loadingMoreCandidates || !hasMoreCandidates) return

    setLoadingMoreCandidates(true)
    try {
      const nextPage = candidatePage + 1
      const res = await getLocalDrivingLicenseApplications(
        accessToken,
        nextPage,
        50,
      )
      setCandidatePage(nextPage)
      setHasMoreCandidates(Boolean(res.hasNextPage))
      setLoadedCandidates((prev) => [...prev, ...(res.items ?? [])])
    } catch {
      // Keep existing items on failure
    } finally {
      setLoadingMoreCandidates(false)
    }
  }

  // Direct candidate lookup by application ID
  const handleDirectLookup = async (e: React.FormEvent) => {
    e.preventDefault()
    const id = Number(directLookupId.trim())
    if (!id || id <= 0) return

    setDirectLookupLoading(true)
    setDirectLookupError(null)

    try {
      const app = await getLocalDrivingLicenseApplicationById(accessToken, id)
      if (app) {
        setLoadedCandidates((prev) => {
          const exists = prev.some(
            (x) => x.localDrivingLicenseApplicationId === app.localDrivingLicenseApplicationId,
          )
          return exists ? prev : [app, ...prev]
        })
        setSelectedAppId(app.localDrivingLicenseApplicationId)
        setSelectedDate('')
        setSelectedSlot('')
        setSubmitSuccess(false)
        setSubmitError(null)
      } else {
        setDirectLookupError(`Application #${id} was not found.`)
      }
    } catch (err) {
      setDirectLookupError(
        err instanceof Error ? err.message : `Failed to load application #${id}.`,
      )
    } finally {
      setDirectLookupLoading(false)
    }
  }

  // 2. Selected Application Record
  const appFetcher = useCallback(async (): Promise<LocalDrivingLicenseApplication | null> => {
    if (selectedAppId === null) return null
    return getLocalDrivingLicenseApplicationById(accessToken, selectedAppId)
  }, [accessToken, selectedAppId])

  const {
    data: application = null,
    loading: loadingApp,
    error: appError,
    refresh: refreshApp,
  } = useAsyncData(appFetcher, {
    enabled: Boolean(accessToken && selectedAppId !== null),
    initialData: null,
    dependencies: [selectedAppId],
    defaultErrorMessage: `Failed to load application #${selectedAppId ?? ''}.`,
  })

  // 3. Preparation, Existing Appointments & Availability for Stage
  interface BookingContextData {
    schedulePrep: ScheduleTestPreparation | null
    existingAppointments: TestAppointment[]
    availability: AppointmentAvailabilityResponse | null
    availabilityError: string | null
  }

  const bookingContextFetcher = useCallback(async (): Promise<BookingContextData> => {
    if (selectedAppId === null) {
      return {
        schedulePrep: null,
        existingAppointments: [],
        availability: null,
        availabilityError: null,
      }
    }

    const startDate = getJordanDateString(new Date(), 2)
    const endDate = getJordanDateString(new Date(), 30)

    const [prepRes, apptsRes, availRes] = await Promise.allSettled([
      getSchedulePreparation(accessToken, selectedAppId, selectedTestTypeId),
      getAppointmentsByLocalAppId(accessToken, selectedAppId),
      getAppointmentAvailability(
        accessToken,
        selectedAppId,
        selectedTestTypeId,
        startDate,
        endDate,
      ),
    ])

    const prep = prepRes.status === 'fulfilled' ? prepRes.value : null
    const existing = apptsRes.status === 'fulfilled' ? apptsRes.value ?? [] : []

    let availData: AppointmentAvailabilityResponse | null = null
    let availError: string | null = null

    if (availRes.status === 'fulfilled') {
      availData = availRes.value
    } else {
      availError =
        availRes.reason instanceof Error
          ? availRes.reason.message
          : 'Could not load appointment availability from DVLD server.'
    }

    return {
      schedulePrep: prep,
      existingAppointments: existing,
      availability: availData,
      availabilityError: availError,
    }
  }, [accessToken, selectedAppId, selectedTestTypeId])

  const {
    data: bookingContext = {
      schedulePrep: null,
      existingAppointments: [],
      availability: null,
      availabilityError: null,
    },
    loading: loadingContext,
    refresh: refreshBookingContext,
  } = useAsyncData(bookingContextFetcher, {
    enabled: Boolean(accessToken && selectedAppId !== null),
    initialData: {
      schedulePrep: null,
      existingAppointments: [],
      availability: null,
      availabilityError: null,
    },
    dependencies: [selectedAppId, selectedTestTypeId],
    defaultErrorMessage: 'Could not load appointment availability from DVLD server.',
    onSuccess: (data) => {
      if (data.availability && data.availability.availableDays.length > 0) {
        setSelectedDate((prevDate) => {
          const dateExists = data.availability!.availableDays.some(
            (d) => d.date === prevDate,
          )
          const nextDate = dateExists
            ? prevDate
            : data.availability!.availableDays[0].date
          const chosenDay = data.availability!.availableDays.find(
            (d) => d.date === nextDate,
          )
          setSelectedSlot((prevSlot) => {
            const slotExists = chosenDay?.slots.some(
              (s) => s.startTime === prevSlot,
            )
            return slotExists ? prevSlot : chosenDay?.slots[0]?.startTime ?? ''
          })
          return nextDate
        })
      } else {
        setSelectedDate('')
        setSelectedSlot('')
      }
    },
  })

  const schedulePrep = bookingContext.schedulePrep
  const existingAppointments = bookingContext.existingAppointments
  const availability = bookingContext.availability
  const availabilityError = bookingContext.availabilityError
  const loadingAvailability = loadingContext
  const loadingPrep = loadingContext

  // Eligibility Evaluation
  const currentStageConfig = STAGE_CONFIG[selectedTestTypeId]
  const isNewStatus =
    (
      application?.statusText ||
      application?.applicationStatus ||
      ''
    ).toLowerCase() === 'new'
  const passedCount = application?.passedTest ?? 0
  const isPrerequisiteMet = passedCount >= currentStageConfig.requiredPasses
  const isStageAlreadyPassed =
    (selectedTestTypeId === 1 && passedCount >= 1) ||
    (selectedTestTypeId === 2 && passedCount >= 2) ||
    (selectedTestTypeId === 3 && passedCount >= 3)

  const activeAppointment = useMemo(() => {
    return existingAppointments
      .filter((a) => a.testTypeId === selectedTestTypeId)
      .find(isAppointmentActive)
  }, [existingAppointments, selectedTestTypeId])

  const hasActiveAppointment = Boolean(activeAppointment)

  const isEligibleToSchedule =
    Boolean(application) &&
    isNewStatus &&
    isPrerequisiteMet &&
    !isStageAlreadyPassed &&
    !hasActiveAppointment

  // Slot calculations for selected date
  const currentDaySlots = useMemo(() => {
    if (!availability || !selectedDate) return []
    return (
      availability.availableDays.find((d) => d.date === selectedDate)?.slots ??
      []
    )
  }, [availability, selectedDate])

  const totalFees = (schedulePrep?.fees ?? 0) + (schedulePrep?.retakerFees ?? 0)

  // Filtered candidate list for Scenario B
  const filteredCandidates = useMemo(() => {
    const query = candidateSearchQuery.trim().toLowerCase()
    if (!query) return loadedCandidates
    return loadedCandidates.filter((app) => {
      return (
        app.fullName?.toLowerCase().includes(query) ||
        app.nationalNo?.toLowerCase().includes(query) ||
        String(app.localDrivingLicenseApplicationId).includes(query) ||
        app.licenseClassName?.toLowerCase().includes(query)
      )
    })
  }, [loadedCandidates, candidateSearchQuery])

  // Booking Submission Handler
  const handleConfirmSchedule = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedAppId || !selectedDate || !selectedSlot || !isEligibleToSchedule)
      return

    setSubmitting(true)
    setSubmitError(null)

    try {
      const submissionDateTime = formatAppointmentSubmission(
        `${selectedDate}T${selectedSlot}`,
      )
      await scheduleTestAppointment(accessToken, {
        localDrivingLicenseApplicationId: selectedAppId,
        testTypeId: selectedTestTypeId,
        appointmentDate: submissionDateTime,
      })

      setBookedDetails({
        date: selectedDate,
        time: selectedSlot,
        fee: totalFees,
        testName: currentStageConfig.name,
        appId: selectedAppId,
        candidateName: application?.fullName || 'Candidate',
      })
      setSubmitSuccess(true)

      // Refresh availability and prep
      void refreshBookingContext()
    } catch (err) {
      setSubmitError(
        err instanceof Error
          ? err.message
          : 'Failed to schedule examination appointment. The requested slot may have just been reserved.',
      )
      void refreshBookingContext()
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="page-content space-y-6">
      {/* 1. Strictly Informational PageHero (Zero buttons or links inside) */}
      <PageHero
        pageId="Schedule Appointment"
        title="Schedule Test"
        highlight="Appointment"
        description="Reserve an official driving test slot in accordance with Jordan Licensing Department regulations and facility working hours."
      />

      {/* 2. Top Action and Navigation Bar (Outside PageHero) */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onBack}
          className="gap-2 text-slate-300 hover:text-white border-[#263e68] bg-[#0e1a33] hover:bg-[#16274a] rounded-xl cursor-pointer"
        >
          <ArrowLeft className="size-4" />
          <span>
            {returnPage === 'Test Appointments'
              ? 'Back to Test Appointments'
              : 'Back to Local Applications'}
          </span>
        </Button>

        <div className="flex items-center gap-2 text-xs text-slate-400 font-mono bg-[#0c1830] border border-[#1e3458] px-3 py-1.5 rounded-xl">
          <Clock className="size-3.5 text-cyan-400" />
          <span>Jordan Facility Time: Asia/Amman (+03:00)</span>
        </div>
      </div>

      {/* 3. Global Feedback Alerts */}
      {appError && (
        <ErrorAlert
          message={appError}
          onRetry={() => {
            void refreshApp()
          }}
          retryLabel="Retry Loading Application"
        />
      )}

      {submitError && (
        <ErrorAlert
          message={submitError}
          onRetry={() => setSubmitError(null)}
          retryLabel="Dismiss"
        />
      )}

      {submitSuccess && bookedDetails && (
        <div className="rounded-2xl border border-emerald-500/40 bg-emerald-950/30 p-5 space-y-3 shadow-lg animate-in fade-in">
          <div className="flex items-center gap-3 text-emerald-300 font-bold text-sm">
            <CheckCircle2 className="size-5 text-emerald-400 shrink-0" />
            <span>
              Appointment Successfully Scheduled for {bookedDetails.candidateName}!
            </span>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 text-xs text-slate-300 pt-1">
            <div className="bg-slate-900/80 p-2.5 rounded-xl border border-slate-800">
              <span className="text-slate-400 block text-[10px] uppercase">
                Examination Stage
              </span>
              <span className="font-semibold text-white">
                {bookedDetails.testName}
              </span>
            </div>
            <div className="bg-slate-900/80 p-2.5 rounded-xl border border-slate-800">
              <span className="text-slate-400 block text-[10px] uppercase">
                Date & Time (Asia/Amman)
              </span>
              <span className="font-semibold text-cyan-300 font-mono">
                {formatAvailableDateLabel(bookedDetails.date)} at {bookedDetails.time}
              </span>
            </div>
            <div className="bg-slate-900/80 p-2.5 rounded-xl border border-slate-800">
              <span className="text-slate-400 block text-[10px] uppercase">
                Applicable Fee
              </span>
              <span className="font-semibold text-emerald-400 font-mono">
                {bookedDetails.fee.toFixed(2)} JOD
              </span>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2.5 pt-2">
            <Button
              type="button"
              size="sm"
              onClick={onBack}
              className="bg-emerald-600 hover:bg-emerald-500 text-white font-semibold text-xs rounded-xl cursor-pointer"
            >
              Return to {returnPage}
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                setSubmitSuccess(false)
                setBookedDetails(null)
                void refreshBookingContext()
              }}
              className="text-xs text-slate-300 hover:text-white border-slate-700 hover:bg-slate-800 rounded-xl cursor-pointer"
            >
              Schedule Another Slot
            </Button>
            {onNavigate && returnPage !== 'Test Appointments' && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => onNavigate('Test Appointments')}
                className="text-xs text-cyan-300 hover:text-white border-cyan-700/60 hover:bg-cyan-950/40 rounded-xl cursor-pointer"
              >
                Go to Test Appointments
              </Button>
            )}
          </div>
        </div>
      )}

      {/* 4. Loading Application Spinner */}
      {loadingApp ? (
        <div className="flex items-center justify-center min-h-[300px]">
          <LoadingSpinner size="lg" message="Loading candidate application details…" />
        </div>
      ) : (
        /* 5. Main 2-Column Responsive Layout */
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
          {/* Left Column: Application Context, Examination Stage & Fee Breakdown */}
          <div className="lg:col-span-5 space-y-6">
            {/* Card 1: Applicant & Application Selection */}
            <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-md overflow-hidden">
              <CardHeader className="border-b border-[#1e3458] px-6 py-5">
                <div className="flex items-center justify-between gap-3">
                  <CardTitle className="text-base font-bold text-white flex items-center gap-2.5">
                    <CalendarDays className="size-4.5 text-cyan-400" />
                    <span>Candidate & Application</span>
                  </CardTitle>
                  {application ? (
                    <StatusBadge
                      variant={
                        isNewStatus
                          ? 'warning'
                          : application.applicationStatus === 'Completed'
                            ? 'success'
                            : 'neutral'
                      }
                      label={
                        application.statusText || application.applicationStatus || 'New'
                      }
                    />
                  ) : (
                    <span className="text-[11px] text-amber-300 font-medium px-2.5 py-0.5 rounded-full bg-amber-500/10 border border-amber-500/20">
                      Selection Required
                    </span>
                  )}
                </div>
                <CardDescription className="text-xs text-slate-400 mt-1">
                  Select or verify the local driving license application for this appointment.
                </CardDescription>
              </CardHeader>

              <CardContent className="p-6 space-y-4">
                {/* Scenario B Candidate Selection Tools (when initially unselected or user chooses to change) */}
                {initialLocalAppId === undefined && (
                  <div className="space-y-3.5 rounded-2xl bg-slate-950/60 p-4.5 border border-slate-800">
                    <div className="space-y-1.5">
                      <label
                        htmlFor="candidate-search"
                        className="block text-[11px] font-semibold uppercase tracking-wider text-slate-400"
                      >
                        Search Candidate (Name, National No, or App ID)
                      </label>
                      <div className="relative">
                        <Search className="size-4 absolute left-3.5 top-3 text-slate-500" />
                        <input
                          id="candidate-search"
                          type="text"
                          value={candidateSearchQuery}
                          onChange={(e) => setCandidateSearchQuery(e.target.value)}
                          placeholder="Type applicant name, national ID..."
                          className="w-full bg-[#111e33] border border-[#223656] text-xs text-white pl-9.5 pr-3 py-2 rounded-xl focus:outline-none focus:border-cyan-500"
                        />
                      </div>
                    </div>

                    {/* Candidate Select Dropdown with Status Indicators */}
                    <div className="space-y-1.5">
                      <label
                        htmlFor="select-application"
                        className="block text-[11px] font-semibold uppercase tracking-wider text-slate-400 mb-1"
                      >
                        Loaded Applications ({filteredCandidates.length})
                      </label>
                      <select
                        id="select-application"
                        value={selectedAppId ? String(selectedAppId) : ''}
                        onChange={(e) => {
                          const val = e.target.value
                          if (!val) {
                            setSelectedAppId(null)
                          } else {
                            setSelectedAppId(Number(val))
                          }
                          setSelectedDate('')
                          setSelectedSlot('')
                          setSubmitSuccess(false)
                          setSubmitError(null)
                        }}
                        className="w-full bg-[#111e33] border border-[#223656] text-xs text-white p-2.5 rounded-xl focus:outline-none focus:border-cyan-500 cursor-pointer"
                      >
                        <option value="">— Select an application —</option>
                        {filteredCandidates.map((app) => {
                          const status = (
                            app.statusText ||
                            app.applicationStatus ||
                            'New'
                          ).toLowerCase()
                          const isEligible = status === 'new'
                          return (
                            <option
                              key={app.localDrivingLicenseApplicationId}
                              value={app.localDrivingLicenseApplicationId}
                              className={
                                isEligible
                                  ? 'text-white bg-slate-900'
                                  : 'text-slate-400 bg-slate-950'
                              }
                            >
                              #{app.localDrivingLicenseApplicationId} — {app.fullName}{' '}
                              [{app.statusText || app.applicationStatus || 'New'}] (
                              {app.licenseClassName})
                            </option>
                          )
                        })}
                      </select>
                    </div>

                    {/* Load More Candidates Pagination Button */}
                    {hasMoreCandidates && (
                      <div className="pt-1">
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={handleLoadMoreCandidates}
                          disabled={loadingMoreCandidates}
                          className="w-full text-xs h-8 text-cyan-300 border-[#223656] hover:bg-[#14233c] rounded-xl"
                        >
                          {loadingMoreCandidates ? (
                            <span>Loading more candidates…</span>
                          ) : (
                            <span className="flex items-center gap-1.5 justify-center">
                              <ChevronDown className="size-3.5" />
                              <span>Load More Applications</span>
                            </span>
                          )}
                        </Button>
                      </div>
                    )}

                    {/* Direct App ID Lookup Form */}
                    <form onSubmit={handleDirectLookup} className="pt-3 border-t border-slate-800">
                      <span className="text-[10px] text-slate-400 block mb-1.5">
                        Or enter exact Application ID:
                      </span>
                      <div className="flex gap-2">
                        <input
                          type="number"
                          value={directLookupId}
                          onChange={(e) => setDirectLookupId(e.target.value)}
                          placeholder="App ID #"
                          className="w-32 bg-[#111e33] border border-[#223656] text-xs text-white px-3 py-1.5 rounded-xl focus:outline-none focus:border-cyan-500 font-mono"
                        />
                        <Button
                          type="submit"
                          size="sm"
                          disabled={directLookupLoading || !directLookupId.trim()}
                          className="h-8 text-xs bg-cyan-700 hover:bg-cyan-600 text-white rounded-xl px-3.5 cursor-pointer"
                        >
                          {directLookupLoading ? 'Finding…' : 'Lookup'}
                        </Button>
                      </div>
                      {directLookupError && (
                        <span className="text-[11px] text-rose-400 block pt-1.5">
                          {directLookupError}
                        </span>
                      )}
                    </form>
                  </div>
                )}

                {/* Selected Application Details */}
                {application ? (
                  <div className="p-4.5 rounded-xl bg-slate-950/40 border border-slate-800/80 space-y-2.5 text-xs">
                    <div className="flex items-center justify-between py-1.5 border-b border-slate-800/80">
                      <span className="text-slate-400">Applicant Full Name:</span>
                      <strong className="text-white font-medium">
                        {application.fullName}
                      </strong>
                    </div>
                    <div className="flex items-center justify-between py-1.5 border-b border-slate-800/80">
                      <span className="text-slate-400">National Number:</span>
                      <span className="font-mono text-slate-200">
                        {application.nationalNo}
                      </span>
                    </div>
                    <div className="flex items-center justify-between py-1.5 border-b border-slate-800/80">
                      <span className="text-slate-400">License Class:</span>
                      <span className="text-cyan-300 font-medium">
                        {application.licenseClassName ||
                          `Class ${application.licenseClassId}`}
                      </span>
                    </div>
                    <div className="flex items-center justify-between py-1.5">
                      <span className="text-slate-400">Examinations Passed:</span>
                      <span className="font-bold text-emerald-400 font-mono">
                        {application.passedTest} / 3 Stages
                      </span>
                    </div>
                  </div>
                ) : (
                  <div className="p-5 rounded-xl border border-dashed border-slate-700 text-center text-xs text-slate-400">
                    Please select an application from the dropdown above to load candidate details.
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Card 2: Examination Stage & Eligibility */}
            <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-md overflow-hidden">
              <CardHeader className="border-b border-[#1e3458] px-6 py-5">
                <div className="flex items-center justify-between gap-3">
                  <CardTitle className="text-base font-bold text-white flex items-center gap-2.5">
                    <Award className="size-4.5 text-cyan-400" />
                    <span>Examination Stage</span>
                  </CardTitle>
                  {schedulePrep && (
                    <span className="px-2.5 py-1 rounded-full text-[11px] font-mono font-semibold bg-cyan-950/80 text-cyan-300 border border-cyan-500/30">
                      Trial #{schedulePrep.trial}
                    </span>
                  )}
                </div>
                <CardDescription className="text-xs text-slate-400 mt-1">
                  Select the required examination stage in sequential order.
                </CardDescription>
              </CardHeader>

              <CardContent className="p-6 space-y-4">
                <div className="grid grid-cols-3 gap-3">
                  {([1, 2, 3] as const).map((stageId) => {
                    const cfg = STAGE_CONFIG[stageId]
                    const StageIcon = cfg.icon
                    const isSelected = selectedTestTypeId === stageId
                    const isPassed = passedCount >= stageId
                    const isPrereqMet = passedCount >= cfg.requiredPasses

                    return (
                      <button
                        key={stageId}
                        type="button"
                        onClick={() => {
                          setSelectedTestTypeId(stageId)
                          setSelectedDate('')
                          setSelectedSlot('')
                          setSubmitSuccess(false)
                          setSubmitError(null)
                        }}
                        className={`flex flex-col items-center justify-center p-3.5 rounded-xl border text-center transition-all cursor-pointer ${
                          isSelected
                            ? 'bg-cyan-600 text-white border-cyan-400 shadow-md ring-2 ring-cyan-400/40'
                            : isPassed
                              ? 'bg-emerald-950/20 text-emerald-300 border-emerald-500/30 hover:bg-emerald-950/40'
                              : isPrereqMet
                                ? 'bg-slate-900/80 text-slate-300 border-slate-700 hover:border-slate-500'
                                : 'bg-slate-950/60 text-slate-500 border-slate-800 opacity-60 cursor-not-allowed'
                        }`}
                      >
                        <StageIcon className="size-5 mb-1.5" />
                        <span className="text-xs font-bold leading-tight">
                          {cfg.shortName}
                        </span>
                        <span className="text-[10px] mt-1">
                          {isPassed ? (
                            <span className="text-emerald-400 font-semibold flex items-center gap-0.5">
                              <Check className="size-3" /> Passed
                            </span>
                          ) : !isPrereqMet ? (
                            'Prereq Req.'
                          ) : (
                            'Ready'
                          )}
                        </span>
                      </button>
                    )
                  })}
                </div>

                {/* Eligibility Feedback Alerts */}
                {application && !isNewStatus && (
                  <div className="rounded-xl border border-rose-500/30 bg-rose-500/10 p-3.5 text-xs text-rose-300 flex items-center gap-2.5">
                    <AlertCircle className="size-4.5 shrink-0" />
                    <span>
                      Application status is "{application.statusText || application.applicationStatus}". Only applications with status "New" can schedule examinations.
                    </span>
                  </div>
                )}

                {application && isStageAlreadyPassed && (
                  <div className="rounded-xl border border-emerald-500/30 bg-emerald-500/10 p-3.5 text-xs text-emerald-300 flex items-center gap-2.5">
                    <CheckCircle2 className="size-4.5 shrink-0" />
                    <span>
                      {currentStageConfig.name} has already been passed for this application.
                    </span>
                  </div>
                )}

                {application && !isPrerequisiteMet && (
                  <div className="rounded-xl border border-amber-500/30 bg-amber-500/10 p-3.5 text-xs text-amber-300 flex items-center gap-2.5">
                    <AlertCircle className="size-4.5 shrink-0" />
                    <span>
                      Sequential examination rule: Candidate must pass previous stage(s) before scheduling {currentStageConfig.name}.
                    </span>
                  </div>
                )}

                {hasActiveAppointment && activeAppointment && (
                  <div className="rounded-xl border border-cyan-500/30 bg-cyan-950/40 p-4 text-xs text-cyan-300 space-y-1.5">
                    <div className="flex items-center gap-2 font-bold">
                      <Clock className="size-4.5 shrink-0 text-cyan-400" />
                      <span>Active Appointment Already Scheduled</span>
                    </div>
                    <p className="text-[11px] text-slate-300 leading-relaxed">
                      Appointment #{activeAppointment.testAppointmentId} is scheduled on{' '}
                      {formatJordanAppointmentDateTime(
                        activeAppointment.appointmentDate,
                        activeAppointment.appointmentDateFormatted,
                      )}.
                    </p>
                    {onShowAppointmentDetails && (
                      <button
                        type="button"
                        onClick={() =>
                          onShowAppointmentDetails(activeAppointment.testAppointmentId)
                        }
                        className="text-[11px] font-semibold text-cyan-400 hover:underline pt-1 block cursor-pointer"
                      >
                        View Active Appointment Details →
                      </button>
                    )}
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Card 3: Fee Breakdown Card */}
            <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-md overflow-hidden">
              <CardHeader className="border-b border-[#1e3458] px-6 py-5">
                <CardTitle className="text-base font-bold text-white flex items-center gap-2.5">
                  <DollarSign className="size-4.5 text-emerald-400" />
                  <span>Applicable Fee Breakdown</span>
                </CardTitle>
                <CardDescription className="text-xs text-slate-400 mt-1">
                  Official fee schedule retrieved directly from DVLD backend.
                </CardDescription>
              </CardHeader>

              <CardContent className="p-6 space-y-3 text-xs">
                {loadingPrep ? (
                  <div className="flex items-center gap-2.5 py-4 text-slate-400">
                    <LoadingSpinner size="sm" />
                    <span>Retrieving official fee quote…</span>
                  </div>
                ) : !application ? (
                  <div className="text-xs text-slate-400 py-3 italic">
                    Fee quote will appear once an application is selected.
                  </div>
                ) : (
                  <>
                    <div className="flex items-center justify-between text-slate-300 py-1.5">
                      <span>Standard Examination Fee:</span>
                      <span className="font-mono font-semibold text-slate-100">
                        {(schedulePrep?.fees ?? 0).toFixed(2)} JOD
                      </span>
                    </div>

                    {Boolean(schedulePrep?.retakerFees) && (
                      <div className="flex items-center justify-between text-amber-300 py-1.5 border-t border-slate-800">
                        <span>Retake Application Fee (Trial #{schedulePrep?.trial}):</span>
                        <span className="font-mono font-semibold">
                          {(schedulePrep?.retakerFees ?? 0).toFixed(2)} JOD
                        </span>
                      </div>
                    )}

                    <div className="flex items-center justify-between py-3 border-t border-slate-700 font-bold text-sm">
                      <span className="text-white">Total Applicable Fee:</span>
                      <span className="text-cyan-400 font-mono text-base">
                        {totalFees.toFixed(2)} JOD
                      </span>
                    </div>
                  </>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Right Column: Date & Slot Availability Picker and Booking Confirmation */}
          <div className="lg:col-span-7 space-y-6">
            <form onSubmit={handleConfirmSchedule} className="space-y-6">
              {/* Card 4: Date & Slot Selection Card */}
              <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-md overflow-hidden">
                <CardHeader className="border-b border-[#1e3458] px-6 py-5">
                  <div className="flex items-center justify-between gap-3">
                    <CardTitle className="text-base font-bold text-white flex items-center gap-2.5">
                      <Calendar className="size-4.5 text-cyan-400" />
                      <span>Facility Available Dates & Slots</span>
                    </CardTitle>
                    {availability && (
                      <span className="text-[11px] text-cyan-300 font-mono font-semibold px-2.5 py-1 rounded-full bg-cyan-950/80 border border-cyan-500/30">
                        {availability.availableDays.length} working days open
                      </span>
                    )}
                  </div>
                  <CardDescription className="text-xs text-slate-400 mt-1">
                    Official Jordan facility calendar (Sunday–Thursday, 08:00–16:00). Minimum booking lead time is 2 calendar days.
                  </CardDescription>
                </CardHeader>

                <CardContent className="p-6 space-y-5">
                  {!application ? (
                    <EmptyState
                      message="No Application Selected"
                      description="Please select an eligible local driving license application on the left to inspect facility appointment availability."
                      icon={<Calendar className="size-8 text-slate-500" />}
                    />
                  ) : !isEligibleToSchedule ? (
                    <div className="rounded-xl border border-slate-800 bg-slate-950/70 p-6 text-center space-y-2.5">
                      <div className="flex items-center justify-center gap-2 text-sm font-bold text-amber-300">
                        <ShieldAlert className="size-5 shrink-0" />
                        <span>Examination Scheduling Locked</span>
                      </div>
                      <p className="text-xs text-slate-300 max-w-md mx-auto leading-relaxed">
                        {!isNewStatus
                          ? `Application status is "${application.statusText || application.applicationStatus}". Only New applications may be scheduled.`
                          : isStageAlreadyPassed
                            ? `${currentStageConfig.name} has already been passed.`
                            : !isPrerequisiteMet
                              ? `Prerequisite examination stage must be completed before ${currentStageConfig.name}.`
                              : hasActiveAppointment
                                ? `An active appointment is currently scheduled on ${formatJordanAppointmentDateTime(activeAppointment?.appointmentDate)}.`
                                : 'Please review application requirements.'}
                      </p>
                    </div>
                  ) : availabilityError ? (
                    <div className="rounded-xl border border-rose-500/30 bg-rose-500/10 p-4.5 space-y-2 text-xs text-rose-300">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2 font-semibold">
                          <AlertCircle className="size-4.5 shrink-0" />
                          <span>Availability Query: {availabilityError}</span>
                        </div>
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => void refreshBookingContext()}
                          className="h-8 text-xs border-rose-500/40 hover:bg-rose-500/20 rounded-lg cursor-pointer"
                        >
                          Retry
                        </Button>
                      </div>
                    </div>
                  ) : loadingAvailability ? (
                    <div className="flex flex-col items-center justify-center py-12 gap-3 text-cyan-300 text-xs">
                      <LoadingSpinner size="md" />
                      <span>Checking real-time facility availability (Asia/Amman)…</span>
                    </div>
                  ) : availability && availability.availableDays.length === 0 ? (
                    <EmptyState
                      message="No Open Slots"
                      description="There are currently no available 40-minute slots returned by the licensing department for this test stage in the next 30 calendar days."
                      icon={<Calendar className="size-8 text-amber-400" />}
                    />
                  ) : (
                    <>
                      {/* Available Dates: Interactive Calendar Cards Grid (DEF-04) */}
                      <div className="space-y-2.5">
                        <div className="flex items-center justify-between">
                          <label className="block text-xs font-semibold text-slate-300">
                            Available Facility Dates ({availability?.availableDays.length ?? 0} Working Days)
                          </label>
                          {selectedDate && (
                            <span className="text-[11px] font-mono text-cyan-300 font-semibold">
                              Selected: {formatAvailableDateLabel(selectedDate)}
                            </span>
                          )}
                        </div>

                        {/* Interactive Grid of Date Cards */}
                        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-2.5 max-h-56 overflow-y-auto pr-1">
                          {availability?.availableDays.map((d) => {
                            const isSelected = selectedDate === d.date
                            const [yyyy, mm, dd] = d.date.split('-')
                            const dateObj = new Date(Date.UTC(Number(yyyy), Number(mm) - 1, Number(dd)))
                            const weekdayStr = dateObj.toLocaleDateString('en-US', {
                              weekday: 'short',
                              timeZone: 'UTC',
                            })
                            const monthDayStr = dateObj.toLocaleDateString('en-US', {
                              month: 'short',
                              day: 'numeric',
                              timeZone: 'UTC',
                            })

                            return (
                              <button
                                key={d.date}
                                type="button"
                                onClick={() => {
                                  setSelectedDate(d.date)
                                  const day = availability.availableDays.find(
                                    (x) => x.date === d.date,
                                  )
                                  setSelectedSlot(day?.slots[0]?.startTime ?? '')
                                }}
                                className={`p-3 rounded-xl border text-left transition-all cursor-pointer flex flex-col justify-between ${
                                  isSelected
                                    ? 'bg-cyan-600 text-white border-cyan-400 shadow-md ring-2 ring-cyan-400/50'
                                    : 'bg-slate-900/90 text-slate-200 border-slate-700 hover:border-cyan-500/60 hover:bg-slate-850'
                                }`}
                              >
                                <div className="flex items-center justify-between w-full">
                                  <span
                                    className={`text-[11px] font-bold uppercase tracking-wider ${
                                      isSelected ? 'text-white' : 'text-cyan-400'
                                    }`}
                                  >
                                    {weekdayStr}
                                  </span>
                                  {isSelected && <Check className="size-3.5" />}
                                </div>
                                <div className="text-xs font-semibold mt-1">
                                  {monthDayStr}
                                </div>
                                <div
                                  className={`text-[10px] mt-1.5 font-mono ${
                                    isSelected ? 'text-cyan-100' : 'text-slate-400'
                                  }`}
                                >
                                  {d.slots.length} {d.slots.length === 1 ? 'slot' : 'slots'} open
                                </div>
                              </button>
                            )
                          })}
                        </div>
                      </div>

                      {/* 12 Predefined 40-minute Time Slots Grid */}
                      <div className="space-y-3 pt-3 border-t border-slate-800">
                        <div className="flex items-center justify-between">
                          <label className="text-xs font-semibold text-slate-300">
                            Available 40-Minute Time Slots for{' '}
                            <span className="text-cyan-300">
                              {formatAvailableDateLabel(selectedDate)}
                            </span>
                          </label>
                          <span className="text-[11px] text-slate-400 font-mono">
                            {currentDaySlots.length} open
                          </span>
                        </div>

                        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-2.5">
                          {APPROVED_APPOINTMENT_SLOTS.map((slotTime) => {
                            const isAvailable = currentDaySlots.some(
                              (s) => s.startTime === slotTime,
                            )
                            const isSelected = selectedSlot === slotTime
                            const slotObj = currentDaySlots.find(
                              (s) => s.startTime === slotTime,
                            )

                            return (
                              <button
                                key={slotTime}
                                type="button"
                                disabled={!isAvailable || !isEligibleToSchedule}
                                onClick={() => setSelectedSlot(slotTime)}
                                className={`px-3.5 py-2.5 rounded-xl text-xs font-mono font-semibold border transition-all text-center ${
                                  isSelected
                                    ? 'bg-cyan-600 text-white border-cyan-400 shadow-md ring-2 ring-cyan-400/50'
                                    : isAvailable && isEligibleToSchedule
                                      ? 'bg-slate-900/90 text-cyan-300 border-slate-700 hover:border-cyan-500 hover:bg-slate-800 cursor-pointer'
                                      : 'bg-slate-950/40 text-slate-600 border-slate-850 opacity-40 cursor-not-allowed'
                                }`}
                                title={
                                  slotObj
                                    ? `${slotObj.startTime} - ${slotObj.endTime} (Available)`
                                    : `${slotTime} (Unavailable or Booked)`
                                }
                              >
                                <div>{slotTime}</div>
                                <div className="text-[10px] font-sans font-normal opacity-80 mt-0.5">
                                  {isAvailable ? 'Open' : 'Unavailable'}
                                </div>
                              </button>
                            )
                          })}
                        </div>

                        {selectedSlot && (
                          <div className="mt-3 p-4 rounded-xl bg-cyan-950/40 border border-cyan-500/30 flex items-center justify-between text-xs text-cyan-300">
                            <div className="flex items-center gap-2.5">
                              <Clock className="size-4.5 text-cyan-400 shrink-0" />
                              <span>
                                Selected: <strong>{selectedSlot}</strong> –{' '}
                                {currentDaySlots.find(
                                  (s) => s.startTime === selectedSlot,
                                )?.endTime ?? ''}{' '}
                                on {formatAvailableDateLabel(selectedDate)}
                              </span>
                            </div>
                            <span className="font-mono text-[11px] text-cyan-400 uppercase font-semibold">
                              Asia/Amman
                            </span>
                          </div>
                        )}
                      </div>
                    </>
                  )}
                </CardContent>
              </Card>

              {/* Card 5: Booking Confirmation & Action Bar */}
              <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-md overflow-hidden">
                <CardContent className="p-6">
                  <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-4">
                    <div className="space-y-1">
                      <span className="text-[11px] font-bold uppercase tracking-wider text-slate-400 block">
                        Booking Summary
                      </span>
                      <div className="text-xs text-slate-200">
                        {application ? (
                          <>
                            <strong className="text-white">{application.fullName}</strong> •{' '}
                            {currentStageConfig.name} •{' '}
                            <span className="text-cyan-300 font-mono font-semibold">
                              {totalFees.toFixed(2)} JOD
                            </span>
                          </>
                        ) : (
                          'Select application to proceed'
                        )}
                      </div>
                    </div>

                    <div className="flex items-center gap-3 justify-end">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={onBack}
                        disabled={submitting}
                        className="h-10 px-5 rounded-xl border-slate-700 hover:bg-slate-800 text-slate-300 cursor-pointer"
                      >
                        Cancel
                      </Button>

                      <Button
                        type="submit"
                        disabled={
                          submitting ||
                          !isEligibleToSchedule ||
                          !selectedDate ||
                          !selectedSlot ||
                          loadingAvailability ||
                          Boolean(
                            availability && availability.availableDays.length === 0,
                          )
                        }
                        className="h-10 bg-gradient-to-r from-blue-600 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 text-white font-bold text-xs px-6 rounded-xl shadow-[0_0_15px_rgba(34,211,238,0.35)] cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        {submitting ? (
                          <div className="flex items-center gap-2">
                            <LoadingSpinner size="sm" />
                            <span>Reserving Slot…</span>
                          </div>
                        ) : (
                          <span>Confirm & Schedule Appointment</span>
                        )}
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </form>
          </div>
        </div>
      )}
    </section>
  )
}
