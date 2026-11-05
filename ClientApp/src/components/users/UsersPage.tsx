import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Users,
  ShieldCheck,
  UserCheck,
  UserCog,
  UserPlus,
  Eye,
  Pencil,
  Power,
  PowerOff,
  MoreHorizontal,
  Lock,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  X,
} from "lucide-react";
import PageHero from "../common/PageHero";
import StatsGrid from "../common/StatsGrid";
import StatCard from "../common/StatCard";
import TableToolbar from "../common/TableToolbar";
import DataTable, {
  type DataTableColumn,
  type DataTableRowAction,
  type SortDirection,
} from "../common/DataTable";
import EmptyState from "../common/EmptyState";
import PersonPhoto from "../common/PersonPhoto";
import StatusBadge from "../common/StatusBadge";
import ErrorAlert from "../common/ErrorAlert";
import ConfirmModal from "../common/ConfirmModal";
import AccessDenied from "../common/AccessDenied";
import { Button } from "../ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../ui/select";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "../ui/dropdown-menu";
import { useAuth } from "../../context/useAuth";
import { useAsyncData, usePagination } from "../../hooks";
import { getUsers, updateUser, type User } from "../../services/userService";
import { isPagePermitted } from "../../utils/permissions";
import { formatNumber } from "../../utils/formatters";
import AddUserModal from "./AddUserModal";
import EditUserModal from "./EditUserModal";
import UserDetailsModal from "./UserDetailsModal";
import {
  getDeactivationBlockReason,
  getRoleLabel,
  isAdminRole,
  isPrimaryAdminUser,
} from "./userValidation";

export interface UsersPageProps {
  accessToken: string;
  onNavigate: (page: string) => void;
}

type SearchField = "all" | "fullName" | "userName" | "userId" | "personId";
type RoleFilter = "all" | "admin" | "staff";
type StatusFilter = "all" | "active" | "inactive";

const SEARCH_FIELD_OPTIONS: { value: SearchField; label: string }[] = [
  { value: "all", label: "All Fields" },
  { value: "fullName", label: "Full Name" },
  { value: "userName", label: "Username" },
  { value: "userId", label: "User ID" },
  { value: "personId", label: "Person ID" },
];

