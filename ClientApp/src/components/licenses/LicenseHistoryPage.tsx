import { useEffect, useState, useMemo } from 'react'
import {
  ArrowLeft,
  IdCard,
  Globe,
  Award,
  CheckCircle2,
  Calendar,
  Search,
} from 'lucide-react'
import type { License } from '../../types/License'
import type { InternationalLicense } from '../../types/InternationalLicense'
import type { PersonDetails } from '../../types/Person'
import type { Driver } from '../../types/Driver'
import { getPersonById } from '../../services/peopleService'
import { getLicensesByPersonId, getLicensesByDriverId } from '../../services/licenseService'
import { getInternationalLicensesByDriverId } from '../../services/internationalLicenseService'
import { getDrivers } from '../../services/driverService'
import { formatDate } from '../../utils/formatters'
import PageHero from '../common/PageHero'
import StatsGrid from '../common/StatsGrid'
import StatCard from '../common/StatCard'
import PersonPhoto from '../common/PersonPhoto'
import LoadingSpinner from '../common/LoadingSpinner'
import ErrorAlert from '../common/ErrorAlert'
import EmptyState from '../common/EmptyState'
import { Button } from '../ui/button'
import { Input } from '../ui/input'
import LocalLicenseCard from './LocalLicenseCard'
import InternationalLicenseCard from './InternationalLicenseCard'

export interface LicenseHistoryPageProps {
  accessToken: string
  personId?: number | null
  driver?: Driver | null
  onBack: () => void
}

type CategoryTab = 'local' | 'international'

