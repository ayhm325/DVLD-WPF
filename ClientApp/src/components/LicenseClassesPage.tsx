import { useCallback, useMemo, useState } from "react";
import {
  getLicenseClasses,
  updateLicenseClass,
} from "../services/licenseClassService";
import type { LicenseClass } from "../types/LicenseClass";
import { useAsyncData, useTableFilters } from "../hooks";
import PageHero from "./common/PageHero";
import TableToolbar from "./common/TableToolbar";
import ErrorAlert from "./common/ErrorAlert";
import EmptyState from "./common/EmptyState";
import DataTableContainer from "./common/DataTableContainer";
import LicenseClassRow, {
  type LicenseClassEditForm,
} from "./licenseClasses/LicenseClassRow";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { IdCard as LicenseIcon } from "lucide-react";
import { canManageReferenceData } from "../utils/permissions";
import { useAuth } from "../context/useAuth";

type Props = {
  accessToken: string;
  userRole?: string | null;
};

type SearchBy = "all" | "name" | "id" | "desc";
type AgeFilter = "all" | "18" | "21";

interface LicenseClassFilters extends Record<string, unknown> {
  ageFilter: AgeFilter;
}

const SEARCH_OPTIONS: Array<{ value: SearchBy; label: string }> = [
  { value: "all", label: "All Fields" },
  { value: "name", label: "Class Name" },
  { value: "id", label: "Class ID" },
  { value: "desc", label: "Description" },
];

