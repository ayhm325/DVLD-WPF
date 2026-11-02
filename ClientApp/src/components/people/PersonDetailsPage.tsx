import { useEffect, useState } from 'react'
import {
  ArrowLeft,
  Calendar,
  Globe,
  IdCard,
  Mail,
  MapPin,
  Pencil,
  Phone,
  User,
} from 'lucide-react'
import { getPersonById } from '../../services/peopleService'
import type { PersonDetails } from '../../types/Person'
import { formatDate } from '../../utils/formatters'
import PageHero from '../common/PageHero'
import ErrorAlert from '../common/ErrorAlert'
import LoadingSpinner from '../common/LoadingSpinner'
import PersonPhoto from '../common/PersonPhoto'
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

  return (
    <section className="page-content space-y-6">
      {/* Page Hero Header */}
      <PageHero
        pageId="People"
        title="Person"
        highlight="Details"
        description={
          personId
            ? `Viewing comprehensive civil registry records and demographic profile for Person #${personId}.`
            : 'Viewing person profile details in the DVLD central database.'
        }
      />

      {/* Back button toolbar */}
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
          {/* Identity Header Card */}
          <Card className="border-[#1e3458] bg-[#0c1930] p-6 sm:p-8">
            <div className="flex flex-col sm:flex-row items-start sm:items-center gap-6">
              <PersonPhoto
                personId={person.personId}
                hasPhoto={Boolean(person.imagePath)}
                photoKey={person.imagePath}
                fullName={person.fullName || person.firstName}
                size="lg"
                accessToken={accessToken}
                className="border-2 border-cyan-400/50 shadow-[0_0_20px_rgba(34,211,238,0.25)]"
              />

              <div className="flex-1 min-w-0 space-y-2">
                <div className="flex flex-wrap items-center gap-2.5">
                  <span className="text-xs font-bold uppercase tracking-wider text-cyan-400">
                    Civil Registry Record
                  </span>
                  <span className="font-mono text-xs font-semibold px-2.5 py-0.5 rounded-full bg-blue-500/20 text-blue-300 border border-blue-500/30">
                    ID #{person.personId}
                  </span>
                  <span
                    className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${
                      person.gender === 0
                        ? 'bg-blue-500/20 text-blue-300 border border-blue-500/30'
                        : 'bg-pink-500/20 text-pink-300 border border-pink-500/30'
                    }`}
                  >
                    {person.gender === 0 ? 'Male' : 'Female'}
                  </span>
                </div>

                <h2 className="text-2xl font-bold text-white tracking-tight">
                  {person.fullName || `${person.firstName} ${person.secondName} ${person.thirdName ?? ''} ${person.lastName}`.replace(/\s+/g, ' ').trim()}
                </h2>

                <p className="text-sm text-slate-400 flex items-center gap-2">
                  <IdCard className="size-4 text-cyan-400" />
                  <span>National No:</span>
                  <strong className="text-slate-200 font-mono text-base">{person.nationalNo}</strong>
                </p>
              </div>
            </div>
          </Card>

          {/* Detailed Information Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Identity & Demographics */}
            <Card className="border-[#1e3458] bg-[#0c1930]">
              <CardHeader className="flex-row items-center gap-3 pb-4 border-b border-[#1e3458]">
                <div className="flex size-9 items-center justify-center rounded-xl bg-blue-500/10 text-cyan-400 border border-blue-500/20">
                  <User className="size-4" />
                </div>
                <div>
                  <CardTitle className="text-base text-white">Full Name & Identity</CardTitle>
                  <p className="text-xs text-slate-400">Official legal identity details</p>
                </div>
              </CardHeader>
              <CardContent className="p-6 space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <span className="text-xs text-slate-400 block mb-1">First Name</span>
                    <span className="text-sm font-semibold text-white">{person.firstName}</span>
                  </div>
                  <div>
                    <span className="text-xs text-slate-400 block mb-1">Second Name</span>
                    <span className="text-sm font-semibold text-white">{person.secondName}</span>
                  </div>
                  <div>
                    <span className="text-xs text-slate-400 block mb-1">Third Name</span>
                    <span className="text-sm font-semibold text-slate-200">{person.thirdName || '—'}</span>
                  </div>
                  <div>
                    <span className="text-xs text-slate-400 block mb-1">Last Name</span>
                    <span className="text-sm font-semibold text-white">{person.lastName}</span>
                  </div>
                </div>

                <div className="pt-3 border-t border-[#1e3458] grid grid-cols-2 gap-4">
                  <div>
                    <span className="text-xs text-slate-400 flex items-center gap-1.5 mb-1">
                      <Calendar className="size-3.5 text-cyan-400" />
                      Date of Birth
                    </span>
                    <span className="text-sm font-semibold text-slate-200 font-mono">
                      {formatDate(person.dateOfBirth)}
                    </span>
                  </div>
                  <div>
                    <span className="text-xs text-slate-400 flex items-center gap-1.5 mb-1">
                      <Globe className="size-3.5 text-cyan-400" />
                      Nationality
                    </span>
                    <span className="text-sm font-semibold text-cyan-300">
                      {person.countryName || `Country #${person.nationalityCountryID}`}
                    </span>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Contact & Location Details */}
            <Card className="border-[#1e3458] bg-[#0c1930]">
              <CardHeader className="flex-row items-center gap-3 pb-4 border-b border-[#1e3458]">
                <div className="flex size-9 items-center justify-center rounded-xl bg-blue-500/10 text-cyan-400 border border-blue-500/20">
                  <Phone className="size-4" />
                </div>
                <div>
                  <CardTitle className="text-base text-white">Contact & Residency</CardTitle>
                  <p className="text-xs text-slate-400">Communication channels and address</p>
                </div>
              </CardHeader>
              <CardContent className="p-6 space-y-4">
                <div>
                  <span className="text-xs text-slate-400 flex items-center gap-1.5 mb-1">
                    <Phone className="size-3.5 text-cyan-400" />
                    Phone Number
                  </span>
                  {person.phone ? (
                    <a
                      href={`tel:${person.phone}`}
                      className="text-sm font-semibold text-cyan-300 hover:underline font-mono"
                    >
                      {person.phone}
                    </a>
                  ) : (
                    <span className="text-sm text-slate-500">—</span>
                  )}
                </div>

                <div>
                  <span className="text-xs text-slate-400 flex items-center gap-1.5 mb-1">
                    <Mail className="size-3.5 text-cyan-400" />
                    Email Address
                  </span>
                  {person.email ? (
                    <a
                      href={`mailto:${person.email}`}
                      className="text-sm font-semibold text-cyan-300 hover:underline break-all"
                    >
                      {person.email}
                    </a>
                  ) : (
                    <span className="text-sm text-slate-500">—</span>
                  )}
                </div>

                <div className="pt-3 border-t border-[#1e3458]">
                  <span className="text-xs text-slate-400 flex items-center gap-1.5 mb-1">
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
        </div>
      ) : null}
    </section>
  )
}