export default function LicenseHistoryPage({
  accessToken,
  personId,
  driver: propDriver,
  onBack,
}: LicenseHistoryPageProps) {
  const [person, setPerson] = useState<PersonDetails | null>(null)
  const [resolvedDriver, setResolvedDriver] = useState<Driver | null>(propDriver ?? null)
  const [localLicenses, setLocalLicenses] = useState<License[]>([])
  const [internationalLicenses, setInternationalLicenses] = useState<InternationalLicense[]>([])
  const [loading, setLoading] = useState(Boolean(personId && accessToken))
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<CategoryTab>('local')
  const [searchQuery, setSearchQuery] = useState('')

  // Load comprehensive person details and licenses
  useEffect(() => {
    if (!personId || !accessToken) {
      return
    }

    let cancelled = false

    async function loadData() {
      setLoading(true)
      setError(null)

      try {
        // 1. Fetch Person Details
        const personData = await getPersonById(accessToken, personId!)
        if (cancelled) return
        setPerson(personData)

        // 2. Resolve Driver record if not provided via props
        let currentDriver = propDriver ?? null
        if (!currentDriver) {
          try {
            const allDrivers = await getDrivers(accessToken)
            if (!cancelled) {
              const matched = allDrivers.find((d) => d.personId === personId)
              if (matched) {
                currentDriver = matched
                setResolvedDriver(matched)
              }
            }
          } catch {
            // Optional driver lookup failure does not abort person info
          }
        }

        // 3. Fetch Local and International Licenses concurrently
        const localPromise = currentDriver
          ? getLicensesByDriverId(accessToken, currentDriver.driverId)
          : getLicensesByPersonId(accessToken, personId!)

        const intlPromise = currentDriver
          ? getInternationalLicensesByDriverId(accessToken, currentDriver.driverId)
          : Promise.resolve<InternationalLicense[]>([])

        const [localRes, intlRes] = await Promise.allSettled([localPromise, intlPromise])

        if (cancelled) return

        if (localRes.status === 'fulfilled') {
          setLocalLicenses(localRes.value || [])
        } else {
          setLocalLicenses([])
        }

        if (intlRes.status === 'fulfilled') {
          setInternationalLicenses(intlRes.value || [])
        } else {
          setInternationalLicenses([])
        }

        if (localRes.status === 'rejected' && intlRes.status === 'rejected') {
          setError('Failed to retrieve license records for this person.')
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : `Failed to load license records for Person #${personId}.`,
          )
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void loadData()

    return () => {
      cancelled = true
    }
  }, [accessToken, personId, propDriver])

  // Statistics
  const totalLicensesCount = localLicenses.length + internationalLicenses.length
  const activeLicensesCount =
    localLicenses.filter((l) => l.isActive).length +
    internationalLicenses.filter((l) => l.isActive).length

  // Filtered lists based on search input
  const filteredLocalLicenses = useMemo(() => {
    const q = searchQuery.trim().toLowerCase()
    if (!q) return localLicenses
    return localLicenses.filter(
      (l) =>
        String(l.licenseId).includes(q) ||
        String(l.applicationId).includes(q) ||
        (l.licenseClassName || '').toLowerCase().includes(q) ||
        (l.issueReasonText || '').toLowerCase().includes(q) ||
        (l.notes || '').toLowerCase().includes(q),
    )
  }, [localLicenses, searchQuery])

  const filteredInternationalLicenses = useMemo(() => {
    const q = searchQuery.trim().toLowerCase()
    if (!q) return internationalLicenses
    return internationalLicenses.filter(
      (intl) =>
        String(intl.internationalLicenseId).includes(q) ||
        String(intl.applicationId).includes(q) ||
        String(intl.issuedUsingLocalLicenseId).includes(q),
    )
  }, [internationalLicenses, searchQuery])

  // Handle case where personId is missing
  if (!personId) {
    return (
      <section className="page-content space-y-6">
        <PageHero
          pageId="Person License History"
          title="Person License"
          highlight="History"
          description="View and inspect all issued local and international driving license credentials."
        />

        <div className="flex items-center justify-between">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onBack}
            className="gap-2 text-slate-300 hover:text-white border-[#263e68] bg-[#0e1a33] hover:bg-[#16274a] rounded-xl cursor-pointer"
          >
            <ArrowLeft className="size-4" />
            <span>Back to Drivers Directory</span>
          </Button>
        </div>

        <EmptyState
          icon={<IdCard size={40} />}
          message="No person was selected. Please select a driver or person to view license history."
        />
      </section>
    )
  }

  return (
    <section className="page-content space-y-6">
      {/* Page Hero */}
      <PageHero
        pageId="Person License History"
        title="Person License"
        highlight="History"
        description={
          person
            ? `Comprehensive license portfolio and driving credentials for ${person.fullName} (National No: ${person.nationalNo}).`
            : 'Viewing official license history and permit credentials.'
        }
      />

      {/* Back button action bar */}
      <div className="flex items-center justify-between">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onBack}
          className="gap-2 text-slate-300 hover:text-white border-[#263e68] bg-[#0e1a33] hover:bg-[#16274a] rounded-xl cursor-pointer"
        >
          <ArrowLeft className="size-4" />
          <span>Back to Drivers Directory</span>
        </Button>
      </div>

      {/* Error state */}
      {error && (
        <ErrorAlert
          title="Could Not Load License History"
          message={error}
          onRetry={() => {
            setError(null)
            setLoading(true)
          }}
        />
      )}

      {/* Loading state */}
      {loading ? (
        <div className="py-16 flex flex-col items-center justify-center">
          <LoadingSpinner size="lg" message="Loading license history records..." layout="col" />
        </div>
      ) : person ? (
        <>
          {/* Person Profile Banner */}
          <div className="rounded-2xl border border-[#1e3458] bg-[#0c1930] p-6 sm:p-7 shadow-xl">
            <div className="flex flex-col sm:flex-row items-start sm:items-center gap-6">
              <PersonPhoto
                personId={person.personId}
                hasPhoto={Boolean(person.imagePath)}
                photoKey={person.imagePath}
                fullName={person.fullName}
                size="xl"
                accessToken={accessToken}
                className="border-2 border-cyan-400/50 shadow-[0_0_20px_rgba(34,211,238,0.25)] shrink-0"
              />

              <div className="flex-1 min-w-0 space-y-3">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="text-[11px] font-bold uppercase tracking-wider text-cyan-400">
                    License Holder Profile
                  </span>
                  <span className="font-mono text-xs font-semibold px-2.5 py-0.5 rounded-full bg-cyan-500/20 text-cyan-300 border border-cyan-500/30">
                    Person #{person.personId}
                  </span>
                  {resolvedDriver ? (
                    <span className="font-mono text-xs font-semibold px-2.5 py-0.5 rounded-full bg-blue-500/20 text-blue-300 border border-blue-500/30">
                      Driver #{resolvedDriver.driverId}
                    </span>
                  ) : null}
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

                <h2 className="text-2xl sm:text-3xl font-bold text-white tracking-tight">
                  {person.fullName}
                </h2>

                <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-xs text-slate-300 pt-1 border-t border-[#1a2d4c]">
                  <div>
                    <span className="text-slate-400">National No: </span>
                    <strong className="font-mono text-cyan-300 text-sm">{person.nationalNo}</strong>
                  </div>

                  <div className="flex items-center gap-1.5">
                    <Calendar className="size-3.5 text-cyan-400" />
                    <span className="text-slate-400">Date of Birth: </span>
                    <strong className="font-mono">{formatDate(person.dateOfBirth)}</strong>
                  </div>

                  {person.countryName && (
                    <div>
                      <span className="text-slate-400">Nationality: </span>
                      <strong className="text-slate-200">{person.countryName}</strong>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Summary Statistics Grid */}
          <StatsGrid columns={4}>
            <StatCard
              label="Total Licenses"
              variant="blue"
              icon={<Award size={20} />}
              value={totalLicensesCount}
              description="Local and international records"
            />

            <StatCard
              label="Local Licenses"
              variant="violet"
              icon={<IdCard size={20} />}
              value={localLicenses.length}
              description="Issued driving licenses"
            />

            <StatCard
              label="International Licenses"
              variant="purple"
              icon={<Globe size={20} />}
              value={internationalLicenses.length}
              description="International driving permits"
            />

            <StatCard
              label="Active Licenses"
              variant="emerald"
              icon={<CheckCircle2 size={20} />}
              value={activeLicensesCount}
              description="Currently valid & active"
            />
          </StatsGrid>

          {/* License Categories Panel */}
          <div className="panel space-y-6 p-6 rounded-2xl bg-[#0a1528] border border-[#1b3154]">
            {/* Header controls: Tabs and Search */}
            <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-4 border-b border-[#1b3154] pb-4">
              {/* Category tabs */}
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  data-tab="local"
                  onClick={() => {
                    setActiveTab('local')
                    setSearchQuery('')
                  }}
                  className={`flex items-center gap-2.5 px-4 py-2 text-xs font-semibold rounded-xl transition-all cursor-pointer ${
                    activeTab === 'local'
                      ? 'bg-gradient-to-r from-blue-600 to-cyan-500 text-white shadow-[0_0_15px_rgba(34,211,238,0.3)]'
                      : 'bg-[#0f1d35] text-slate-300 hover:text-white hover:bg-[#142646] border border-[#1e3458]'
                  }`}
                >
                  <IdCard className="size-4" />
                  <span>Local Licenses</span>
                  <span className={`px-2 py-0.5 text-[10px] font-mono font-bold rounded-full ${
                    activeTab === 'local' ? 'bg-black/30 text-white' : 'bg-[#1b2f52] text-cyan-300'
                  }`}>
                    {localLicenses.length}
                  </span>
                </button>

                <button
                  type="button"
                  data-tab="international"
                  onClick={() => {
                    setActiveTab('international')
                    setSearchQuery('')
                  }}
                  className={`flex items-center gap-2.5 px-4 py-2 text-xs font-semibold rounded-xl transition-all cursor-pointer ${
                    activeTab === 'international'
                      ? 'bg-gradient-to-r from-blue-600 to-cyan-500 text-white shadow-[0_0_15px_rgba(34,211,238,0.3)]'
                      : 'bg-[#0f1d35] text-slate-300 hover:text-white hover:bg-[#142646] border border-[#1e3458]'
                  }`}
                >
                  <Globe className="size-4" />
                  <span>International Licenses</span>
                  <span className={`px-2 py-0.5 text-[10px] font-mono font-bold rounded-full ${
                    activeTab === 'international' ? 'bg-black/30 text-white' : 'bg-[#1b2f52] text-cyan-300'
                  }`}>
                    {internationalLicenses.length}
                  </span>
                </button>
              </div>

              {/* In-tab search bar */}
              <div className="relative max-w-xs w-full">
                <Search className="size-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
                <Input
                  type="search"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder={
                    activeTab === 'local'
                      ? 'Filter by ID, class, reason...'
                      : 'Filter by ID, local ID, app...'
                  }
                  className="pl-9 h-9 text-xs bg-[#0b162c] border-[#1e3458] text-white rounded-xl placeholder:text-slate-500"
                />
              </div>
            </div>

            {/* Tab 1: Local Licenses Cards */}
            {activeTab === 'local' && (
              <div>
                {localLicenses.length === 0 ? (
                  <div className="py-12">
                    <EmptyState
                      icon={<IdCard size={40} />}
                      message="No local driving licenses found for this person."
                    />
                  </div>
                ) : filteredLocalLicenses.length === 0 ? (
                  <div className="py-8">
                    <EmptyState
                      icon={<Search size={32} />}
                      message="No local licenses match your search filter."
                    />
                  </div>
                ) : (
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    {filteredLocalLicenses.map((license) => (
                      <LocalLicenseCard
                        key={license.licenseId}
                        license={license}
                        person={person}
                        accessToken={accessToken}
                      />
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Tab 2: International Licenses Cards */}
            {activeTab === 'international' && (
              <div>
                {internationalLicenses.length === 0 ? (
                  <div className="py-12">
                    <EmptyState
                      icon={<Globe size={40} />}
                      message="No international driving permits issued for this driver."
                    />
                  </div>
                ) : filteredInternationalLicenses.length === 0 ? (
                  <div className="py-8">
                    <EmptyState
                      icon={<Search size={32} />}
                      message="No international permits match your search filter."
                    />
                  </div>
                ) : (
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    {filteredInternationalLicenses.map((intl) => (
                      <InternationalLicenseCard
                        key={intl.internationalLicenseId}
                        license={intl}
                        person={person}
                        accessToken={accessToken}
                      />
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        </>
      ) : null}
    </section>
  )
}