export default function LicenseClassesPage({ accessToken, userRole }: Props) {
  const { user } = useAuth();
  const effectiveRole = userRole ?? user?.role;
  const canEdit = canManageReferenceData("License Classes", effectiveRole);

  const [success, setSuccess] = useState("");
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<LicenseClassEditForm>({
    licenseClassName: "",
    licenseClassDescription: "",
    minAllowedAge: "",
    defaultValidityLength: "",
    licenseClassFees: "",
  });
  const [saving, setSaving] = useState(false);

  const fetcher = useCallback(
    () => getLicenseClasses(accessToken),
    [accessToken],
  );

  const {
    data: items,
    loading,
    error,
    refresh,
    setError,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    initialData: [],
    defaultErrorMessage: "Failed to load license classes.",
  });

  const {
    search,
    setSearch,
    searchField: searchBy,
    setSearchField: setSearchBy,
    filters,
    setFilter,
    filteredData: filteredItems,
    isFiltered,
    resetFilters: handleResetFilters,
  } = useTableFilters<LicenseClass, SearchBy, LicenseClassFilters>({
    data: items,
    initialSearchField: "all",
    initialFilters: { ageFilter: "all" },
    filterItem: (item, query, field, currentFilters) => {
      // 1. Age check
      if (currentFilters.ageFilter === "18" && item.minAllowedAge < 18)
        return false;
      if (currentFilters.ageFilter === "21" && item.minAllowedAge < 21)
        return false;

      // 2. Query check
      if (!query) return true;

      if (field === "name") {
        return item.licenseClassName.toLowerCase().includes(query);
      }
      if (field === "id") {
        return String(item.licenseClassId).includes(query);
      }
      if (field === "desc") {
        return item.licenseClassDescription.toLowerCase().includes(query);
      }

      // 'all'
      return (
        item.licenseClassName.toLowerCase().includes(query) ||
        String(item.licenseClassId).includes(query) ||
        item.licenseClassDescription.toLowerCase().includes(query)
      );
    },
  });

  const ageFilter = filters.ageFilter;

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case "name":
        return "Search by license class name...";
      case "id":
        return "Search by class ID...";
      case "desc":
        return "Search by description...";
      default:
        return "Search by name, ID, or description...";
    }
  }, [searchBy]);

  function startEditing(item: LicenseClass) {
    if (!canEdit) return;
    setEditingId(item.licenseClassId);
    setForm({
      licenseClassName: item.licenseClassName,
      licenseClassDescription: item.licenseClassDescription,
      minAllowedAge: String(item.minAllowedAge),
      defaultValidityLength: String(item.defaultValidityLength),
      licenseClassFees: String(item.licenseClassFees),
    });
    setError("");
    setSuccess("");
  }

  async function saveChanges() {
    if (!canEdit || editingId === null || saving) return;

    const name = form.licenseClassName.trim();
    const description = form.licenseClassDescription.trim();
    const minAge = Number(form.minAllowedAge);
    const validity = Number(form.defaultValidityLength);
    const fees = Number(form.licenseClassFees);

    if (!name) {
      setError("License class name is required.");
      return;
    }
    if (name.length > 100) {
      setError("License class name cannot exceed 100 characters.");
      return;
    }

    if (!description) {
      setError("License class description is required.");
      return;
    }
    if (description.length > 500) {
      setError("License class description cannot exceed 500 characters.");
      return;
    }

    if (
      form.minAllowedAge.trim() === "" ||
      !Number.isInteger(minAge) ||
      minAge <= 0 ||
      minAge > 100
    ) {
      setError(
        "Minimum allowed age must be a valid integer greater than zero.",
      );
      return;
    }

    if (
      form.defaultValidityLength.trim() === "" ||
      !Number.isInteger(validity) ||
      validity <= 0 ||
      validity > 100
    ) {
      setError(
        "Default validity length must be a valid integer greater than zero.",
      );
      return;
    }

    if (
      form.licenseClassFees.trim() === "" ||
      !Number.isFinite(fees) ||
      fees < 0
    ) {
      setError("Enter a valid fee greater than or equal to zero.");
      return;
    }

    setSaving(true);
    setError("");
    setSuccess("");

    try {
      await updateLicenseClass(accessToken, editingId, {
        licenseClassId: editingId,
        licenseClassName: name,
        licenseClassDescription: description,
        minAllowedAge: minAge,
        defaultValidityLength: validity,
        licenseClassFees: fees,
      });

      await refresh();
      setEditingId(null);
      setSuccess("License class updated successfully.");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to update license class.",
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="page-content">
      <PageHero pageId="License Classes" />

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>License Classes Directory</h2>
            <p>License classes registered in DVLD</p>
          </div>

          <span className="record-count">
            {loading
              ? "Loading..."
              : filteredItems.length === items.length
                ? `${items.length} Classes`
                : `${filteredItems.length} of ${items.length} Classes`}
          </span>
        </div>

        <TableToolbar
          searchField={searchBy}
          searchFieldOptions={SEARCH_OPTIONS}
          onSearchFieldChange={(val) => setSearchBy(val as SearchBy)}
          searchFieldMinWidth={140}
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={placeholderText}
          searchAriaLabel="Search license classes"
          filters={
            <div style={{ minWidth: 140 }} className="shrink-0">
              <Select
                value={ageFilter}
                onValueChange={(val) =>
                  setFilter("ageFilter", val as AgeFilter)
                }
              >
                <SelectTrigger
                  className="h-10 min-h-10 w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg"
                  aria-label="Filter by minimum age"
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Ages</SelectItem>
                  <SelectItem value="18">Min Age 18+</SelectItem>
                  <SelectItem value="21">Min Age 21+</SelectItem>
                </SelectContent>
              </Select>
            </div>
          }
          isFiltered={isFiltered}
          onReset={handleResetFilters}
          onRefresh={() => {
            setSuccess("");
            void refresh();
          }}
          refreshing={loading}
        />

        {error && (
          <div style={{ margin: "0 25px 20px" }}>
            <ErrorAlert
              message={error}
              onRetry={() => {
                setSuccess("");
                void refresh();
              }}
            />
          </div>
        )}

        {success && (
          <p
            role="status"
            style={{
              padding: "0 25px 20px",
              margin: 0,
              color: "#059669",
              fontWeight: 600,
              fontSize: 13,
            }}
          >
            {success}
          </p>
        )}

        {loading ? (
          <p className="empty-state">Loading license classes...</p>
        ) : (
          <DataTableContainer minWidth={840}>
            <table className="people-table">
              <thead>
                <tr>
                  <th className="w-16 col-compact">ID</th>
                  <th className="w-48 whitespace-nowrap">Class Name</th>
                  <th className="table-cell-description">Description</th>
                  <th className="w-28 whitespace-nowrap">Minimum Age</th>
                  <th className="w-32 whitespace-nowrap">Validity (Years)</th>
                  <th className="w-28 whitespace-nowrap">Fees</th>
                  {canEdit && (
                    <th className="w-32 whitespace-nowrap">Actions</th>
                  )}
                </tr>
              </thead>

              <tbody>
                {filteredItems.map((item) => (
                  <LicenseClassRow
                    key={item.licenseClassId}
                    item={item}
                    isEditing={canEdit && editingId === item.licenseClassId}
                    canEdit={canEdit}
                    editForm={form}
                    saving={saving}
                    onStartEdit={startEditing}
                    onFormChange={setForm}
                    onSave={() => void saveChanges()}
                    onCancel={() => {
                      setEditingId(null);
                      setError("");
                    }}
                  />
                ))}

                {filteredItems.length === 0 && (
                  <tr>
                    <td colSpan={canEdit ? 7 : 6}>
                      <EmptyState
                        icon={<LicenseIcon size={36} />}
                        message={
                          items.length === 0
                            ? "No license classes found."
                            : "No license classes match your search."
                        }
                      />
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </DataTableContainer>
        )}

        {!loading && !error && (
          <div className="panel-footer">
            Showing {filteredItems.length} of {items.length} license classes
          </div>
        )}
      </section>
    </section>
  );
}
