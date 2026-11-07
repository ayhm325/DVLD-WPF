import { useCallback, useEffect, useMemo, useState } from "react";
import { getDriversPage } from "../../services/driverService";
import type { Driver } from "../../types/Driver";
import { formatDate, formatNumber } from "../../utils/formatters";
import { useAsyncData, useDebounce, usePagination } from "../../hooks";
import PageHero from "../common/PageHero";
import StatsGrid from "../common/StatsGrid";
import StatCard from "../common/StatCard";
import TableToolbar from "../common/TableToolbar";
import ErrorAlert from "../common/ErrorAlert";
import EmptyState from "../common/EmptyState";
import DataTable, {
  type DataTableColumn,
  type DataTableRowAction,
} from "../common/DataTable";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../ui/select";
import {
  Car as DriversIcon,
  IdCard as LicenseIcon,
  Check as CheckIcon,
  UserRound,
  History,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";

export type DriversPageProps = {
  accessToken?: string;
  drivers?: Driver[];
  loading?: boolean;
  error?: string;
  onRetry?: () => void;
  onShowPersonInfo?: (personId: number) => void;
  onShowLicenseHistory?: (personId: number, driver?: Driver) => void;
};

type SearchBy = "all" | "driverId" | "personId" | "name" | "nationalNo";
type LicenseFilter = "all" | "active" | "none";

const PAGE_SIZE = 10;

const SEARCH_OPTIONS: Array<{ value: SearchBy; label: string }> = [
  { value: "all", label: "All Fields" },
  { value: "name", label: "Full Name" },
  { value: "nationalNo", label: "National No." },
  { value: "driverId", label: "Driver ID" },
  { value: "personId", label: "Person ID" },
];

const columns: DataTableColumn<Driver>[] = [
  {
    key: "driverId",
    header: "Driver ID",
    render: (driver) => (
      <span
        style={{
          fontFamily: "ui-monospace, monospace",
          color: "var(--dvld-muted)",
        }}
      >
        #{driver.driverId}
      </span>
    ),
  },
  {
    key: "personId",
    header: "Person ID",
    render: (driver) => (
      <span
        style={{
          fontFamily: "ui-monospace, monospace",
          color: "var(--dvld-muted)",
        }}
      >
        #{driver.personId}
      </span>
    ),
  },
  {
    key: "fullName",
    header: "Full Name",
    render: (driver) => (
      <div className="person-cell">
        <span className="person-avatar" aria-hidden="true">
          {driver.fullName ? driver.fullName.charAt(0).toUpperCase() : "D"}
        </span>
        <strong>{driver.fullName}</strong>
      </div>
    ),
  },
  {
    key: "nationalNo",
    header: "National No.",
    render: (driver) => (
      <span
        style={{
          fontVariantNumeric: "tabular-nums",
          letterSpacing: "0.3px",
          fontWeight: 600,
        }}
      >
        {driver.nationalNo}
      </span>
    ),
  },
  {
    key: "dateOfBirth",
    header: "Date of Birth",
    render: (driver) => formatDate(driver.dateOfBirth),
  },
  {
    key: "activeLicenses",
    header: "Active Licenses",
    render: (driver) => (
      <span
        className="record-count"
        style={{ padding: "2px 8px", fontSize: 11 }}
      >
        {driver.activeLicenses}
      </span>
    ),
  },
  {
    key: "createdDate",
    header: "Created Date",
    render: (driver) => formatDate(driver.createdDate),
  },
];

export default function DriversPage({
  accessToken,
  drivers: externalDrivers,
  loading: externalLoading,
  error: externalError,
  onRetry: externalOnRetry,
  onShowPersonInfo,
  onShowLicenseHistory,
}: DriversPageProps) {
  const isServerPaged = externalDrivers === undefined;

  const [search, setSearch] = useState("");
  const [searchBy, setSearchBy] = useState<SearchBy>("all");
  const [licenseFilter, setLicenseFilter] = useState<LicenseFilter>("all");

  const debouncedSearch = useDebounce(search, 300);

  const pagination = usePagination({
    pageSize: PAGE_SIZE,
    initialPage: 1,
  });

  const { resetPage } = pagination;

  useEffect(() => {
    resetPage();
  }, [debouncedSearch, searchBy, licenseFilter, resetPage]);

  const fetcher = useCallback(() => {
    if (!isServerPaged) {
      return Promise.resolve(null);
    }
    return getDriversPage(
      accessToken ?? "",
      pagination.page,
      PAGE_SIZE,
      debouncedSearch,
      searchBy,
      licenseFilter,
    );
  }, [
    isServerPaged,
    accessToken,
    pagination.page,
    debouncedSearch,
    searchBy,
    licenseFilter,
  ]);

  const {
    data: page,
    loading: internalLoading,
    error: internalError,
    refresh,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken && isServerPaged),
    dependencies: [
      pagination.page,
      debouncedSearch,
      searchBy,
      licenseFilter,
      isServerPaged,
      accessToken,
    ],
    defaultErrorMessage: "Failed to load drivers.",
  });

  const loading = externalLoading ?? (isServerPaged ? internalLoading : false);
  const error = externalError ?? (isServerPaged ? internalError : undefined);
  const onRetry =
    externalOnRetry ?? (isServerPaged ? () => void refresh() : () => {});

  const isFiltered = Boolean(search.trim() || licenseFilter !== "all");

  const handleResetFilters = useCallback(() => {
    setSearch("");
    setSearchBy("all");
    setLicenseFilter("all");
    resetPage();
  }, [resetPage]);

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case "driverId":
        return "Search by driver ID...";
      case "personId":
        return "Search by person ID...";
      case "name":
        return "Search by driver full name...";
      case "nationalNo":
        return "Search by national number...";
      default:
        return "Search by name, national no, driver ID...";
    }
  }, [searchBy]);

  const driversList = useMemo(() => {
    if (isServerPaged) {
      return page?.items ?? [];
    }

    const fallbackList = externalDrivers ?? [];
    return fallbackList.filter((driver) => {
      if (licenseFilter === "active" && (driver.activeLicenses || 0) <= 0) {
        return false;
      }
      if (licenseFilter === "none" && (driver.activeLicenses || 0) > 0) {
        return false;
      }

      if (!search.trim()) return true;
      const query = search.trim().toLowerCase();

      if (searchBy === "driverId") {
        return String(driver.driverId).includes(query);
      }
      if (searchBy === "personId") {
        return String(driver.personId).includes(query);
      }
      if (searchBy === "name") {
        return (driver.fullName ?? "").toLowerCase().includes(query);
      }
      if (searchBy === "nationalNo") {
        return (driver.nationalNo ?? "").toLowerCase().includes(query);
      }

      return [
        String(driver.driverId),
        String(driver.personId),
        driver.fullName,
        driver.nationalNo,
      ].some((val) => (val ?? "").toLowerCase().includes(query));
    });
  }, [isServerPaged, page?.items, externalDrivers, licenseFilter, search, searchBy]);

  const totalDrivers = isServerPaged
    ? (page?.totalCount ?? 0)
    : (externalDrivers?.length ?? 0);

  const totalActiveLicenses = useMemo(() => {
    return driversList.reduce(
      (total, driver) => total + (driver.activeLicenses || 0),
      0,
    );
  }, [driversList]);

  const driversWithLicenses = useMemo(() => {
    return driversList.filter((d) => (d.activeLicenses || 0) > 0).length;
  }, [driversList]);

  const getRowContextMenu = useCallback(
    (driver: Driver): DataTableRowAction<Driver>[] => [
      {
        key: "show-person-info",
        label: "Show Person Info",
        icon: <UserRound className="size-4" />,
        onClick: () => onShowPersonInfo?.(driver.personId),
      },
      {
        key: "show-license-history",
        label: "Show Person License History",
        icon: <History className="size-4" />,
        onClick: () => onShowLicenseHistory?.(driver.personId, driver),
      },
    ],
    [onShowPersonInfo, onShowLicenseHistory],
  );

  return (
    <section className="page-content">
      <PageHero pageId="Drivers" />

      <StatsGrid columns={3}>
        <StatCard
          label="Total Drivers"
          variant="blue"
          icon={<DriversIcon size={20} />}
          value={loading ? "..." : error ? "—" : formatNumber(totalDrivers)}
          description="Registered drivers"
        />

        <StatCard
          label="Active Licenses"
          variant="emerald"
          icon={<LicenseIcon size={20} />}
          value={
            loading ? "..." : error ? "—" : formatNumber(totalActiveLicenses)
          }
          description={
            isServerPaged
              ? "Across loaded drivers"
              : "Across listed drivers"
          }
        />

        <StatCard
          label="Licensed Drivers"
          variant="emerald"
          icon={<CheckIcon size={20} />}
          value={
            loading ? "..." : error ? "—" : formatNumber(driversWithLicenses)
          }
          description={
            isServerPaged
              ? "Holding active licenses on page"
              : "Holding active licenses"
          }
        />
      </StatsGrid>

      <div className="panel">
        <div className="panel-header">
          <div>
            <h2>Driver Directory</h2>
            <p>Browse and search registered drivers in the system.</p>
          </div>

          <span className="record-count">
            {loading
              ? "Loading..."
              : isServerPaged
                ? isFiltered
                  ? `${page?.totalCount ?? 0} of ${page?.totalCount ?? 0} Drivers`
                  : `${page?.totalCount ?? 0} Drivers`
                : driversList.length === (externalDrivers?.length ?? 0)
                  ? `${externalDrivers?.length ?? 0} Drivers`
                  : `${driversList.length} of ${externalDrivers?.length ?? 0} Drivers`}
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
          searchAriaLabel="Search drivers"
          filters={
            <div style={{ minWidth: 160 }} className="shrink-0">
              <Select
                value={licenseFilter}
                onValueChange={(val) =>
                  setLicenseFilter(val as LicenseFilter)
                }
              >
                <SelectTrigger
                  className="h-10 min-h-10 w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg"
                  aria-label="Filter by license status"
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Drivers</SelectItem>
                  <SelectItem value="active">Has Active License</SelectItem>
                  <SelectItem value="none">No Active License</SelectItem>
                </SelectContent>
              </Select>
            </div>
          }
          isFiltered={isFiltered}
          onReset={handleResetFilters}
          onRefresh={onRetry}
          refreshing={loading}
        />

        {loading && <p className="empty-state">Loading drivers...</p>}

        {!loading && error && (
          <div style={{ margin: "0 25px 20px" }}>
            <ErrorAlert
              message={error}
              onRetry={onRetry}
              retryLabel="Try again"
            />
          </div>
        )}

        {!error && (
          <DataTable<Driver>
            data={driversList}
            columns={columns}
            getRowKey={(driver) => driver.driverId}
            loading={loading}
            emptyState={
              <EmptyState
                icon={<DriversIcon size={36} />}
                message={
                  totalDrivers === 0
                    ? "No drivers found."
                    : "No drivers match your search filters."
                }
              />
            }
            minWidth={850}
            rowContextMenu={getRowContextMenu}
          />
        )}

        {isServerPaged && !error && (page?.totalCount ?? 0) > 0 && (
          <div
            className="panel-footer"
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              gap: 16,
              padding: "16px 24px",
              borderTop: "1px solid #1e304d",
            }}
          >
            <span className="text-xs text-slate-400">
              Showing{" "}
              <strong className="text-white">
                {((page?.pageNumber ?? 1) - 1) * PAGE_SIZE + 1}
              </strong>
              –
              <strong className="text-white">
                {Math.min(
                  (page?.pageNumber ?? 1) * PAGE_SIZE,
                  page?.totalCount ?? 0,
                )}
              </strong>{" "}
              of{" "}
              <strong className="text-white">
                {page?.totalCount ?? 0}
              </strong>{" "}
              drivers
            </span>

            <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <button
                type="button"
                className="secondary-button"
                style={{ minHeight: 34, padding: "6px 12px", fontSize: 11 }}
                disabled={!page?.hasPreviousPage || loading}
                onClick={pagination.previousPage}
              >
                <ChevronLeft className="size-3.5" />
                <span>Previous</span>
              </button>

              <span style={{ fontSize: 11, color: "var(--dvld-muted)" }}>
                Page{" "}
                <strong style={{ color: "var(--dvld-heading)" }}>
                  {page?.pageNumber ?? pagination.page}
                </strong>{" "}
                of{" "}
                <strong style={{ color: "var(--dvld-heading)" }}>
                  {page?.totalPages ?? 1}
                </strong>
              </span>

              <button
                type="button"
                className="secondary-button"
                style={{ minHeight: 34, padding: "6px 12px", fontSize: 11 }}
                disabled={!page?.hasNextPage || loading}
                onClick={pagination.nextPage}
              >
                <span>Next</span>
                <ChevronRight className="size-3.5" />
              </button>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
