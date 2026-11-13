import {
  Award,
  CalendarDays,
  Car,
  ClipboardCheck,
  Copy,
  CreditCard,
  FileText,
  Flag,
  Globe,
  Layers,
  LayoutDashboard,
  Lock,
  LockKeyhole,
  RotateCw,
  Settings,
  ShieldAlert,
  UserCog,
  UserRound,
  Users,
  type LucideIcon,
} from "lucide-react";
import { isPagePermitted, type PageId } from "../../../utils/permissions";

/**
 * Single source of truth for the sidebar navigation structure.
 *
 * Every `page` is an existing DVLD page id registered in `PAGE_TO_PATH`
 * (navigationContextInstance.ts) and `PAGE_PERMISSIONS` (permissions.ts).
 * Visibility is always resolved through `isPagePermitted`, so the sidebar
 * never shows a destination the current role cannot open. Route guarding
 * itself remains the responsibility of AppRouter and the backend API.
 */

export interface SidebarLinkDef {
  readonly kind: "link";
  readonly page: PageId;
  readonly label?: string;
  readonly icon: LucideIcon;
  /** Detail/sub pages that should highlight this entry as the active item. */
  readonly activeFor?: readonly PageId[];
}

export interface SidebarGroupDef {
  readonly kind: "group";
  readonly id: string;
  readonly label: string;
  readonly icon: LucideIcon;
  readonly items: readonly SidebarLinkDef[];
}

export type SidebarEntry = SidebarLinkDef | SidebarGroupDef;

export interface SidebarSectionDef {
  readonly id: string;
  readonly title: string;
  readonly entries: readonly SidebarEntry[];
}

export const SIDEBAR_SECTIONS: readonly SidebarSectionDef[] = [
  {
    id: "main",
    title: "Main Navigation",
    entries: [
      { kind: "link", page: "Dashboard", icon: LayoutDashboard },
      { kind: "link", page: "User Management", icon: UserCog },
    ],
  },
  {
    id: "directory",
    title: "Directory",
    entries: [
      {
        kind: "link",
        page: "People",
        icon: Users,
        activeFor: ["Add Person", "Edit Person", "Person Details"],
      },
      { kind: "link", page: "Drivers", icon: Car, activeFor: ["Person License History"] },
    ],
  },
  {
    id: "applications",
    title: "Applications & Tests",
    entries: [
      { kind: "link", page: "Local Applications", icon: FileText },
      {
        kind: "link",
        page: "Test Appointments",
        icon: CalendarDays,
        activeFor: [
          "Manage Test Appointments",
          "Written Test Appointments",
          "Appointment Details",
          "Schedule Appointment",
          "Take Examination",
        ],
      },
      { kind: "link", page: "International Licenses", icon: Globe },
    ],
  },
  {
    id: "license-services",
    title: "License Services",
    entries: [
      { kind: "link", page: "Licenses", icon: CreditCard },
      { kind: "link", page: "Renew License", icon: RotateCw },
      { kind: "link", page: "Replace License", icon: Copy },
      { kind: "link", page: "Detain License", icon: ShieldAlert },
      { kind: "link", page: "Detained Licenses", icon: Lock },
    ],
  },
  {
    id: "settings",
    title: "",
    entries: [
      {
        kind: "group",
        id: "settings",
        label: "Settings",
        icon: Settings,
        items: [
          { kind: "link", page: "Application Types", icon: Layers },
          { kind: "link", page: "License Classes", icon: Award },
          { kind: "link", page: "Test Types", icon: ClipboardCheck },
          { kind: "link", page: "Countries", icon: Flag },
        ],
      },
    ],
  },
  {
    id: "account",
    title: "Account",
    entries: [
      { kind: "link", page: "My Profile", icon: UserRound },
      { kind: "link", page: "Change Password", icon: LockKeyhole },
    ],
  },
];

/**
 * Returns only the sections, groups, and links the given role may open.
 * - Links are filtered with `isPagePermitted` (fails closed for unknown roles).
 * - Groups without permitted children are removed.
 * - Groups with a single permitted child are flattened into a direct link,
 *   avoiding a pointless one-item submenu (e.g. "Directory" for Admin).
 * - Sections left without entries are removed.
 */
export function buildVisibleSections(
  role: string | null | undefined,
): SidebarSectionDef[] {
  const visibleSections: SidebarSectionDef[] = [];

  for (const section of SIDEBAR_SECTIONS) {
    const entries: SidebarEntry[] = [];

    for (const entry of section.entries) {
      if (entry.kind === "link") {
        if (isPagePermitted(entry.page, role)) entries.push(entry);
        continue;
      }

      const items = entry.items.filter((item) =>
        isPagePermitted(item.page, role),
      );
      if (items.length === 1) {
        entries.push(items[0]);
      } else if (items.length > 1) {
        entries.push({ ...entry, items });
      }
    }

    if (entries.length > 0) {
      visibleSections.push({ ...section, entries });
    }
  }

  return visibleSections;
}

export function isLinkActive(
  link: SidebarLinkDef,
  activePage: string,
): boolean {
  return (
    link.page === activePage ||
    (link.activeFor?.some((page) => page === activePage) ?? false)
  );
}

export function findActiveGroupId(
  sections: readonly SidebarSectionDef[],
  activePage: string,
): string | null {
  for (const section of sections) {
    for (const entry of section.entries) {
      if (
        entry.kind === "group" &&
        entry.items.some((item) => isLinkActive(item, activePage))
      ) {
        return entry.id;
      }
    }
  }
  return null;
}
