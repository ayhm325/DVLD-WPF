import { useCallback, useMemo, useState } from "react";
import {
  UserPlus,
  Users,
  Eye,
  Pencil,
  Trash2,
  Mail,
  Phone,
  MoreHorizontal,
} from "lucide-react";
import type { Person } from "../types/Person";
import { formatDate } from "../utils/formatters";
import TableToolbar from "./common/TableToolbar";
import EmptyState from "./common/EmptyState";
import PersonPhoto from "./common/PersonPhoto";
import DataTable, {
  type DataTableColumn,
  type DataTableRowAction,
} from "./common/DataTable";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "./ui/dropdown-menu";
import { Button } from "./ui/button";

type SearchBy = "all" | "name" | "nationalNo" | "id" | "phone";

const SEARCH_OPTIONS: { value: SearchBy; label: string }[] = [
  { value: "all", label: "All Fields" },
  { value: "name", label: "Full Name" },
  { value: "nationalNo", label: "National No." },
  { value: "id", label: "Person ID" },
  { value: "phone", label: "Phone" },
];
type GenderFilter = "all" | "0" | "1";

export type PeopleTableProps = {
  people: Person[];
  searchName?: string;
  onSearchChange?: (value: string) => void;
  onAddPerson: () => void;
  onRefresh?: () => void;
  loading?: boolean;
  onShowDetails?: (person: Person) => void;
  onEditPerson?: (person: Person) => void;
  onDeletePerson?: (person: Person) => void;
};

