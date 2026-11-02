import { useEffect, useState } from 'react'
import {
  ArrowLeft,
  Calendar,
  Globe,
  IdCard,
  Mail,
  MapPin,
  Maximize2,
  Pencil,
  Phone,
  ShieldCheck,
  User,
  UserCheck,
} from 'lucide-react'
import { getPersonById } from '../../services/peopleService'
import type { PersonDetails } from '../../types/Person'
import { formatDate } from '../../utils/formatters'
import PageHero from '../common/PageHero'
import ErrorAlert from '../common/ErrorAlert'
import LoadingSpinner from '../common/LoadingSpinner'
import PersonPhoto from '../common/PersonPhoto'
import ModalShell from '../common/ModalShell'
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card'
import { Button } from '../ui/button'

export type PersonDetailsPageProps = {
  accessToken: string
  personId: number | null
  onBack: () => void
  onEdit?: (personId: number) => void
}

export default function PersonDetailsPage({
  accessToken,
  personId,
  onBack,
  onEdit,
}: PersonDetailsPageProps) {
  const [person, setPerson] = useState<PersonDetails | null>(null)
  const [loading, setLoading] = useState(Boolean(personId))
  const [error, setError] = useState(!personId ? 'No person ID was specified.' : '')
  const [isPhotoPreviewOpen, setIsPhotoPreviewOpen] = useState(false)

  useEffect(() => {
    if (!personId) {
      return
    }

    let cancelled = false

    async function loadPerson() {
      try {
        setLoading(true)
        setError('')
        const data = await getPersonById(accessToken, personId!)
        if (!cancelled) {
          setPerson(data)
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : `Failed to load details for person #${personId}. Record may not exist.`,
          )
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void loadPerson()

    return () => {
      cancelled = true
    }
  }, [accessToken, personId])

  const fullName =
    person?.fullName ||
    (person
      ? `${person.firstName} ${person.secondName} ${person.thirdName ?? ''} ${person.lastName}`
          .replace(/\s+/g, ' ')
          .trim()
      : '')

  return (
    <section className="page-content space-y-6">
      {/* Page Hero Header with compact vertical spacing */}
      <PageHero
        pageId="People"
        title="Person"
        highlight="Details"
        className="py-4 sm:py-5"
        description={
          person
            ? `Comprehensive civil registry profile and verified demographic credentials for ${fullName}.`
            : personId
              ? `Viewing comprehensive civil registry records for Person #${personId}.`
              : 'Viewing person profile details in the DVLD central database.'
        }
      />

      {/* Action Toolbar */}
      <div className="flex items-center justify-between">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onBack}
          className="gap-2 text-slate-300 hover:text-white border-[#263e68] bg-[#0e1a33] hover:bg-[#16274a] rounded-xl cursor-pointer"
        >
          <ArrowLeft className="size-4" />
          <span>Back to People Directory</span>
        </Button>

        {person && onEdit && (
          <Button
            type="button"
            size="sm"
            onClick={() => onEdit(person.personId)}
            className="gap-2 text-sm font-bold text-white bg-gradient-to-r from-blue-600 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 border-none rounded-xl shadow-[0_0_15px_rgba(0,140,255,0.3)] cursor-pointer"
          >
            <Pencil className="size-4" />
            <span>Edit Person</span>
          </Button>
        )}
      </div>

      {/* Error notification banner */}
      {error && (
        <ErrorAlert
          title="Could Not Load Person"
          message={error}
          onDismiss={() => setError('')}
        />
      )}

      {/* Loading state */}
      {loading ? (
        <Card className="p-12 text-center border-[#1e3458] bg-[#0c1930]">
          <LoadingSpinner
            message={`Loading details for person #${personId ?? ''}...`}
            size="lg"
            layout="col"
          />
        </Card>
      ) : person ? (
        <div className="space-y-6">
          {/* Phase 2: Premium Profile Header Card */}
          <div className="relative overflow-hidden rounded-2xl border border-[#1e3458] bg-gradient-to-br from-[#0c1a33] via-[#09152b] to-[#060e1e] p-6 shadow-xl sm:p-8">
            {/* Ambient background glows */}
            <div className="pointer-events-none absolute -left-12 -top-12 size-64 rounded-full bg-cyan-500/10 blur-3xl" />
            <div className="pointer-events-none absolute right-0 top-0 size-72 rounded-full bg-blue-500/5 blur-3xl" />

            <div className="relative z-10 flex flex-col items-center gap-6 sm:flex-row sm:items-start md:gap-8">
              {/* Prominent Personal Photograph (approx. 208x240px desktop) */}
              <div className="group relative shrink-0">
                <div className="relative rounded-2xl border-2 border-cyan-400/40 bg-slate-950/70 p-1.5 shadow-[0_0_25px_rgba(34,211,238,0.18)] transition-all duration-300 group-hover:border-cyan-400/70 group-hover:shadow-[0_0_30px_rgba(34,211,238,0.3)]">
                  <PersonPhoto
                    personId={person.personId}
                    hasPhoto={Boolean(person.imagePath)}
                    photoKey={person.imagePath}
                    fullName={fullName}
                    size="2xl"
                    accessToken={accessToken}
                    onClick={() => setIsPhotoPreviewOpen(true)}
                    className="border border-slate-700/60 shadow-inner"
                  />

                  {/* Enlarge / inspect photograph trigger button */}
                  <button
                    type="button"
                    onClick={() => setIsPhotoPreviewOpen(true)}
                    className="absolute bottom-3 right-3 flex size-8 items-center justify-center rounded-lg border border-cyan-500/50 bg-slate-950/85 text-cyan-300 shadow-md backdrop-blur-xs transition-transform duration-200 hover:scale-110 hover:bg-slate-900 hover:text-white cursor-pointer"
                    title={person.imagePath ? 'Click to view full photograph' : 'Inspect profile avatar'}
                    aria-label={person.imagePath ? 'Enlarge personal photograph' : 'Inspect profile avatar'}
                  >
                    <Maximize2 className="size-4" />
                  </button>
                </div>

                <div className="mt-2 text-center text-[11px] font-medium tracking-wider text-slate-400">
                  {person.imagePath ? (
                    <span className="inline-flex items-center gap-1 text-cyan-400/90">
                      <span>Personal Photo</span>
                      <span className="text-slate-500">• Click to zoom</span>
                    </span>
                  ) : (
                    <span className="inline-flex items-center gap-1 text-slate-400">
                      <span>No Photo on Record</span>
                      <span className="text-slate-500">• Click to inspect</span>
                    </span>
                  )}
                </div>
              </div>

              {/* Identity Information Column */}
              <div className="flex flex-1 flex-col items-center text-center sm:items-start sm:text-left space-y-3.5 min-w-0">
                {/* Registry Badges */}
                <div className="flex flex-wrap items-center justify-center sm:justify-start gap-2">
                  <div className="inline-flex items-center gap-1.5 rounded-full border border-cyan-500/40 bg-cyan-950/70 px-3 py-1 text-xs font-bold uppercase tracking-wider text-cyan-300 shadow-[0_0_10px_rgba(34,211,238,0.15)]">
                    <ShieldCheck className="size-3.5 text-cyan-400" />
                    <span>Civil Registry Record</span>
                  </div>

                  <span className="font-mono text-xs font-semibold px-2.5 py-1 rounded-full bg-blue-500/15 text-blue-300 border border-blue-500/30">
                    ID #{person.personId}
                  </span>

                  <span
                    className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold ${
                      person.gender === 0
                        ? 'bg-blue-500/15 text-blue-300 border border-blue-500/30'
                        : 'bg-pink-500/15 text-pink-300 border border-pink-500/30'
                    }`}
                  >
                    <span
                      className={`size-1.5 rounded-full ${
                        person.gender === 0 ? 'bg-blue-400' : 'bg-pink-400'
                      }`}
                    />
                    <span>{person.gender === 0 ? 'Male' : 'Female'}</span>
                  </span>

                  <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-500/15 text-emerald-300 border border-emerald-500/30">
                    <Globe className="size-3.5 text-emerald-400" />
                    <span>{person.countryName || `Country #${person.nationalityCountryID}`}</span>
                  </span>
                </div>

                {/* Primary Heading: Full Name */}
                <h2
                  data-testid="person-full-name"
                  className="text-2xl font-extrabold tracking-tight text-white sm:text-3xl lg:text-4xl leading-tight"
                >
                  {fullName}
                </h2>

                {/* Identification & Vital Stats Row */}
                <div className="flex flex-wrap items-center justify-center sm:justify-start gap-3 pt-1">
                  <div className="flex items-center gap-2.5 rounded-xl border border-cyan-500/30 bg-slate-900/80 px-3.5 py-1.5 text-slate-300 shadow-xs">
                    <IdCard className="size-4 text-cyan-400" />
                    <span className="text-xs font-medium uppercase tracking-wider text-slate-400">
                      National No:
                    </span>
                    <strong className="font-mono text-base font-bold text-cyan-300 tracking-wider">
                      {person.nationalNo}
                    </strong>
                  </div>

                  <div className="flex items-center gap-2 rounded-xl border border-slate-800 bg-slate-900/60 px-3.5 py-1.5 text-xs text-slate-400">
                    <Calendar className="size-3.5 text-cyan-400" />
                    <span>Date of Birth:</span>
                    <span className="font-mono font-semibold text-slate-200">
                      {formatDate(person.dateOfBirth)}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Phase 3: Detailed Information Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Identity & Demographics */}
            <Card className="border-[#1e3458] bg-[#0c1930] shadow-md">
              <CardHeader className="flex-row items-center gap-3 pb-4 border-b border-[#1e3458]">
                <div className="flex size-10 items-center justify-center rounded-xl bg-blue-500/10 text-cyan-400 border border-blue-500/20 shadow-inner">
                  <UserCheck className="size-5" />
                </div>
                <div>
                  <CardTitle className="text-base font-bold text-white">
                    Legal Identity & Demographics
                  </CardTitle>
                  <p className="text-xs text-slate-400">
                    Verified four-part civil registry name breakdown
                  </p>
                </div>
              </CardHeader>
              <CardContent className="p-6 space-y-4">
                <div className="grid grid-cols-2 gap-3.5">
                  <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
                    <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                      First Name
                    </span>
                    <span className="text-sm font-bold text-white">{person.firstName}</span>
                  </div>

                  <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
                    <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                      Second Name
                    </span>
                    <span className="text-sm font-bold text-white">{person.secondName}</span>
                  </div>

                  <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
                    <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                      Third Name
                    </span>
                    <span className="text-sm font-semibold text-slate-200">
                      {person.thirdName || '—'}
                    </span>
                  </div>

                  <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
                    <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                      Last Name
                    </span>
                    <span className="text-sm font-bold text-white">{person.lastName}</span>
                  </div>
                </div>

                <div className="pt-3 border-t border-[#1e3458] grid grid-cols-2 gap-3.5">
                  <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
                    <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 flex items-center gap-1.5 mb-0.5">
                      <Calendar className="size-3.5 text-cyan-400" />
                      Date of Birth
                    </span>
                    <span className="text-sm font-semibold text-slate-200 font-mono">
                      {formatDate(person.dateOfBirth)}
                    </span>
                  </div>

                  <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
                    <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 flex items-center gap-1.5 mb-0.5">
                      <Globe className="size-3.5 text-cyan-400" />
                      Nationality
                    </span>
                    <span className="text-sm font-bold text-cyan-300">
                      {person.countryName || `Country #${person.nationalityCountryID}`}
                    </span>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Contact & Location Details */}
            <Card className="border-[#1e3458] bg-[#0c1930] shadow-md">
              <CardHeader className="flex-row items-center gap-3 pb-4 border-b border-[#1e3458]">
                <div className="flex size-10 items-center justify-center rounded-xl bg-cyan-500/10 text-cyan-400 border border-cyan-500/20 shadow-inner">
                  <Phone className="size-5" />
                </div>
                <div>
                  <CardTitle className="text-base font-bold text-white">
                    Contact & Residency
                  </CardTitle>
                  <p className="text-xs text-slate-400">Communication channels and registered address</p>
                </div>
              </CardHeader>
              <CardContent className="p-6 space-y-4">
                <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3.5">
                  <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 flex items-center gap-1.5 mb-1">
                    <Phone className="size-3.5 text-cyan-400" />
                    Phone Number
                  </span>
                  {person.phone ? (
                    <a
                      href={`tel:${person.phone}`}
                      className="text-sm font-bold text-cyan-300 hover:text-cyan-200 hover:underline font-mono inline-block"
                    >
                      {person.phone}
                    </a>
                  ) : (
                    <span className="text-sm text-slate-500">—</span>
                  )}
                </div>

                <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3.5">
                  <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 flex items-center gap-1.5 mb-1">
                    <Mail className="size-3.5 text-cyan-400" />
                    Email Address
                  </span>
                  {person.email ? (
                    <a
                      href={`mailto:${person.email}`}
                      className="text-sm font-bold text-cyan-300 hover:text-cyan-200 hover:underline break-all inline-block"
                    >
                      {person.email}
                    </a>
                  ) : (
                    <span className="text-sm text-slate-500">—</span>
                  )}
                </div>

                <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3.5">
                  <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 flex items-center gap-1.5 mb-1">
                    <MapPin className="size-3.5 text-cyan-400" />
                    Residential Address
                  </span>
                  <p className="text-sm text-slate-200 leading-relaxed">
                    {person.address || '—'}
                  </p>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Photo Zoom Lightbox Dialog */}
          <ModalShell
            isOpen={isPhotoPreviewOpen}
            onClose={() => setIsPhotoPreviewOpen(false)}
            title={
              <div className="flex items-center gap-2.5 text-white">
                <User className="size-5 text-cyan-400" />
                <span>Personal Photo — {fullName}</span>
              </div>
            }
            description={
              person.imagePath
                ? 'High-resolution personal photograph recorded in the civil registry.'
                : 'Civil registry identity record (no personal photo uploaded).'
            }
            size="xl"
          >
            <div className="flex flex-col items-center justify-center p-3 sm:p-5">
              {person.imagePath ? (
                <div className="relative overflow-hidden rounded-2xl border-2 border-cyan-500/40 bg-slate-950 p-2 shadow-[0_0_40px_rgba(34,211,238,0.25)]">
                  <PersonPhoto
                    personId={person.personId}
                    hasPhoto={true}
                    photoKey={person.imagePath}
                    fullName={fullName}
                    size="2xl"
                    accessToken={accessToken}
                    className="w-[280px] h-[330px] sm:w-[480px] sm:h-[560px] rounded-xl shadow-inner"
                  />
                </div>
              ) : (
                <div className="flex flex-col items-center justify-center gap-4 py-8 text-center">
                  <div className="relative overflow-hidden rounded-2xl border-2 border-slate-700/50 bg-slate-950/80 p-2 shadow-inner">
                    <PersonPhoto
                      personId={person.personId}
                      hasPhoto={false}
                      fullName={fullName}
                      size="2xl"
                      accessToken={accessToken}
                      className="w-[180px] h-[210px] sm:w-[208px] sm:h-[240px] rounded-xl"
                    />
                  </div>
                  <p className="text-sm text-slate-400 max-w-sm">
                    No personal photograph is currently registered for this person in the Central Civil Registry.
                  </p>
                </div>
              )}
            </div>
          </ModalShell>
        </div>
      ) : null}
    </section>
  )
}
