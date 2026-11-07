import { useCallback, useEffect, useState } from "react";
import PeopleTable, { type GenderFilter, type SearchBy } from "./PeopleTable";
import PageHero from "../common/PageHero";
import StatsGrid from "../common/StatsGrid";
import StatCard from "../common/StatCard";
import ErrorAlert from "../common/ErrorAlert";
import ConfirmModal from "../common/ConfirmModal";
import { Users as UsersIcon, User as UserIcon } from "lucide-react";
import type { Person } from "../../types/Person";
import { formatNumber } from "../../utils/formatters";
import { useAuth } from "../../context/useAuth";
import { useAsyncData, usePagination, useDebounce } from "../../hooks";
import {
  deletePerson,
  getPeoplePage,
  getPeopleStatistics,
} from "../../services/peopleService";

const PAGE_SIZE = 10;

export type PeoplePageProps = {
  onAddPerson: () => void;
  onShowDetails?: (person: Person) => void;
  onEditPerson?: (person: Person) => void;
};

export default function PeoplePage({
  onAddPerson,
  onShowDetails,
  onEditPerson,
}: PeoplePageProps) {
  const { accessToken } = useAuth();

  const [deletePersonTarget, setDeletePersonTarget] = useState<Person | null>(
    null,
  );
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState("");
  const [deleteSuccess, setDeleteSuccess] = useState("");

  const [search, setSearch] = useState("");
  const [searchBy, setSearchBy] = useState<SearchBy>("all");
  const [genderFilter, setGenderFilter] = useState<GenderFilter>("all");

  const debouncedSearch = useDebounce(search, 300);

  const pagination = usePagination({
    pageSize: PAGE_SIZE,
    initialPage: 1,
  });

  const { resetPage } = pagination;

  useEffect(() => {
    resetPage();
  }, [debouncedSearch, searchBy, genderFilter, resetPage]);

  const fetcher = useCallback(() => {
    return getPeoplePage(
      accessToken ?? "",
      pagination.page,
      pagination.pageSize,
      debouncedSearch,
      searchBy,
      genderFilter,
    );
  }, [accessToken, pagination.page, pagination.pageSize, debouncedSearch, searchBy, genderFilter]);

  const {
    data: page,
    loading,
    error,
    refresh,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    dependencies: [pagination.page, pagination.pageSize, debouncedSearch, searchBy, genderFilter],
    defaultErrorMessage: "Could not load people directory.",
  });

  const statsFetcher = useCallback(() => {
    return getPeopleStatistics(accessToken ?? "");
  }, [accessToken]);

  const { data: stats, refresh: refreshStats } = useAsyncData(statsFetcher, {
    enabled: Boolean(accessToken),
    dependencies: [accessToken],
    defaultErrorMessage: "Could not load people statistics.",
  });

  const totalPeople = stats?.totalCount ?? page?.totalCount ?? 0;
  const maleCount = stats?.maleCount ?? 0;
  const femaleCount = stats?.femaleCount ?? 0;

  const handleResetFilters = useCallback(() => {
    setSearch("");
    setSearchBy("all");
    setGenderFilter("all");
    resetPage();
  }, [resetPage]);

  async function handleConfirmDelete() {
    if (!deletePersonTarget || isDeleting) return;

    setIsDeleting(true);
    setDeleteError("");
    setDeleteSuccess("");

    try {
      await deletePerson(accessToken ?? "", deletePersonTarget.personId);
      setDeleteSuccess(
        `Person #${deletePersonTarget.personId} (${deletePersonTarget.fullName}) was deleted successfully.`,
      );
      setDeletePersonTarget(null);
      void refresh();
      void refreshStats();
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
            loading
              ? "..."
              : error
                ? "—"
                : formatNumber(totalPeople)
          }
          description="Registered citizens"
        />

        <StatCard
          label="Male"
          variant="blue"
          icon={<UserIcon size={20} />}
          value={loading ? "..." : error ? "—" : formatNumber(maleCount)}
          description="Registered male citizens"
        />

        <StatCard
          label="Female"
          variant="rose"
          icon={<UserIcon size={20} />}
          value={
            loading
              ? "..."
              : error
                ? "—"
                : formatNumber(femaleCount)
          }
          description="Registered female citizens"
        />
      </StatsGrid>

      {error && (
        <ErrorAlert
          message={error}
          style={{ marginBottom: 20 }}
          onRetry={refresh}
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
        people={page?.items ?? []}
        totalCount={page?.totalCount ?? 0}
        pageNumber={page?.pageNumber ?? pagination.page}
        pageSize={pagination.pageSize}
        totalPages={page?.totalPages ?? 0}
        hasPreviousPage={Boolean(page?.hasPreviousPage)}
        hasNextPage={Boolean(page?.hasNextPage)}
        onPreviousPage={pagination.previousPage}
        onNextPage={pagination.nextPage}
        onPageSizeChange={pagination.setPageSize}
        search={search}
        onSearchChange={setSearch}
        searchBy={searchBy}
        onSearchByChange={setSearchBy}
        genderFilter={genderFilter}
        onGenderFilterChange={setGenderFilter}
        onResetFilters={handleResetFilters}
        onAddPerson={onAddPerson}
        onRefresh={() => {
          void refresh();
          void refreshStats();
        }}
        loading={loading}
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