export default function UsersPage({ accessToken, onNavigate }: UsersPageProps) {
  const { user: currentUser, updateUser: updateAuthUser } = useAuth();
  const isAuthorizedAdmin = isPagePermitted(
    "User Management",
    currentUser?.role,
  );

  // Search, Filters & Sorting state
  const [searchQuery, setSearchQuery] = useState("");
  const [searchField, setSearchField] = useState<SearchField>("all");
  const [roleFilter, setRoleFilter] = useState<RoleFilter>("all");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [sortColumn, setSortColumn] = useState<string>("userId");
  const [sortDirection, setSortDirection] = useState<SortDirection>("asc");

  // Modals & Action Feedback state
  const [isAddUserOpen, setIsAddUserOpen] = useState(false);
  const [detailsUserId, setDetailsUserId] = useState<number | null>(null);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [statusToggleTarget, setStatusToggleTarget] = useState<User | null>(
    null,
  );
  const [togglingStatus, setTogglingStatus] = useState(false);

  const [actionError, setActionError] = useState<string | null>(null);
  const [successBanner, setSuccessBanner] = useState<string | null>(null);

  const fetchUsers = useCallback(async () => {
    if (!accessToken || !isAuthorizedAdmin) {
      return [] as User[];
    }
    return getUsers(accessToken);
  }, [accessToken, isAuthorizedAdmin]);

  const {
    data: rawUsers,
    loading,
    error: loadError,
    refresh: refreshUsers,
  } = useAsyncData(fetchUsers, {
    dependencies: [accessToken, isAuthorizedAdmin],
    enabled: Boolean(accessToken && isAuthorizedAdmin),
    defaultErrorMessage: "Failed to load system users.",
  });

  const users = useMemo(() => rawUsers ?? [], [rawUsers]);

  // Real statistics computed strictly from backend /api/Users records
  const totalUsersCount = users.length;
  const adminUsersCount = useMemo(
    () => users.filter((u) => isAdminRole(u.role)).length,
    [users],
  );
  const staffUsersCount = useMemo(
    () => users.filter((u) => !isAdminRole(u.role)).length,
    [users],
  );
  const activeUsersCount = useMemo(
    () => users.filter((u) => u.isActive).length,
    [users],
  );
  const inactiveUsersCount = totalUsersCount - activeUsersCount;
  const activeAdminCount = useMemo(
    () => users.filter((u) => isAdminRole(u.role) && u.isActive).length,
    [users],
  );

  const isFiltered = Boolean(
    searchQuery.trim() ||
    searchField !== "all" ||
    roleFilter !== "all" ||
    statusFilter !== "all",
  );

  const filteredAndSortedUsers = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();

    const filtered = users.filter((item) => {
      if (roleFilter === "admin" && !isAdminRole(item.role)) return false;
      if (roleFilter === "staff" && isAdminRole(item.role)) return false;

      if (statusFilter === "active" && !item.isActive) return false;
      if (statusFilter === "inactive" && item.isActive) return false;

      if (!q) return true;

      const fullName = (item.personFullName ?? "").toLowerCase();
      const uname = item.userName.toLowerCase();
      const uid = String(item.userId);
      const pid = String(item.personId);

      switch (searchField) {
        case "fullName":
          return fullName.includes(q);
        case "userName":
          return uname.includes(q);
        case "userId":
          return uid.includes(q);
        case "personId":
          return pid.includes(q);
        case "all":
        default:
          return (
            fullName.includes(q) ||
            uname.includes(q) ||
            uid.includes(q) ||
            pid.includes(q)
          );
      }
    });

    const sorted = [...filtered].sort((a, b) => {
      let cmp: number;
      switch (sortColumn) {
        case "fullName":
          cmp = (a.personFullName ?? "").localeCompare(b.personFullName ?? "");
          break;
        case "userName":
          cmp = a.userName.localeCompare(b.userName);
          break;
        case "role":
          cmp = a.role - b.role;
          break;
        case "status":
          cmp = Number(b.isActive) - Number(a.isActive);
          break;
        case "userId":
        default:
          cmp = a.userId - b.userId;
          break;
      }
      return sortDirection === "asc" ? cmp : -cmp;
    });

    return sorted;
  }, [
    users,
    searchQuery,
    searchField,
    roleFilter,
    statusFilter,
    sortColumn,
    sortDirection,
  ]);

  const pagination = usePagination<User>({
    data: filteredAndSortedUsers,
    pageSize: 10,
  });
  const { resetPage } = pagination;

  // Reset to page 1 whenever search or filters change
  useEffect(() => {
    resetPage();
  }, [searchQuery, searchField, roleFilter, statusFilter, resetPage]);

  const displayedUsers = pagination.paginatedData ?? filteredAndSortedUsers;

  function handleResetFilters() {
    setSearchQuery("");
    setSearchField("all");
    setRoleFilter("all");
    setStatusFilter("all");
  }

  function handleSort(columnKey: string, direction: SortDirection) {
    setSortColumn(columnKey);
    setSortDirection(direction);
  }

  const handleRequestToggleStatus = useCallback(
    async (target: User) => {
      setActionError(null);
      setSuccessBanner(null);

      if (target.isActive) {
        const blockReason = getDeactivationBlockReason(
          target,
          currentUser?.userId,
          activeAdminCount,
        );
        if (blockReason) {
          setActionError(blockReason);
          return;
        }
        // Require explicit confirmation before deactivating an active account
        setStatusToggleTarget(target);
        return;
      }

      // Activating an inactive account executes directly with clear feedback
      setTogglingStatus(true);
      try {
        await updateUser(accessToken, target.userId, {
          personId: target.personId,
          userName: target.userName,
          isActive: true,
        });
        setSuccessBanner(
          `Account @${target.userName} (#${target.userId}) has been activated.`,
        );
        refreshUsers();
      } catch (err) {
        setActionError(
          err instanceof Error
            ? err.message
            : `Failed to activate account @${target.userName}.`,
        );
      } finally {
        setTogglingStatus(false);
      }
    },
    [accessToken, activeAdminCount, currentUser?.userId, refreshUsers],
  );

  async function handleConfirmDeactivate() {
    if (!statusToggleTarget || togglingStatus) return;

    setTogglingStatus(true);
    setActionError(null);
    setSuccessBanner(null);

    try {
      await updateUser(accessToken, statusToggleTarget.userId, {
        personId: statusToggleTarget.personId,
        userName: statusToggleTarget.userName,
        isActive: false,
      });
      setSuccessBanner(
        `Account @${statusToggleTarget.userName} (#${statusToggleTarget.userId}) was deactivated. Any active sessions have been revoked.`,
      );
      setStatusToggleTarget(null);
      refreshUsers();
    } catch (err) {
      setActionError(
        err instanceof Error
          ? err.message
          : `Failed to deactivate account @${statusToggleTarget.userName}.`,
      );
      setStatusToggleTarget(null);
    } finally {
      setTogglingStatus(false);
    }
  }

  const getRowContextMenu = useCallback(
    (row: User): DataTableRowAction<User>[] => {
      const isPrimary = isPrimaryAdminUser(row);
      const blockReason = getDeactivationBlockReason(
        row,
        currentUser?.userId,
        activeAdminCount,
      );

      const actions: DataTableRowAction<User>[] = [
        {
          key: "view-details",
          label: "View Account Details",
          icon: <Eye className="size-4 text-cyan-400" />,
          onClick: (u: User) => setDetailsUserId(u.userId),
        },
        {
          key: "edit-user",
          label: isPrimary ? "Inspect Protected Account" : "Edit User Settings",
          icon: isPrimary ? (
            <Lock className="size-4 text-amber-400" />
          ) : (
            <Pencil className="size-4 text-blue-400" />
          ),
          separatorAfter: true,
          onClick: (u: User) => setEditingUser(u),
        },
      ];

      if (row.isActive) {
        actions.push({
          key: "deactivate-user",
          label: blockReason ? "Deactivation Protected" : "Deactivate Account",
          icon: <PowerOff className="size-4" />,
          danger: true,
          disabled: Boolean(blockReason) || togglingStatus,
          onClick: (u: User) => {
            void handleRequestToggleStatus(u);
          },
        });
      } else {
        actions.push({
          key: "activate-user",
          label: "Activate Account",
          icon: <Power className="size-4 text-emerald-400" />,
          disabled: togglingStatus,
          onClick: (u: User) => {
            void handleRequestToggleStatus(u);
          },
        });
      }

      return actions;
    },
    [
      activeAdminCount,
      currentUser?.userId,
      handleRequestToggleStatus,
      togglingStatus,
    ],
  );

  const columns: DataTableColumn<User>[] = useMemo(
    () => [
      {
        key: "userId",
        header: "User ID",
        width: 105,
        sortable: true,
        render: (row) => (
          <span className="font-mono font-bold text-slate-200">
            #{row.userId}
          </span>
        ),
      },
      {
        key: "fullName",
        header: "User / Linked Person",
        sortable: true,
        render: (row) => (
          <div className="flex items-center gap-3 min-w-52.5">
            <PersonPhoto
              personId={row.personId}
              hasPhoto={Boolean(row.imagePath)}
              photoKey={row.imagePath}
              fullName={row.personFullName}
              accessToken={accessToken}
              size="sm"
            />
            <div className="min-w-0">
              <p className="text-sm font-bold text-white truncate">
                {row.personFullName || "Unnamed Person"}
              </p>
              <p className="text-xs font-mono text-slate-400">
                Person #{row.personId}
              </p>
            </div>
          </div>
        ),
      },
      {
        key: "userName",
        header: "Username",
        sortable: true,
        render: (row) => {
          const isPrimary = isPrimaryAdminUser(row);
          const isSelf = row.userId === currentUser?.userId;

          return (
            <div className="flex flex-wrap items-center gap-1.5">
              <span className="font-mono text-xs font-semibold text-cyan-300 bg-[#0c1d36] border border-cyan-500/25 px-2.5 py-1 rounded-lg">
                @{row.userName}
              </span>
              {isPrimary && (
                <span
                  className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-500/15 text-amber-300 border border-amber-500/30"
                  title="System-protected primary administrator account"
                >
                  <Lock className="size-2.5" />
                  Primary
                </span>
              )}
              {isSelf && (
                <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold bg-blue-500/20 text-blue-200 border border-blue-400/30">
                  You
                </span>
              )}
            </div>
          );
        },
      },
      {
        key: "role",
        header: "Role",
        width: 140,
        sortable: true,
        render: (row) => {
          const admin = isAdminRole(row.role);
          return admin ? (
            <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold bg-violet-500/20 text-violet-200 border border-violet-400/40 shadow-[0_0_10px_rgba(139,92,246,0.2)]">
              <ShieldCheck className="size-3.5 text-violet-300" />
              Admin
            </span>
          ) : (
            <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold bg-slate-800/90 text-slate-200 border border-slate-600/60">
              <UserCog className="size-3.5 text-cyan-400" />
              Staff
            </span>
          );
        },
      },
      {
        key: "status",
        header: "Account Status",
        width: 145,
        sortable: true,
        render: (row) => (
          <StatusBadge
            variant={row.isActive ? "success" : "danger"}
            label={row.isActive ? "Active" : "Inactive"}
          />
        ),
      },
      {
        key: "actions",
        header: "Actions",
        align: "right",
        width: 150,
        render: (row) => {
          const isPrimary = isPrimaryAdminUser(row);
          const blockReason = getDeactivationBlockReason(
            row,
            currentUser?.userId,
            activeAdminCount,
          );

          return (
            <div className="inline-flex items-center justify-end gap-1.5">
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                onClick={() => setDetailsUserId(row.userId)}
                title={`View details for @${row.userName}`}
                aria-label={`View details for ${row.userName}`}
                className="text-slate-300 hover:text-cyan-300 hover:bg-[#162847] cursor-pointer"
              >
                <Eye className="size-4" />
              </Button>

              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                onClick={() => setEditingUser(row)}
                title={
                  isPrimary
                    ? `Inspect protected account @${row.userName}`
                    : `Edit account @${row.userName}`
                }
                aria-label={`Edit account ${row.userName}`}
                className="text-slate-300 hover:text-blue-300 hover:bg-[#162847] cursor-pointer"
              >
                {isPrimary ? (
                  <Lock className="size-4 text-amber-400" />
                ) : (
                  <Pencil className="size-4" />
                )}
              </Button>

              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon-sm"
                    aria-label={`More actions for ${row.userName}`}
                    className="text-slate-300 hover:text-white hover:bg-[#162847] cursor-pointer"
                  >
                    <MoreHorizontal className="size-4" />
                  </Button>
                </DropdownMenuTrigger>

                <DropdownMenuContent
                  align="end"
                  className="w-52 bg-[#0d182c] border border-[#263750] text-slate-200 shadow-xl"
                >
                  <DropdownMenuItem
                    onClick={() => setDetailsUserId(row.userId)}
                    className="gap-2 cursor-pointer focus:bg-[#162847] focus:text-white"
                  >
                    <Eye className="size-4 text-cyan-400" />
                    <span>View Account Details</span>
                  </DropdownMenuItem>

                  <DropdownMenuItem
                    onClick={() => setEditingUser(row)}
                    className="gap-2 cursor-pointer focus:bg-[#162847] focus:text-white"
                  >
                    {isPrimary ? (
                      <Lock className="size-4 text-amber-400" />
                    ) : (
                      <Pencil className="size-4 text-blue-400" />
                    )}
                    <span>
                      {isPrimary
                        ? "Protected Account Info"
                        : "Edit User Settings"}
                    </span>
                  </DropdownMenuItem>

                  <DropdownMenuSeparator className="bg-[#223656]" />

                  {row.isActive ? (
                    <DropdownMenuItem
                      disabled={Boolean(blockReason) || togglingStatus}
                      onClick={() => {
                        void handleRequestToggleStatus(row);
                      }}
                      title={blockReason ?? "Deactivate this user account"}
                      className="gap-2 text-rose-400 focus:text-rose-300 focus:bg-rose-950/40 cursor-pointer"
                    >
                      <PowerOff className="size-4" />
                      <span>
                        {blockReason
                          ? "Protected from Deactivation"
                          : "Deactivate Account"}
                      </span>
                    </DropdownMenuItem>
                  ) : (
                    <DropdownMenuItem
                      disabled={togglingStatus}
                      onClick={() => {
                        void handleRequestToggleStatus(row);
                      }}
                      className="gap-2 text-emerald-400 focus:text-emerald-300 focus:bg-emerald-950/40 cursor-pointer"
                    >
                      <Power className="size-4" />
                      <span>Activate Account</span>
                    </DropdownMenuItem>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          );
        },
      },
    ],
    [
      accessToken,
      activeAdminCount,
      currentUser?.userId,
      handleRequestToggleStatus,
      togglingStatus,
    ],
  );

  if (!isAuthorizedAdmin) {
    return (
      <section className="page-content">
        <AccessDenied
          pageTitle="User Management"
          userRole={currentUser?.role ?? undefined}
          onBackToDashboard={() => onNavigate("Dashboard")}
        />
      </section>
    );
  }

  const startRecord =
    filteredAndSortedUsers.length === 0
      ? 0
      : (pagination.page - 1) * pagination.pageSize + 1;
  const endRecord = Math.min(
    pagination.page * pagination.pageSize,
    filteredAndSortedUsers.length,
  );

  return (
    <section className="page-content space-y-6">
      {/* 1. PageHero — Visual Introduction ONLY (Zero Interactive Controls Inside) */}
      <PageHero pageId="User Management" />

      {/* 2. Page Actions — Dedicated Action Bar Directly Below PageHero */}
      <div className="flex items-center gap-2 pt-1">
        <span
          className="size-2 rounded-full bg-cyan-400 shadow-[0_0_8px_#00e5ff]"
          aria-hidden="true"
        />
        <span className="text-xs font-semibold text-slate-300">
          Administrator Governance · Exclusive Admin Access
        </span>
      </div>

      {/* Feedback Alerts */}
      {(loadError || actionError) && (
        <ErrorAlert
          message={actionError || loadError || "An error occurred."}
          onRetry={loadError ? refreshUsers : undefined}
          retryLabel="Retry loading users"
        />
      )}

      {successBanner && (
        <div
          role="status"
          className="flex items-center justify-between gap-3 px-4 py-3 rounded-xl bg-emerald-950/50 border border-emerald-500/40 text-emerald-200 text-xs sm:text-sm font-medium shadow-md"
        >
          <div className="flex items-center gap-2.5">
            <CheckCircle2 className="size-4 text-emerald-400 shrink-0" />
            <span>{successBanner}</span>
          </div>
          <button
            type="button"
            onClick={() => setSuccessBanner(null)}
            aria-label="Dismiss notification"
            className="text-emerald-300/80 hover:text-white cursor-pointer bg-transparent border-0 p-1"
          >
            <X className="size-4" />
          </button>
        </div>
      )}

      {/* 3. Real User Statistics */}
      <StatsGrid columns={4}>
        <StatCard
          label="Total Users"
          variant="blue"
          icon={<Users size={20} />}
          value={
            loading ? "..." : loadError ? "—" : formatNumber(totalUsersCount)
          }
          description="Registered system accounts"
        />

        <StatCard
          label="Administrators"
          variant="violet"
          icon={<ShieldCheck size={20} />}
          value={
            loading ? "..." : loadError ? "—" : formatNumber(adminUsersCount)
          }
          description="Full administrative access"
        />

        <StatCard
          label="Staff Accounts"
          variant="amber"
          icon={<UserCog size={20} />}
          value={
            loading ? "..." : loadError ? "—" : formatNumber(staffUsersCount)
          }
          description="Licensing & operational staff"
        />

        <StatCard
          label="Active Accounts"
          variant="emerald"
          icon={<UserCheck size={20} />}
          value={
            loading ? "..." : loadError ? "—" : formatNumber(activeUsersCount)
          }
          description={
            loading || loadError
              ? "Authorized sign-in sessions"
              : `${formatNumber(inactiveUsersCount)} inactive account${inactiveUsersCount === 1 ? "" : "s"}`
          }
        />
      </StatsGrid>

      {/* 4. Main Users Directory Panel */}
      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>System Users Directory</h2>
            <p>
              Inspect linked person accounts, manage administrator access, and
              control active sign-in status
            </p>
          </div>

          <div className="people-header-actions">
            <span className="record-count">
              {loading
                ? "Loading..."
                : filteredAndSortedUsers.length === users.length
                  ? `${users.length} Users`
                  : `${filteredAndSortedUsers.length} of ${users.length} Users`}
            </span>

            <button
              type="button"
              className="primary-button"
              onClick={() => {
                setActionError(null);
                setSuccessBanner(null);
                setIsAddUserOpen(true);
              }}
            >
              <UserPlus size={15} />
              <span>Add User</span>
            </button>
          </div>
        </div>

        <TableToolbar<SearchField>
          searchValue={searchQuery}
          onSearchChange={setSearchQuery}
          searchPlaceholder="Search by full name, @username, User ID, or Person ID..."
          searchAriaLabel="Search system users"
          searchField={searchField}
          searchFieldOptions={SEARCH_FIELD_OPTIONS}
          onSearchFieldChange={setSearchField}
          filters={
            <>
              <div style={{ minWidth: 135 }} className="shrink-0">
                <Select
                  value={roleFilter}
                  onValueChange={(val) => setRoleFilter(val as RoleFilter)}
                >
                  <SelectTrigger
                    className="h-10 min-h-10 w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] rounded-lg"
                    aria-label="Filter by role"
                  >
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Roles</SelectItem>
                    <SelectItem value="admin">Administrators</SelectItem>
                    <SelectItem value="staff">Staff</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div style={{ minWidth: 140 }} className="shrink-0">
                <Select
                  value={statusFilter}
                  onValueChange={(val) => setStatusFilter(val as StatusFilter)}
                >
                  <SelectTrigger
                    className="h-10 min-h-10 w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] rounded-lg"
                    aria-label="Filter by account status"
                  >
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Statuses</SelectItem>
                    <SelectItem value="active">Active Only</SelectItem>
                    <SelectItem value="inactive">Inactive Only</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </>
          }
          onReset={handleResetFilters}
          isFiltered={isFiltered}
          onRefresh={refreshUsers}
          refreshing={loading}
        />

        <DataTable<User>
          data={displayedUsers}
          columns={columns}
          getRowKey={(u) => u.userId}
          loading={loading}
          loadingMessage="Loading system user accounts..."
          emptyState={
            <EmptyState
              icon={<Users size={36} className="text-slate-400 mx-auto mb-2" />}
              message={
                isFiltered
                  ? "No user accounts match your current search or filter criteria."
                  : "No user accounts found."
              }
            />
          }
          minWidth={940}
          sortColumn={sortColumn}
          sortDirection={sortDirection}
          onSort={handleSort}
          onRowClick={(u) => setDetailsUserId(u.userId)}
          rowContextMenu={getRowContextMenu}
          ariaLabel="System users management table"
        />

        {/* Pagination Bar */}
        {!loading && filteredAndSortedUsers.length > 0 && (
          <div className="flex flex-col sm:flex-row items-center justify-between gap-3 pt-4 mt-4 border-t border-[#1e304d] text-xs text-slate-400">
            <span>
              Showing <strong className="text-white">{startRecord}</strong>–
              <strong className="text-white">{endRecord}</strong> of{" "}
              <strong className="text-white">
                {filteredAndSortedUsers.length}
              </strong>{" "}
              user{filteredAndSortedUsers.length === 1 ? "" : "s"}
            </span>

            {pagination.totalPages > 1 && (
              <div className="flex items-center gap-1.5">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={!pagination.hasPreviousPage}
                  onClick={pagination.previousPage}
                  className="h-8 px-2.5 bg-[#0e1b33] border-[#263750] text-slate-200 hover:bg-[#162847] cursor-pointer"
                >
                  <ChevronLeft className="size-3.5 mr-1" />
                  Prev
                </Button>

                <span className="px-2.5 font-semibold text-slate-200">
                  Page {pagination.page} of {pagination.totalPages}
                </span>

                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={!pagination.hasNextPage}
                  onClick={pagination.nextPage}
                  className="h-8 px-2.5 bg-[#0e1b33] border-[#263750] text-slate-200 hover:bg-[#162847] cursor-pointer"
                >
                  Next
                  <ChevronRight className="size-3.5 ml-1" />
                </Button>
              </div>
            )}
          </div>
        )}
      </section>

      {/* Modals */}
      <AddUserModal
        isOpen={isAddUserOpen}
        accessToken={accessToken}
        existingUsers={users}
        onClose={() => setIsAddUserOpen(false)}
        onNavigateToAddPerson={() => onNavigate("Add Person")}
        onCreated={(newUserId, createdUserName, role) => {
          setIsAddUserOpen(false);
          setSuccessBanner(
            `${getRoleLabel(role)} account @${createdUserName} (#${newUserId}) was created successfully.`,
          );
          refreshUsers();
        }}
      />

      <UserDetailsModal
        isOpen={detailsUserId !== null}
        userId={detailsUserId}
        accessToken={accessToken}
        currentUserId={currentUser?.userId}
        onClose={() => setDetailsUserId(null)}
        onEditUser={(id) => {
          const found = users.find((u) => u.userId === id) ?? null;
          if (found) {
            setEditingUser(found);
          }
        }}
      />

      <EditUserModal
        isOpen={editingUser !== null}
        user={editingUser}
        existingUsers={users}
        accessToken={accessToken}
        currentUserId={currentUser?.userId}
        activeAdminCount={activeAdminCount}
        onClose={() => setEditingUser(null)}
        onUpdated={(updatedId, updatedUserName, updatedIsActive, updatedRole) => {
          setEditingUser(null);
          if (
            currentUser?.userId === updatedId &&
            currentUser.userName !== updatedUserName
          ) {
            updateAuthUser({ userName: updatedUserName });
          }
          setSuccessBanner(
            `Account @${updatedUserName} (#${updatedId}) was updated (${updatedIsActive ? "Active" : "Inactive"}${updatedRole ? ` · ${getRoleLabel(updatedRole)}` : ""}).`,
          );
          refreshUsers();
        }}
      />

      <ConfirmModal
        isOpen={statusToggleTarget !== null}
        title="Deactivate User Account"
        message={
          statusToggleTarget
            ? `Are you sure you want to deactivate @${statusToggleTarget.userName} (${getRoleLabel(statusToggleTarget.role)})? Their active sessions will be immediately invalidated and they will no longer be able to sign in.`
            : ""
        }
        confirmText={togglingStatus ? "Deactivating..." : "Deactivate Account"}
        cancelText="Cancel"
        confirmVariant="danger"
        onConfirm={handleConfirmDeactivate}
        onCancel={() => {
          if (!togglingStatus) {
            setStatusToggleTarget(null);
          }
        }}
      />
    </section>
  );
}
