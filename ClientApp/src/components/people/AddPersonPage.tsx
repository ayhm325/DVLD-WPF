import { useEffect, useState } from "react";
import { getCountries } from "../../services/countryService";
import type { Country } from "../../types/Country";
import { createPerson, uploadPersonPhoto } from "../../services/peopleService";
import PageHero from "../common/PageHero";
import LoadingSpinner from "../common/LoadingSpinner";
import { Card } from "../ui";
import PersonForm, { type PersonFormSubmitValues } from "./PersonForm";

export type AddPersonPageProps = {
  accessToken: string;
  onBack: () => void;
  onPersonAdded: () => void;
};

export default function AddPersonPage({
  accessToken,
  onBack,
  onPersonAdded,
}: AddPersonPageProps) {
  const [countries, setCountries] = useState<Country[]>([]);
  const [loadingCountries, setLoadingCountries] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  // Load countries registry
  useEffect(() => {
    let cancelled = false;

    async function loadCountries() {
      try {
        setLoadingCountries(true);
        setError("");
        const result = await getCountries(accessToken);
        if (!cancelled) {
          setCountries(result);
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : "Failed to load countries registry.",
          );
        }
      } finally {
        if (!cancelled) {
          setLoadingCountries(false);
        }
      }
    }

    void loadCountries();

    return () => {
      cancelled = true;
    };
  }, [accessToken]);

  // Submit person creation and photograph upload
  async function handleSubmit(values: PersonFormSubmitValues) {
    setSaving(true);
    setError("");

    try {
      const created = await createPerson(accessToken, {
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
      });

      if (values.photoFile && created?.personId) {
        try {
          await uploadPersonPhoto(
            accessToken,
            created.personId,
            values.photoFile,
          );
        } catch (uploadErr) {
          setError(
            uploadErr instanceof Error
              ? `Person record created successfully, but photograph upload failed: ${uploadErr.message}. You can upload the photo from the Edit page.`
              : "Person record created successfully, but photograph upload failed. You can upload the photo from the Edit page.",
          );
          setSaving(false);
          return;
        }
      }

      onPersonAdded();
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Failed to create person record. Please verify inputs and try again.",
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="page-content space-y-6">
      {/* Informational PageHero without interactive action buttons */}
      <PageHero
        pageId="Add Person"
        title="Add New"
        highlight="Person"
        description="Register a new citizen or resident record in the DVLD central database. Complete all required identity, contact, and residency fields."
      />

      {loadingCountries ? (
        <Card className="p-12 text-center border-[#1e3458] bg-[#0c1930]">
          <LoadingSpinner
            message="Loading nationality countries registry..."
            size="lg"
            layout="col"
          />
        </Card>
      ) : (
        <PersonForm
          mode="create"
          accessToken={accessToken}
          countries={countries}
          loadingCountries={loadingCountries}
          saving={saving}
          error={error}
          onClearError={() => setError("")}
          onSubmit={handleSubmit}
          onCancel={onBack}
        />
      )}
    </section>
  );
}
