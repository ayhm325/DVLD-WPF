import { useCallback, useEffect, useMemo, useState } from "react";
import { UserPlus, CheckCircle2, X } from "lucide-react";
import PageHero from "../common/PageHero";
import ErrorAlert from "../common/ErrorAlert";
import ConfirmModal from "../common/ConfirmModal";
import AccessDenied from "../common/AccessDenied";
import { type SortDirection } from "../common/DataTable";
import { useAuth } from "../../context/useAuth";
import { useAsyncData, usePagination } from "../../hooks";
import { getUsers, updateUser, type User } from "../../services/userService";
import { isPagePermitted } from "../../utils/permissions";
import AddUserModal from "./AddUserModal";
import EditUserModal from "./EditUserModal";
import UserDetailsModal from "./UserDetailsModal";
import {
  getDeactivationBlockReason,
  getRoleLabel,
  isAdminRole,
} from "./userValidation";
import type { SearchField, RoleFilter, StatusFilter } from "./userTypes";
import UsersSummary from "./UsersSummary";
import UsersToolbar from "./UsersToolbar";
import UsersTable from "./UsersTable";

export interface UsersPageProps {
  accessToken: string;
  onNavigate: (page: string) => void;
}

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

  // Statistics computed strictly from backend /api/Users records
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
      {/* 1. PageHero */}
      <PageHero pageId="User Management" />

      {/* 2. Page Actions */}
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
      <UsersSummary
        totalUsersCount={totalUsersCount}
        adminUsersCount={adminUsersCount}
        staffUsersCount={staffUsersCount}
        activeUsersCount={activeUsersCount}
        inactiveUsersCount={inactiveUsersCount}
        loading={loading}
        loadError={loadError}
      />

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

        {/* Table Toolbar */}
        <UsersToolbar
          searchQuery={searchQuery}
          onSearchChange={setSearchQuery}
          searchField={searchField}
          onSearchFieldChange={setSearchField}
          roleFilter={roleFilter}
          onRoleFilterChange={setRoleFilter}
          statusFilter={statusFilter}
          onStatusFilterChange={setStatusFilter}
          isFiltered={isFiltered}
          onResetFilters={handleResetFilters}
          onRefresh={refreshUsers}
          loading={loading}
        />

        {/* Users Table */}
        <UsersTable
          users={displayedUsers}
          totalFilteredCount={filteredAndSortedUsers.length}
          loading={loading}
          isFiltered={isFiltered}
          accessToken={accessToken}
          currentUserId={currentUser?.userId}
          activeAdminCount={activeAdminCount}
          sortColumn={sortColumn}
          sortDirection={sortDirection}
          onSort={handleSort}
          onViewDetails={(id) => setDetailsUserId(id)}
          onEditUser={(u) => setEditingUser(u)}
          onRequestToggleStatus={(u) => void handleRequestToggleStatus(u)}
          togglingStatus={togglingStatus}
          pagination={{
            page: pagination.page,
            totalPages: pagination.totalPages,
            hasPreviousPage: pagination.hasPreviousPage,
            hasNextPage: pagination.hasNextPage,
            previousPage: pagination.previousPage,
            nextPage: pagination.nextPage,
            startRecord,
            endRecord,
          }}
        />
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
