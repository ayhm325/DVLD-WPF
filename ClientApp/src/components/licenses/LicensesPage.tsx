import { useCallback, useEffect, useMemo, useState } from "react";
import { getLicensesPage } from "../../services/licenseService";
import type { License } from "../../types/License";
import { formatDate, formatCurrency, formatNumber } from "../../utils/formatters";
import { useAsyncData, useDebounce, usePagination } from "../../hooks";
import PageHero from "../common/PageHero";
import StatsGrid from "../common/StatsGrid";
import StatCard from "../common/StatCard";
import TableToolbar from "../common/TableToolbar";
import StatusBadge from "../common/StatusBadge";
import { getActiveStatusProps } from "../../utils/status";
import ErrorAlert from "../common/ErrorAlert";
import EmptyState from "../common/EmptyState";
import PaginationControls from "../common/PaginationControls";
import DataTable, { type DataTableColumn } from "../common/DataTable";
import LicenseDetailsModal from "./LicenseDetailsModal";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../ui/select";
import {
  IdCard as LicenseIcon,
  Check as CheckIcon,
  Clock as ClockIcon,
  RotateCw,
  Copy,
  Lock,
} from "lucide-react";
import { Button } from "../ui";

type LicensesPageProps = {
  accessToken: string;
  onNavigate?: (page: string) => void;
};

type SearchBy = "all" | "licenseId" | "appId" | "driver" | "class";

const PAGE_SIZE = 10;

const SEARCH_OPTIONS: Array<{ value: SearchBy; label: string }> = [
  { value: "all", label: "All Fields" },
  { value: "licenseId", label: "License ID" },
  { value: "appId", label: "Application ID" },
  { value: "driver", label: "Driver Name" },
  { value: "class", label: "License Class" },
];

const columns: DataTableColumn<License>[] = [
  {
    key: "licenseId",
    header: "License ID",
    render: (license) => (
      <span
        style={{
          fontFamily: "ui-monospace, monospace",
          color: "var(--dvld-muted)",
        }}
      >
        #{license.licenseId}
      </span>
    ),
  },
  {
    key: "applicationId",
    header: "App ID",
    render: (license) => (
      <span
        style={{
          fontFamily: "ui-monospace, monospace",
          color: "var(--dvld-muted)",
        }}
      >
        #{license.applicationId}
      </span>
    ),
  },
  {
    key: "driverName",
    header: "Driver Name",
    render: (license) => (
      <strong style={{ color: "var(--dvld-heading)" }}>
        {license.driverName ?? "—"}
      </strong>
    ),
  },
  {
    key: "licenseClassName",
    header: "Class",
    accessor: (license) => license.licenseClassName ?? "—",
  },
  {
    key: "issueDate",
    header: "Issue Date",
    render: (license) => formatDate(license.issueDate),
  },
  {
    key: "expirationDate",
    header: "Expiration Date",
    render: (license) => formatDate(license.expirationDate),
  },
  {
    key: "paidFees",
    header: "Fees",
    render: (license) => formatCurrency(license.paidFees, "JOD"),
  },
  {
    key: "status",
    header: "Status",
    render: (license) => (
      <StatusBadge {...getActiveStatusProps(license.isActive)} />
    ),
  },
];

