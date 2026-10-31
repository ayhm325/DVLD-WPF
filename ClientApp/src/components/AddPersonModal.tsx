
import { useEffect, useState } from 'react'
import type { FormEvent, ChangeEvent } from 'react'
import ModalShell from './common/ModalShell'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from './ui/select'

import { getCountries } from '../services/countryService'
import type { Country } from '../types/Country'
import { createPerson } from '../services/peopleService'

type AddPersonModalProps = {
  accessToken: string
  onClose: () => void
  onPersonAdded: () => void
}

export default function AddPersonModal({
  accessToken,
  onClose,
  onPersonAdded,
}: AddPersonModalProps) {
  const [countries, setCountries] = useState<Country[]>([])
  const [loadingCountries, setLoadingCountries] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [gender, setGender] = useState('0')
  const [nationalityCountryID, setNationalityCountryID] = useState('')
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imagePreview, setImagePreview] = useState('')

  // Load countries from the API
  useEffect(() => {
    let cancelled = false

    async function loadCountries() {
      try {
        setLoadingCountries(true)
        setError('')

        const result = await getCountries(accessToken)

        if (!cancelled) {
          setCountries(result)
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Failed to load countries.',
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

  // Create and clean up the image preview asynchronously
  useEffect(() => {
    if (!imageFile) {
      return
    }

    const reader = new FileReader()

    reader.onload = () => {
      if (typeof reader.result === 'string') {
        setImagePreview(reader.result)
      }
    }

    reader.onerror = () => {
      setError('Failed to preview the selected image.')
      setImagePreview('')
    }

    reader.readAsDataURL(imageFile)

    return () => {
      if (reader.readyState === FileReader.LOADING) {
        reader.abort()
      }
    }
  }, [imageFile])

  // Validate the selected image
  function handleImageChange(
    event: ChangeEvent<HTMLInputElement>,
  ) {
    const file = event.target.files?.[0]

    if (!file) return

    if (!file.type.startsWith('image/')) {
      setError('Please select a valid image file.')
      event.target.value = ''
      return
    }

    if (file.size > 5 * 1024 * 1024) {
      setError('Image size must not exceed 5 MB.')
      event.target.value = ''
      return
    }

    setError('')
    setImagePreview('')
    setImageFile(file)
  }

  // Remove the selected image
  function handleRemoveImage() {
    setImageFile(null)
    setImagePreview('')
    setError('')

    const imageInput = document.getElementById(
      'person-image',
    ) as HTMLInputElement | null

    if (imageInput) {
      imageInput.value = ''
    }
  }

  // Create a person through the API
  async function handleSubmit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault(); if (saving) return
    setError('')

    const form = event.currentTarget
    const formData = new FormData(form)

    const dateOfBirth = String(
      formData.get('dateOfBirth') ?? '',
    )

    const birthDate = new Date(`${dateOfBirth}T00:00:00`)
    const today = new Date()

    let age =
      today.getFullYear() - birthDate.getFullYear()

    const monthDifference =
      today.getMonth() - birthDate.getMonth()

    if (
      monthDifference < 0 ||
      (monthDifference === 0 &&
        today.getDate() < birthDate.getDate())
    ) {
      age--
    }

    if (!Number.isFinite(birthDate.getTime()) || age < 18) {
      setError('The person must be at least 18 years old.')
      return
    }

    const nationalNo = String(
      formData.get('nationalNo') ?? '',
    ).trim()

    const phone = String(
      formData.get('phone') ?? '',
    ).trim()

    if (!/^\d{10}$/.test(nationalNo)) {
      setError('National number must be exactly 10 digits.')
      return
    }

    if (!/^(077|078|079)\d{7}$/.test(phone)) {
      setError(
        'Phone number must start with 077, 078, or 079 and contain 10 digits.',
      )
      return
    }

    setSaving(true)

    if (!nationalityCountryID && !formData.get('nationalityCountryID')) {
      setError('Please select a nationality country.')
      setSaving(false)
      return
    }

    try {
      await createPerson(accessToken, {
        nationalNo,
        firstName: String(
          formData.get('firstName') ?? '',
        ).trim(),
        secondName: String(
          formData.get('secondName') ?? '',
        ).trim(),
        thirdName:
          String(formData.get('thirdName') ?? '').trim() ||
          null,
        lastName: String(
          formData.get('lastName') ?? '',
        ).trim(),
        dateOfBirth,
        gender: Number(gender || formData.get('gender') || 0),
        address: String(
          formData.get('address') ?? '',
        ).trim(),
        phone,
        email:
          String(formData.get('email') ?? '').trim() ||
          null,
        nationalityCountryID: Number(
          nationalityCountryID || formData.get('nationalityCountryID'),
        ),
        // The API currently receives a path, not the image file.
        imagePath: null,
      })

      onPersonAdded()
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Failed to create person.',
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <ModalShell
      isOpen
      onClose={onClose}
      preventClose={saving}
      size="xl"
      title="Add New Person"
      description="Enter the person's information below."
      className="max-h-[calc(100vh-48px)] overflow-y-auto"
    >
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}

      {loadingCountries ? (
        <p className="py-4 text-center text-slate-400">Loading countries...</p>
      ) : (
        <form
          className="person-form"
          onSubmit={handleSubmit}
        >
            {/* Personal photo */}
            <div className="person-photo-section">
              <div className="person-photo-preview">
                {imagePreview ? (
                  <img
                    src={imagePreview}
                    alt="Person preview"
                    className="person-photo-image"
                  />
                ) : (
                  <span className="person-photo-placeholder">
                    No Photo
                  </span>
                )}
              </div>

              <div className="person-photo-controls">
                <label htmlFor="person-image">
                  Personal Photo
                </label>

                <input
                  id="person-image"
                  name="image"
                  type="file"
                  accept="image/*"
                  onChange={handleImageChange}
                  disabled={saving}
                />

                <p>Optional · Maximum size: 5 MB</p>

                {imageFile && (
                  <button
                    type="button"
                    className="secondary-button"
                    onClick={handleRemoveImage}
                    disabled={saving}
                  >
                    Remove Photo
                  </button>
                )}
              </div>
            </div>

            {/* National number */}
            <label>
              National Number *
              <input
                name="nationalNo"
                required
                maxLength={10}
                inputMode="numeric"
                pattern="\d{10}"
                title="Enter exactly 10 digits."
              />
            </label>

            <div className="form-grid">
              <label>
                First Name *
                <input
                  name="firstName"
                  required
                  maxLength={50}
                />
              </label>

              <label>
                Second Name *
                <input
                  name="secondName"
                  required
                  maxLength={50}
                />
              </label>

              <label>
                Third Name
                <input name="thirdName" maxLength={50} />
              </label>

              <label>
                Last Name *
                <input
                  name="lastName"
                  required
                  maxLength={50}
                />
              </label>

              <label>
                Date of Birth *
                <input
                  name="dateOfBirth"
                  type="date"
                  required
                  max={new Date().toISOString().slice(0, 10)}
                  min={`${new Date().getFullYear() - 120}-01-01`}
                />
              </label>

              <label>
                Gender *
                <Select
                  value={gender}
                  onValueChange={setGender}
                  name="gender"
                >
                  <SelectTrigger className="w-full min-h-[42px] bg-[#162238] border-[#354a68] text-white">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="0">Male</SelectItem>
                    <SelectItem value="1">Female</SelectItem>
                  </SelectContent>
                </Select>
              </label>

              <label>
                Nationality *
                <Select
                  value={nationalityCountryID || undefined}
                  onValueChange={setNationalityCountryID}
                  name="nationalityCountryID"
                  required
                >
                  <SelectTrigger className="w-full min-h-[42px] bg-[#162238] border-[#354a68] text-white">
                    <SelectValue placeholder="Select a country" />
                  </SelectTrigger>
                  <SelectContent className="max-h-64">
                    {countries.map((country) => (
                      <SelectItem
                        key={country.countryId}
                        value={String(country.countryId)}
                      >
                        {country.countryName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </label>

              <label>
                Phone *
                <input
                  name="phone"
                  type="tel"
                  required
                  maxLength={10}
                  inputMode="numeric"
                  pattern="(077|078|079)\d{7}"
                  placeholder="07XXXXXXXX"
                  title="Enter a 10-digit Jordanian mobile number."
                />
              </label>

              <label className="form-full-width">
                Address *
                <input
                  name="address"
                  required
                  maxLength={200}
                />
              </label>

              <label className="form-full-width">
                Email
                <input
                  name="email"
                  type="email"
                  maxLength={100}
                />
              </label>
            </div>

            <div className="modal-actions">
              <button
                type="button"
                className="secondary-button"
                onClick={onClose}
                disabled={saving}
              >
                Cancel
              </button>

              <button
                type="submit"
                className="primary-button"
                disabled={saving || loadingCountries}
              >
                {saving ? 'Saving...' : 'Save Person'}
              </button>
            </div>
          </form>
        )}
    </ModalShell>
  )
}
