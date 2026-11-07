import { useState, useEffect } from 'react'
import { AlertCircle, ArrowLeft } from 'lucide-react'
import { getCountries } from '../../services/countryService'
import type { Country } from '../../types/Country'
import {
  getPersonById,
  updatePerson,
  uploadPersonPhoto,
  deletePersonPhoto,
} from '../../services/peopleService'
import type { PersonDetails } from '../../types/Person'
import PageHero from '../common/PageHero'
import LoadingSpinner from '../common/LoadingSpinner'
import EmptyState from '../common/EmptyState'
import { Card } from '../ui/card'
import { Button } from '../ui/button'
import PersonForm, { type PersonFormSubmitValues } from './PersonForm'

export type EditPersonPageProps = {
  accessToken: string
  personId: number | null
  onBack: () => void
  onPersonUpdated: () => void
}

export default function EditPersonPage({
  accessToken,
  personId,
  onBack,
  onPersonUpdated,
}: EditPersonPageProps) {
  const [countries, setCountries] = useState<Country[]>([])
  const [loadingCountries, setLoadingCountries] = useState(true)
  const [loadingPerson, setLoadingPerson] = useState(Boolean(personId))
  const [person, setPerson] = useState<PersonDetails | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState(!personId ? 'No person ID was provided for editing.' : '')

  // Load countries registry
  useEffect(() => {
    let cancelled = false

    async function loadCountries() {
      try {
        setLoadingCountries(true)
        const result = await getCountries(accessToken)
        if (!cancelled) {
          setCountries(result)
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Failed to load nationality countries.',
          )
        }
      } finally {
        if (!cancelled) {
          setLoadingCountries(false)
        }
      }
    }

    void loadCountries()

    return () => {
      cancelled = true
    }
  }, [accessToken])

  // Load person details
  useEffect(() => {
    if (!personId) return

    let cancelled = false

    async function loadPerson() {
      try {
        setLoadingPerson(true)
        setError('')
        const p = await getPersonById(accessToken, personId!)
        if (cancelled) return
        setPerson(p)
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
          setLoadingPerson(false)
        }
      }
    }

    void loadPerson()

    return () => {
      cancelled = true
    }
  }, [accessToken, personId])

  // Submit person update and handle photo upload or deletion
  async function handleSubmit(values: PersonFormSubmitValues) {
    if (!personId) return

    setSaving(true)
    setError('')

    try {
      await updatePerson(accessToken, personId, {
        nationalNo: values.nationalNo,
        firstName: values.firstName,
        secondName: values.secondName,
        thirdName: values.thirdName,
        lastName: values.lastName,
        dateOfBirth: values.dateOfBirth,
        gender: values.gender,
        address: values.address,
        phone: values.phone,
        email: values.email,
        nationalityCountryID: values.nationalityCountryID,
        imagePath: null,
      })

      if (values.photoFile) {
        try {
          await uploadPersonPhoto(accessToken, personId, values.photoFile)
        } catch (photoErr) {
          setError(
            photoErr instanceof Error
              ? `Person details saved, but photograph upload failed: ${photoErr.message}.`
              : 'Person details saved, but photograph upload failed.',
          )
          setSaving(false)
          return
        }
      } else if (values.isPhotoRemoved && Boolean(person?.imagePath)) {
        try {
          await deletePersonPhoto(accessToken, personId)
        } catch (delErr) {
          setError(
            delErr instanceof Error
              ? `Person details saved, but photograph removal failed: ${delErr.message}.`
              : 'Person details saved, but photograph removal failed.',
          )
          setSaving(false)
          return
        }
      }

      onPersonUpdated()
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : `Failed to update person record #${personId}. Please verify inputs and try again.`,
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="page-content space-y-6">
      {/* Informational PageHero without interactive action buttons */}
      <PageHero
        pageId="Edit Person"
        title="Edit"
        highlight="Person"
        description={
          personId
            ? `Update identity, contact, and residency details for person #${personId}.`
            : 'Update person record in the DVLD central database.'
        }
      />

      {/* Dedicated top navigation bar outside of PageHero */}
      <div className="flex items-center">
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
      </div>

      {!personId || personId <= 0 ? (
        <Card className="p-12 text-center border-[#1e3458] bg-[#0c1930]">
          <EmptyState
            icon={<AlertCircle className="size-10 text-amber-400" />}
            message="No Person Specified for Editing"
            description="A valid person ID is required to edit personal details. Please return to the People directory and select a person."
          />
        </Card>
      ) : loadingCountries || loadingPerson ? (
        <Card className="p-12 text-center border-[#1e3458] bg-[#0c1930]">
          <LoadingSpinner
            message={
              loadingPerson
                ? `Loading details for person #${personId ?? ''}...`
                : 'Loading nationality countries registry...'
            }
            size="lg"
            layout="col"
          />
        </Card>
      ) : (
        <PersonForm
          mode="edit"
          personId={personId}
          accessToken={accessToken}
          countries={countries}
          loadingCountries={loadingCountries}
          initialValues={person ?? undefined}
          saving={saving}
          error={error}
          onClearError={() => setError('')}
          onSubmit={handleSubmit}
          onCancel={onBack}
        />
      )}
    </section>
  )
}