export default function PeopleTable({
  people,
  searchName,
  onSearchChange,
  onAddPerson,
  onRefresh,
  loading = false,
  onShowDetails,
  onEditPerson,
  onDeletePerson,
}: PeopleTableProps) {
  const [internalSearch, setInternalSearch] = useState(searchName ?? "");
  const [searchBy, setSearchBy] = useState<SearchBy>("all");
  const [genderFilter, setGenderFilter] = useState<GenderFilter>("all");

  const search = searchName !== undefined ? searchName : internalSearch;

  function handleSearchChange(value: string) {
    setInternalSearch(value);
    onSearchChange?.(value);
  }

  function handleResetFilters() {
    handleSearchChange("");
    setSearchBy("all");
    setGenderFilter("all");
  }

  const isFiltered = Boolean(
    search.trim() || genderFilter !== "all" || searchBy !== "all",
  );

  const filteredPeople = useMemo(() => {
    const query = search.trim().toLowerCase();

    return people.filter((person) => {
      if (genderFilter !== "all" && String(person.gender) !== genderFilter) {
        return false;
      }

      if (!query) return true;

      const cleanIdQuery = query.replace(/^#/, "");

      if (searchBy === "name") {
        return (person.fullName || "").toLowerCase().includes(query);
      }
      if (searchBy === "nationalNo") {
        return (person.nationalNo || "").toLowerCase().includes(query);
      }
      if (searchBy === "id") {
        return (
          cleanIdQuery.length > 0 &&
          String(person.personId).includes(cleanIdQuery)
        );
      }
      if (searchBy === "phone") {
        return (person.phone || "").toLowerCase().includes(query);
      }

      return (
        (person.fullName || "").toLowerCase().includes(query) ||
        (person.nationalNo || "").toLowerCase().includes(query) ||
        (cleanIdQuery.length > 0 &&
          String(person.personId).includes(cleanIdQuery)) ||
        (person.phone || "").toLowerCase().includes(query)
      );
    });
  }, [people, search, searchBy, genderFilter]);

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case "name":
        return "Search by full name...";
      case "nationalNo":
        return "Search by national number...";
      case "id":
        return "Search by person ID...";
      case "phone":
        return "Search by phone number...";
      default:
        return "Search by name, national no, ID, phone...";
    }
  }, [searchBy]);

  const getRowContextMenu = useCallback(
    (person: Person): DataTableRowAction<Person>[] => {
      const hasEmail = Boolean(person.email && person.email.trim().length > 0);
      const hasPhone = Boolean(person.phone && person.phone.trim().length > 0);

      return [
        {
          key: "show-details",
          label: "Show Details",
          icon: <Eye className="size-4" />,
          separatorAfter: true,
          onClick: (p) => onShowDetails?.(p),
        },
        {
          key: "add-new-person",
          label: "Add New Person",
          icon: <UserPlus className="size-4" />,
          onClick: () => onAddPerson(),
        },
        {
          key: "edit",
          label: "Edit",
          icon: <Pencil className="size-4" />,
          onClick: (p) => onEditPerson?.(p),
        },
        {
          key: "delete",
          label: "Delete",
          icon: <Trash2 className="size-4" />,
          danger: true,
          separatorAfter: true,
          onClick: (p) => onDeletePerson?.(p),
        },
        {
          key: "send-email",
          label: "Send Email",
          icon: <Mail className="size-4" />,
          disabled: !hasEmail,
          onClick: (p) => {
            if (p.email && p.email.trim()) {
              window.location.href = `mailto:${encodeURIComponent(p.email.trim())}`;
            }
          },
        },
        {
          key: "phone-call",
          label: "Phone Call",
          icon: <Phone className="size-4" />,
          disabled: !hasPhone,
          onClick: (p) => {
            if (p.phone && p.phone.trim()) {
              window.location.href = `tel:${encodeURIComponent(p.phone.trim())}`;
            }
          },
        },
      ];
    },
    [onShowDetails, onAddPerson, onEditPerson, onDeletePerson],
  );

  const columns = useMemo<DataTableColumn<Person>[]>(
    () => [
      {
        key: "personId",
        header: "ID",
        headerClassName: "w-16 col-compact",
        className: "col-compact",
        render: (person) => (
          <span
            style={{
              fontFamily: "ui-monospace, monospace",
              color: "var(--dvld-table-text-muted, #94a3b8)",
            }}
          >
            #{person.personId}
          </span>
        ),
      },
      {
        key: "fullName",
        header: "Full Name",
        render: (person) => (
          <div className="person-cell flex items-center gap-2.5">
            <PersonPhoto
              personId={person.personId}
              hasPhoto={person.hasPhoto}
              photoKey={person.imagePath}
              fullName={person.fullName}
              size="sm"
            />
            <strong>{person.fullName}</strong>
          </div>
        ),
      },
      {
        key: "nationalNo",
        header: "National No.",
        render: (person) => (
          <span
            style={{
              fontVariantNumeric: "tabular-nums",
              letterSpacing: "0.2px",
              color: "var(--dvld-table-text-secondary, #c0cddf)",
            }}
          >
            {person.nationalNo}
          </span>
        ),
      },
      {
        key: "dateOfBirth",
        header: "Date of Birth",
        render: (person) => (
          <span style={{ color: "var(--dvld-table-text-muted, #94a3b8)" }}>
            {formatDate(person.dateOfBirth)}
          </span>
        ),
      },
      {
        key: "gender",
        header: "Gender",
        render: (person) => (
          <span
            className={`gender-badge ${person.gender === 0 ? "is-male" : "is-female"}`}
          >
            {person.gender === 0 ? "Male" : "Female"}
          </span>
        ),
      },
      {
        key: "address",
        header: "Address",
        render: (person) => (
          <span
            className="max-w-45 truncate block"
            title={person.address || "—"}
            style={{ color: "var(--dvld-table-text-secondary, #c0cddf)" }}
          >
            {person.address || "—"}
          </span>
        ),
      },
      {
        key: "phone",
        header: "Phone",
        render: (person) => (
          <span
            style={{
              fontVariantNumeric: "tabular-nums",
              letterSpacing: "0.2px",
              color: "var(--dvld-table-text-secondary, #c0cddf)",
            }}
          >
            {person.phone || "—"}
          </span>
        ),
      },
      {
        key: "email",
        header: "Email",
        render: (person) => (
          <span
            className="max-w-45 truncate block"
            title={person.email || "—"}
            style={{
              color: person.email
                ? "var(--dvld-table-text-muted, #94a3b8)"
                : "var(--dvld-table-text-muted, #64748b)",
            }}
          >
            {person.email || "—"}
          </span>
        ),
      },
      {
        key: "countryName",
        header: "Country",
        render: (person) => (
          <span
            style={{
              color: "var(--dvld-table-text-secondary, #c0cddf)",
              fontWeight: 500,
            }}
          >
            {person.countryName || "—"}
          </span>
        ),
      },
      {
        key: "actions",
        header: "Actions",
        headerClassName: "w-20 text-center",
        className: "text-center",
        render: (person) => {
          const hasEmail = Boolean(
            person.email && person.email.trim().length > 0,
          );
          const hasPhone = Boolean(
            person.phone && person.phone.trim().length > 0,
          );

          return (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className="size-8 p-0 text-slate-400 hover:text-white hover:bg-[#1a2c47] rounded-lg"
                  title="Person actions"
                  aria-label={`Actions for person ${person.fullName}`}
                >
                  <MoreHorizontal className="size-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent
                align="end"
                sideOffset={4}
                className="min-w-48 p-1 bg-[#101a2d] border border-[#263750] shadow-2xl rounded-xl z-50 ring-1 ring-white/10 text-slate-200"
              >
                <DropdownMenuItem
                  onSelect={() => onShowDetails?.(person)}
                  onClick={() => onShowDetails?.(person)}
                  className="gap-2 text-xs font-medium cursor-pointer rounded-lg px-2.5 py-1.5 transition-colors text-slate-200 focus:text-white focus:bg-surface-soft hover:bg-surface-soft"
                >
                  <Eye className="size-4 text-slate-400" />
                  <span>Show Details</span>
                </DropdownMenuItem>
                <DropdownMenuSeparator className="my-1 border-t border-[#263750]" />
                <DropdownMenuItem
                  onClick={onAddPerson}
                  className="gap-2 text-xs font-medium cursor-pointer rounded-lg px-2.5 py-1.5 transition-colors text-slate-200 focus:text-white focus:bg-surface-soft hover:bg-surface-soft"
                >
                  <UserPlus className="size-4 text-slate-400" />
                  <span>Add New Person</span>
                </DropdownMenuItem>
                <DropdownMenuItem
                  onSelect={() => onEditPerson?.(person)}
                  onClick={() => onEditPerson?.(person)}
                  className="gap-2 text-xs font-medium cursor-pointer rounded-lg px-2.5 py-1.5 transition-colors text-slate-200 focus:text-white focus:bg-surface-soft hover:bg-surface-soft"
                >
                  <Pencil className="size-4 text-slate-400" />
                  <span>Edit</span>
                </DropdownMenuItem>
                <DropdownMenuItem
                  onSelect={() => onDeletePerson?.(person)}
                  onClick={() => onDeletePerson?.(person)}
                  className="gap-2 text-xs font-medium cursor-pointer rounded-lg px-2.5 py-1.5 transition-colors text-red-400 focus:text-red-300 focus:bg-red-950/40 hover:bg-red-950/40"
                >
                  <Trash2 className="size-4 text-red-400" />
                  <span>Delete</span>
                </DropdownMenuItem>
                <DropdownMenuSeparator className="my-1 border-t border-[#263750]" />
                <DropdownMenuItem
                  disabled={!hasEmail}
                  onClick={() => {
                    if (person.email && person.email.trim()) {
                      window.location.href = `mailto:${encodeURIComponent(person.email.trim())}`;
                    }
                  }}
                  className="gap-2 text-xs font-medium cursor-pointer rounded-lg px-2.5 py-1.5 transition-colors text-slate-200 focus:text-white focus:bg-surface-soft hover:bg-surface-soft"
                >
                  <Mail className="size-4 text-slate-400" />
                  <span>Send Email</span>
                </DropdownMenuItem>
                <DropdownMenuItem
                  disabled={!hasPhone}
                  onClick={() => {
                    if (person.phone && person.phone.trim()) {
                      window.location.href = `tel:${encodeURIComponent(person.phone.trim())}`;
                    }
                  }}
                  className="gap-2 text-xs font-medium cursor-pointer rounded-lg px-2.5 py-1.5 transition-colors text-slate-200 focus:text-white focus:bg-surface-soft hover:bg-surface-soft"
                >
                  <Phone className="size-4 text-slate-400" />
                  <span>Phone Call</span>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          );
        },
      },
    ],
    [onShowDetails, onAddPerson, onEditPerson, onDeletePerson],
  );

  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <h2>People Directory</h2>
          <p>Manage and search registered citizen and resident records</p>
        </div>

        <div className="people-header-actions">
          <span className="record-count">
            {loading
              ? "Loading..."
              : filteredPeople.length === people.length
                ? `${people.length} People`
                : `${filteredPeople.length} of ${people.length} People`}
          </span>

          <button
            type="button"
            className="primary-button"
            onClick={onAddPerson}
          >
            <UserPlus size={15} />
            <span>Add Person</span>
          </button>
        </div>
      </div>

      <TableToolbar<SearchBy>
        searchValue={search}
        onSearchChange={handleSearchChange}
        searchPlaceholder={placeholderText}
        searchField={searchBy}
        searchFieldOptions={SEARCH_OPTIONS}
        onSearchFieldChange={setSearchBy}
        filters={
          <div style={{ minWidth: 130 }} className="shrink-0">
            <Select
              value={genderFilter}
              onValueChange={(val) => setGenderFilter(val as GenderFilter)}
            >
              <SelectTrigger
                className="h-10 min-h-10 w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg"
                aria-label="Filter by gender"
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Genders</SelectItem>
                <SelectItem value="0">Male</SelectItem>
                <SelectItem value="1">Female</SelectItem>
              </SelectContent>
            </Select>
          </div>
        }
        onReset={handleResetFilters}
        isFiltered={isFiltered}
        onRefresh={onRefresh}
        refreshing={loading}
      />

      <DataTable<Person>
        data={filteredPeople}
        columns={columns}
        getRowKey={(person) => person.personId}
        loading={loading}
        loadingMessage="Loading people directory..."
        emptyState={
          <EmptyState
            icon={<Users size={36} className="text-slate-400 mx-auto mb-2" />}
            message="No people found matching your criteria."
          />
        }
        minWidth={1100}
        rowContextMenu={getRowContextMenu}
      />
    </section>
  );
}
