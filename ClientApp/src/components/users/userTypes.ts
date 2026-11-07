export type SearchField = "all" | "fullName" | "userName" | "userId" | "personId";
export type RoleFilter = "all" | "admin" | "staff";
export type StatusFilter = "all" | "active" | "inactive";

export const SEARCH_FIELD_OPTIONS: { value: SearchField; label: string }[] = [
  { value: "all", label: "All Fields" },
  { value: "fullName", label: "Full Name" },
  { value: "userName", label: "Username" },
  { value: "userId", label: "User ID" },
  { value: "personId", label: "Person ID" },
];
