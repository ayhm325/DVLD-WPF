import { useState, useCallback, useMemo } from 'react'
import {
  ArrowLeft,
  CalendarDays,
  Clock,
  Search,
  ChevronDown,
} from 'lucide-react'
import PageHero from '../common/PageHero'
import StatusBadge from '../common/StatusBadge'
import LoadingSpinner from '../common/LoadingSpinner'
import ErrorAlert from '../common/ErrorAlert'
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
  getJordanDateString,
} from '../../utils/jordanTime'
import { isAppointmentActive } from '../../utils/status'
import { useAsyncData } from '../../hooks'
import AppointmentReceiptCard, {
  type BookedAppointmentDetails,
} from './AppointmentReceiptCard'
import AppointmentFeeSummary from './AppointmentFeeSummary'
import AppointmentStageStepper from './AppointmentStageStepper'
import AppointmentSlotPicker from './AppointmentSlotPicker'
import { STAGE_CONFIG } from './stageConfig'

export { STAGE_CONFIG } from './stageConfig'

export interface ScheduleAppointmentPageProps {
  accessToken: string
  initialLocalAppId?: number | null
  initialTestTypeId?: 1 | 2 | 3
  returnPage?: string
  onBack: () => void
  onNavigate?: (page: string) => void
  onShowAppointmentDetails?: (appointmentId: number) => void
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
  const [bookedDetails, setBookedDetails] = useState<BookedAppointmentDetails | null>(null)

  // 1. Initial Candidate Application Loader
  const candidateFetcher = useCallback(async (): Promise<LocalDrivingLicenseApplication[]> => {
    if (initialLocalAppId !== undefined && initialLocalAppId !== null) {
      const app = await getLocalDrivingLicenseApplicationById(
        accessToken,
        initialLocalAppId,
      )
      return app ? [app] : []
    }

    const res = await getLocalDrivingLicenseApplications(accessToken, 1, 50)
    setHasMoreCandidates(1 * 50 < res.totalCount)
    return res.items
  }, [accessToken, initialLocalAppId])

  useAsyncData(candidateFetcher, {
    enabled: Boolean(accessToken),
    initialData: [],
    dependencies: [accessToken, initialLocalAppId],
    defaultErrorMessage: 'Could not fetch candidate application records.',
    onSuccess: (data) => {
      setLoadedCandidates(data)
      if (initialLocalAppId) {
        setSelectedAppId(initialLocalAppId)
      } else if (data.length > 0 && selectedAppId === null) {
        const firstNew = data.find(
          (x) =>
            (x.statusText || x.applicationStatus || '').toLowerCase() === 'new',
        )
        if (firstNew) {
          setSelectedAppId(firstNew.localDrivingLicenseApplicationId)
        }
      }
    },
  })

  // Candidate Pagination Handlers (Scenario B)
  const handleLoadMoreCandidates = async () => {
    if (loadingMoreCandidates || !hasMoreCandidates) return
    setLoadingMoreCandidates(true)
    try {
      const nextPage = candidatePage + 1
      const res = await getLocalDrivingLicenseApplications(accessToken, nextPage, 50)
      setLoadedCandidates((prev) => [...prev, ...res.items])
      setCandidatePage(nextPage)
      setHasMoreCandidates(nextPage * 50 < res.totalCount)
    } catch {
      // Non-critical background pagination failure
    } finally {
      setLoadingMoreCandidates(false)
    }
  }

  const handleDirectLookup = async (e: React.FormEvent) => {
    e.preventDefault()
    const id = Number(directLookupId.trim())
    if (!id || isNaN(id)) return

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

  const handleSelectStage = (stageId: 1 | 2 | 3) => {
    setSelectedTestTypeId(stageId)
    setSelectedDate('')
    setSelectedSlot('')
    setSubmitSuccess(false)
    setSubmitError(null)
  }

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
      {/* 1. Strictly Informational PageHero */}
      <PageHero
        pageId="Schedule Appointment"
        title="Schedule Test"
        highlight="Appointment"
        description="Reserve an official driving test slot in accordance with Jordan Licensing Department regulations and facility working hours."
      />

      {/* 2. Top Action and Navigation Bar */}
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
        <AppointmentReceiptCard
          bookedDetails={bookedDetails}
          returnPage={returnPage}
          onBack={onBack}
          onScheduleAnother={() => {
            setSubmitSuccess(false)
            setBookedDetails(null)
            void refreshBookingContext()
          }}
          onNavigate={onNavigate}
        />
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
                {/* Scenario B Candidate Selection Tools */}
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

            {/* Card 2: Examination Stage Stepper */}
            <AppointmentStageStepper
              selectedTestTypeId={selectedTestTypeId}
              onSelectStage={handleSelectStage}
              passedCount={passedCount}
              trialNumber={schedulePrep?.trial}
              application={application}
              isNewStatus={isNewStatus}
              isStageAlreadyPassed={isStageAlreadyPassed}
              isPrerequisiteMet={isPrerequisiteMet}
              hasActiveAppointment={hasActiveAppointment}
              activeAppointment={activeAppointment}
              onShowAppointmentDetails={onShowAppointmentDetails}
            />

            {/* Card 3: Fee Breakdown Card */}
            <AppointmentFeeSummary
              loadingPrep={loadingPrep}
              hasApplication={Boolean(application)}
              standardFee={schedulePrep?.fees}
              retakeFee={schedulePrep?.retakerFees}
              trialNumber={schedulePrep?.trial}
              totalFees={totalFees}
            />
          </div>

          {/* Right Column: Date & Slot Availability Picker and Booking Confirmation */}
          <div className="lg:col-span-7 space-y-6">
            <form onSubmit={handleConfirmSchedule} className="space-y-6">
              {/* Card 4: Date & Slot Selection Card */}
              <AppointmentSlotPicker
                application={application}
                isEligibleToSchedule={isEligibleToSchedule}
                isNewStatus={isNewStatus}
                isStageAlreadyPassed={isStageAlreadyPassed}
                isPrerequisiteMet={isPrerequisiteMet}
                hasActiveAppointment={hasActiveAppointment}
                activeAppointment={activeAppointment}
                currentStageName={currentStageConfig.name}
                availability={availability}
                loadingAvailability={loadingAvailability}
                availabilityError={availabilityError}
                selectedDate={selectedDate}
                selectedSlot={selectedSlot}
                onSelectDate={(date) => setSelectedDate(date)}
                onSelectSlot={(slot) => setSelectedSlot(slot)}
                onRetryAvailability={() => void refreshBookingContext()}
              />

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