export default function LicensesPage({
  accessToken,
  onNavigate,
}: LicensesPageProps) {
  const [selectedLicenseForDetails, setSelectedLicenseForDetails] =
    useState<License | null>(null);

  const [search, setSearch] = useState("");
  const [searchBy, setSearchBy] = useState<SearchBy>("all");
  const [statusFilter, setStatusFilter] = useState("all");

  const debouncedSearch = useDebounce(search, 300);

  const pagination = usePagination({
    pageSize: PAGE_SIZE,
    initialPage: 1,
  });

  const { resetPage } = pagination;

  useEffect(() => {
    resetPage();
  }, [debouncedSearch, searchBy, statusFilter, resetPage]);

  const fetcher = useCallback(() => {
    return getLicensesPage(
      accessToken,
      pagination.page,
      pagination.pageSize,
      debouncedSearch,
      searchBy,
      statusFilter,
    );
  }, [accessToken, pagination.page, pagination.pageSize, debouncedSearch, searchBy, statusFilter]);

  const {
    data: page,
    loading,
    error,
    refresh,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    dependencies: [
      pagination.page,
      pagination.pageSize,
      debouncedSearch,
      searchBy,
      statusFilter,
      accessToken,
    ],
    defaultErrorMessage: "Failed to load licenses.",
  });

  const isFiltered = Boolean(search.trim() || statusFilter !== "all");

  const handleResetFilters = useCallback(() => {
    setSearch("");
    setSearchBy("all");
    setStatusFilter("all");
    resetPage();
  }, [resetPage]);

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case "licenseId":
        return "Search by license ID...";
      case "appId":
        return "Search by application ID...";
      case "driver":
        return "Search by driver name...";
      case "class":
        return "Search by license class...";
      default:
        return "Search by license ID, driver, or class...";
    }
  }, [searchBy]);

  const totalLicenses = page?.totalCount ?? 0;
  const licensesList = useMemo(() => page?.items ?? [], [page?.items]);

  const activeOnPage = useMemo(() => {
    return licensesList.filter((lic) => lic.isActive).length;
  }, [licensesList]);

  const inactiveOnPage = useMemo(() => {
    return licensesList.filter((lic) => !lic.isActive).length;
  }, [licensesList]);

  return (
    <section className="page-content">
      <PageHero pageId="Licenses" />

      <StatsGrid columns={3}>
        <StatCard
          label="Total Licenses"
          variant="violet"
          icon={<LicenseIcon size={20} />}
          value={loading ? "..." : error ? "—" : formatNumber(totalLicenses)}
          description="Registered driving licenses"
        />

        <StatCard
          label="Active on Page"
          variant="emerald"
          icon={<CheckIcon size={20} />}
          value={loading ? "..." : error ? "—" : formatNumber(activeOnPage)}
          description="Active licenses on current page"
        />

        <StatCard
          label="Inactive on Page"
          variant="slate"
          icon={<ClockIcon size={20} />}
          value={loading ? "..." : error ? "—" : formatNumber(inactiveOnPage)}
          description="Inactive / expired on current page"
        />
      </StatsGrid>

      <div className="panel">
        <div className="panel-header">
          <div>
            <h2>License Directory</h2>
            <p>View and search issued driving licenses.</p>
          </div>

          <div className="flex items-center gap-3">
            {onNavigate && (
              <div className="hidden sm:flex items-center gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => onNavigate("Renew License")}
                  className="h-8 text-xs font-semibold gap-1.5 bg-[#081326] border-[#1b2f52] text-cyan-300 hover:text-white cursor-pointer"
                >
                  <RotateCw className="size-3.5" />
                  <span>Renew</span>
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => onNavigate("Replace License")}
                  className="h-8 text-xs font-semibold gap-1.5 bg-[#081326] border-[#1b2f52] text-purple-300 hover:text-white cursor-pointer"
                >
                  <Copy className="size-3.5" />
                  <span>Replace</span>
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => onNavigate("Detained Licenses")}
                  className="h-8 text-xs font-semibold gap-1.5 bg-[#081326] border-[#1b2f52] text-rose-300 hover:text-white cursor-pointer"
                >
                  <Lock className="size-3.5" />
                  <span>Detained</span>
                </Button>
              </div>
            )}

            <span className="record-count">
              {loading
                ? "Loading..."
                : isFiltered
                  ? `${totalLicenses} of ${totalLicenses} Licenses`
                  : `${totalLicenses} Licenses`}
            </span>
          </div>
        </div>

        <TableToolbar
          searchField={searchBy}
          searchFieldOptions={SEARCH_OPTIONS}
          onSearchFieldChange={(val) => setSearchBy(val as SearchBy)}
          searchFieldMinWidth={140}
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={placeholderText}
          searchAriaLabel="Search licenses"
          filters={
            <div style={{ minWidth: 140 }} className="shrink-0">
              <Select
                value={statusFilter}
                onValueChange={(val) => setStatusFilter(val)}
              >
                <SelectTrigger
                  className="h-10 min-h-10 w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg"
                  aria-label="Filter by license status"
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All statuses</SelectItem>
                  <SelectItem value="active">Active</SelectItem>
                  <SelectItem value="inactive">Inactive</SelectItem>
                </SelectContent>
              </Select>
            </div>
          }
          isFiltered={isFiltered}
          onReset={handleResetFilters}
          onRefresh={() => void refresh()}
          refreshing={loading}
        />

        {loading && <p className="empty-state">Loading licenses...</p>}

        {!loading && error && (
          <div style={{ margin: "0 25px 20px" }}>
            <ErrorAlert
              message={error}
              onRetry={() => void refresh()}
              retryLabel="Try again"
            />
          </div>
        )}

        {!error && (
          <DataTable<License>
            data={licensesList}
            columns={columns}
            getRowKey={(license) => license.licenseId}
            loading={loading}
            emptyState={
              <EmptyState
                icon={<LicenseIcon size={36} />}
                message={
                  totalLicenses === 0
                    ? "No licenses found."
                    : "No licenses match your filters."
                }
              />
            }
            minWidth={900}
            rowContextMenu={(license) => [
              {
                key: "view-license-details",
                label: "View License Details",
                icon: <LicenseIcon className="size-4" />,
                onClick: () => setSelectedLicenseForDetails(license),
              },
            ]}
          />
        )}

        {!error && totalLicenses > 0 && (
          <PaginationControls
            pageNumber={page?.pageNumber ?? pagination.page}
            pageSize={pagination.pageSize}
            totalCount={totalLicenses}
            totalPages={page?.totalPages ?? 1}
            hasPreviousPage={Boolean(page?.hasPreviousPage)}
            hasNextPage={Boolean(page?.hasNextPage)}
            onPreviousPage={pagination.previousPage}
            onNextPage={pagination.nextPage}
            onPageSizeChange={pagination.setPageSize}
            loading={loading}
            itemName="licenses"
          />
        )}
      </div>

      {/* License Details Modal */}
      <LicenseDetailsModal
        isOpen={Boolean(selectedLicenseForDetails)}
        onClose={() => setSelectedLicenseForDetails(null)}
        accessToken={accessToken}
        target={
          selectedLicenseForDetails
            ? { type: "local", item: selectedLicenseForDetails }
            : null
        }
      />
    </section>
  );
}
