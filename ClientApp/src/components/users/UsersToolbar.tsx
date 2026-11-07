import TableToolbar from "../common/TableToolbar";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../ui/select";
import {
  SEARCH_FIELD_OPTIONS,
  type SearchField,
  type RoleFilter,
  type StatusFilter,
} from "./userTypes";

export interface UsersToolbarProps {
  searchQuery: string;
  onSearchChange: (query: string) => void;
  searchField: SearchField;
  onSearchFieldChange: (field: SearchField) => void;
  roleFilter: RoleFilter;
  onRoleFilterChange: (role: RoleFilter) => void;
  statusFilter: StatusFilter;
  onStatusFilterChange: (status: StatusFilter) => void;
  isFiltered: boolean;
  onResetFilters: () => void;
  onRefresh: () => void;
  loading: boolean;
}

export default function UsersToolbar({
  searchQuery,
  onSearchChange,
  searchField,
  onSearchFieldChange,
  roleFilter,
  onRoleFilterChange,
  statusFilter,
  onStatusFilterChange,
  isFiltered,
  onResetFilters,
  onRefresh,
  loading,
}: UsersToolbarProps) {
  return (
    <TableToolbar<SearchField>
      searchValue={searchQuery}
      onSearchChange={onSearchChange}
      searchPlaceholder="Search by full name, @username, User ID, or Person ID..."
      searchAriaLabel="Search system users"
      searchField={searchField}
      searchFieldOptions={SEARCH_FIELD_OPTIONS}
      onSearchFieldChange={onSearchFieldChange}
      filters={
        <>
          <div style={{ minWidth: 135 }} className="shrink-0">
            <Select
              value={roleFilter}
              onValueChange={(val) => onRoleFilterChange(val as RoleFilter)}
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
              onValueChange={(val) => onStatusFilterChange(val as StatusFilter)}
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
      onReset={onResetFilters}
      isFiltered={isFiltered}
      onRefresh={onRefresh}
      refreshing={loading}
    />
  );
}
