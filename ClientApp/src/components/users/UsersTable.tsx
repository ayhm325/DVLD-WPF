import { useMemo, useCallback } from "react";
import {
  Users,
  ShieldCheck,
  UserCog,
  Eye,
  Pencil,
  Power,
  PowerOff,
  MoreHorizontal,
  Lock,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";
import DataTable, {
  type DataTableColumn,
  type DataTableRowAction,
  type SortDirection,
} from "../common/DataTable";
import EmptyState from "../common/EmptyState";
import PersonPhoto from "../common/PersonPhoto";
import StatusBadge from "../common/StatusBadge";
import { Button } from "../ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "../ui/dropdown-menu";
import type { User } from "../../services/userService";
import {
  getDeactivationBlockReason,
  isAdminRole,
  isPrimaryAdminUser,
} from "./userValidation";

export interface UsersTablePagination {
  page: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  previousPage: () => void;
  nextPage: () => void;
  startRecord: number;
  endRecord: number;
}

export interface UsersTableProps {
  users: User[];
  totalFilteredCount: number;
  loading: boolean;
  isFiltered: boolean;
  accessToken: string;
  currentUserId?: number;
  activeAdminCount: number;
  sortColumn: string;
  sortDirection: SortDirection;
  onSort: (columnKey: string, direction: SortDirection) => void;
  onViewDetails: (userId: number) => void;
  onEditUser: (user: User) => void;
  onRequestToggleStatus: (user: User) => void;
  togglingStatus: boolean;
  pagination: UsersTablePagination;
}

export default function UsersTable({
  users,
  totalFilteredCount,
  loading,
  isFiltered,
  accessToken,
  currentUserId,
  activeAdminCount,
  sortColumn,
  sortDirection,
  onSort,
  onViewDetails,
  onEditUser,
  onRequestToggleStatus,
  togglingStatus,
  pagination,
}: UsersTableProps) {
  const getRowContextMenu = useCallback(
    (row: User): DataTableRowAction<User>[] => {
      const isPrimary = isPrimaryAdminUser(row);
      const blockReason = getDeactivationBlockReason(
        row,
        currentUserId,
        activeAdminCount,
      );

      const actions: DataTableRowAction<User>[] = [
        {
          key: "view-details",
          label: "View Account Details",
          icon: <Eye className="size-4 text-cyan-400" />,
          onClick: (u: User) => onViewDetails(u.userId),
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
          onClick: (u: User) => onEditUser(u),
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
            void onRequestToggleStatus(u);
          },
        });
      } else {
        actions.push({
          key: "activate-user",
          label: "Activate Account",
          icon: <Power className="size-4 text-emerald-400" />,
          disabled: togglingStatus,
          onClick: (u: User) => {
            void onRequestToggleStatus(u);
          },
        });
      }

      return actions;
    },
    [
      activeAdminCount,
      currentUserId,
      onRequestToggleStatus,
      onViewDetails,
      onEditUser,
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
          const isSelf = row.userId === currentUserId;

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
            currentUserId,
            activeAdminCount,
          );

          return (
            <div className="inline-flex items-center justify-end gap-1.5">
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                onClick={() => onViewDetails(row.userId)}
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
                onClick={() => onEditUser(row)}
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
                    onClick={() => onViewDetails(row.userId)}
                    className="gap-2 cursor-pointer focus:bg-[#162847] focus:text-white"
                  >
                    <Eye className="size-4 text-cyan-400" />
                    <span>View Account Details</span>
                  </DropdownMenuItem>

                  <DropdownMenuItem
                    onClick={() => onEditUser(row)}
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
                        void onRequestToggleStatus(row);
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
                        void onRequestToggleStatus(row);
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
      currentUserId,
      onEditUser,
      onRequestToggleStatus,
      onViewDetails,
      togglingStatus,
    ],
  );

  return (
    <>
      <DataTable<User>
        data={users}
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
        onSort={onSort}
        onRowClick={(u) => onViewDetails(u.userId)}
        rowContextMenu={getRowContextMenu}
        ariaLabel="System users management table"
      />

      {/* Pagination Bar */}
      {!loading && totalFilteredCount > 0 && (
        <div className="flex flex-col sm:flex-row items-center justify-between gap-3 pt-4 mt-4 border-t border-[#1e304d] text-xs text-slate-400">
          <span>
            Showing <strong className="text-white">{pagination.startRecord}</strong>–
            <strong className="text-white">{pagination.endRecord}</strong> of{" "}
            <strong className="text-white">{totalFilteredCount}</strong>{" "}
            user{totalFilteredCount === 1 ? "" : "s"}
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
    </>
  );
}
