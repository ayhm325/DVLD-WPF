import { useMemo, useState } from "react";
import PeopleTable from "./PeopleTable";
import PageHero from "../common/PageHero";
import StatsGrid from "../common/StatsGrid";
import StatCard from "../common/StatCard";
import ErrorAlert from "../common/ErrorAlert";
import ConfirmModal from "../common/ConfirmModal";
import { Users as UsersIcon, User as UserIcon } from "lucide-react";
import type { Person } from "../../types/Person";
import { formatNumber } from "../../utils/formatters";
import { deletePerson } from "../../services/peopleService";
import { useAuth } from "../../context/useAuth";

export type PeoplePageProps = {
  accessToken?: string;
  people?: Person[];
  filteredPeople: Person[];
  searchName: string;
  onSearchChange: (value: string) => void;
  loadingPeople: boolean;
  peopleError: string;
  onRetryPeople: () => void;
  onAddPerson: () => void;
  onShowDetails?: (person: Person) => void;
  onEditPerson?: (person: Person) => void;
};

export default function PeoplePage({
  accessToken: propToken,
  people: rawPeople,
  filteredPeople,
  searchName,
  onSearchChange,
  loadingPeople,
  peopleError,
  onRetryPeople,
  onAddPerson,
  onShowDetails,
  onEditPerson,
}: PeoplePageProps) {
  const { accessToken: contextToken } = useAuth();
  const token = propToken || contextToken || "";

  const [deletePersonTarget, setDeletePersonTarget] = useState<Person | null>(
    null,
  );
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState("");
  const [deleteSuccess, setDeleteSuccess] = useState("");

  const people = useMemo(
    () => rawPeople ?? filteredPeople ?? [],
    [rawPeople, filteredPeople],
  );

  const totalPeople = people.length;
  const maleCount = useMemo(
    () => people.filter((p) => p.gender === 0).length,
    [people],
  );
  const femaleCount = useMemo(
    () => people.filter((p) => p.gender === 1).length,
    [people],
  );

  async function handleConfirmDelete() {
    if (!deletePersonTarget || isDeleting || !token) return;

    setIsDeleting(true);
    setDeleteError("");
    setDeleteSuccess("");

    try {
      await deletePerson(token, deletePersonTarget.personId);
      setDeleteSuccess(
        `Person #${deletePersonTarget.personId} (${deletePersonTarget.fullName}) was deleted successfully.`,
      );
      setDeletePersonTarget(null);
      onRetryPeople();
    } catch (err) {
      setDeleteError(
        err instanceof Error
          ? err.message
          : `Failed to delete person #${deletePersonTarget.personId}.`,
      );
    } finally {
      setIsDeleting(false);
    }
  }

  return (
    <section className="page-content">
      <PageHero pageId="People" />

      <StatsGrid columns={3}>
        <StatCard
          label="Total People"
          variant="blue"
          icon={<UsersIcon size={20} />}
          value={
            loadingPeople
              ? "..."
              : peopleError
                ? "—"
                : formatNumber(totalPeople)
          }
          description="Registered citizens"
        />

        <StatCard
          label="Male"
          variant="blue"
          icon={<UserIcon size={20} />}
          value={
            loadingPeople ? "..." : peopleError ? "—" : formatNumber(maleCount)
          }
          description="Registered male citizens"
        />

        <StatCard
          label="Female"
          variant="rose"
          icon={<UserIcon size={20} />}
          value={
            loadingPeople
              ? "..."
              : peopleError
                ? "—"
                : formatNumber(femaleCount)
          }
          description="Registered female citizens"
        />
      </StatsGrid>

      {peopleError && (
        <ErrorAlert
          message={peopleError}
          style={{ marginBottom: 20 }}
          onRetry={onRetryPeople}
          retryLabel="Retry people"
        />
      )}

      {deleteError && (
        <div style={{ marginBottom: 20 }}>
          <ErrorAlert
            message={deleteError}
            onDismiss={() => setDeleteError("")}
          />
        </div>
      )}

      {deleteSuccess && (
        <div style={{ marginBottom: 20 }}>
          <p
            role="status"
            className="text-emerald-400 font-semibold text-xs bg-emerald-950/40 border border-emerald-500/30 rounded-xl px-4 py-2.5 flex items-center justify-between"
          >
            <span>{deleteSuccess}</span>
            <button
              type="button"
              className="text-slate-400 hover:text-white cursor-pointer ml-3"
              onClick={() => setDeleteSuccess("")}
              aria-label="Dismiss message"
            >
              ✕
            </button>
          </p>
        </div>
      )}

      <PeopleTable
        people={people}
        searchName={searchName}
        onSearchChange={onSearchChange}
        onAddPerson={onAddPerson}
        onRefresh={onRetryPeople}
        loading={loadingPeople}
        onShowDetails={onShowDetails}
        onEditPerson={onEditPerson}
        onDeletePerson={(p) => {
          setDeleteError("");
          setDeleteSuccess("");
          setDeletePersonTarget(p);
        }}
      />

      {/* Delete Confirmation Modal */}
      <ConfirmModal
        isOpen={Boolean(deletePersonTarget)}
        title="Delete Person Record"
        message={
          deletePersonTarget
            ? `Are you sure you want to delete person #${deletePersonTarget.personId} — "${deletePersonTarget.fullName}" (National No: ${deletePersonTarget.nationalNo})? This action cannot be undone.`
            : ""
        }
        confirmText="Delete Person"
        cancelText="Cancel"
        confirmVariant="danger"
        loading={isDeleting}
        onConfirm={() => void handleConfirmDelete()}
        onCancel={() => {
          if (!isDeleting) {
            setDeletePersonTarget(null);
          }
        }}
      />
    </section>
  );
}
