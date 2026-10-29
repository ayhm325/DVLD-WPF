import type { SVGProps } from 'react'

export interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number | string
  strokeWidth?: number | string
}

const defaultProps = {
  xmlns: 'http://www.w3.org/2000/svg',
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
}

export function DashboardIcon({ size = 20, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <rect width="7" height="9" x="3" y="3" rx="1" />
      <rect width="7" height="5" x="14" y="3" rx="1" />
      <rect width="7" height="9" x="14" y="12" rx="1" />
      <rect width="7" height="5" x="3" y="16" rx="1" />
    </svg>
  )
}

export function UsersIcon({ size = 20, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
      <path d="M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
  )
}

export function UserIcon({ size = 20, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" />
      <circle cx="12" cy="7" r="4" />
    </svg>
  )
}

export function UserPlusIcon({ size = 20, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <line x1="19" x2="19" y1="8" y2="14" />
      <line x1="22" x2="16" y1="11" y2="11" />
    </svg>
  )
}

export function DriversIcon({ size = 20, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M19 17h2c.6 0 1-.4 1-1v-3c0-.9-.7-1.7-1.5-1.9C18.7 10.6 16 10 16 10s-1.3-1.4-2.2-2.3c-.5-.4-1.1-.7-1.8-.7H5c-.6 0-1.1.4-1.4.9l-1.4 2.9A3.7 3.7 0 0 0 2 12v4c0 .6.4 1 1 1h2" />
      <circle cx="7" cy="17" r="2" />
      <path d="M9 17h6" />
      <circle cx="17" cy="17" r="2" />
    </svg>
  )
}

export function LicenseIcon({ size = 20, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <rect width="18" height="18" x="3" y="3" rx="2" />
      <path d="M7 7h10" />
      <path d="M7 12h10" />
      <path d="M7 17h6" />
    </svg>
  )
}

export function AwardIcon({ size = 20, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <circle cx="12" cy="8" r="6" />
      <path d="m15.477 12.89 1.515 8.526a.5.5 0 0 1-.81.47l-3.58-2.687a1 1 0 0 0-1.197 0l-3.586 2.686a.5.5 0 0 1-.81-.469l1.514-8.526" />
    </svg>
  )
}

export function GlobeIcon({ size = 20, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <circle cx="12" cy="12" r="10" />
      <path d="M12 2a14.5 14.5 0 0 0 0 20 14.5 14.5 0 0 0 0-20" />
      <path d="M2 12h20" />
    </svg>
  )
}

export function ApplicationsIcon({ size = 20, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z" />
      <path d="M14 2v4a2 2 0 0 0 2 2h4" />
      <path d="M10 9H8" />
      <path d="M16 13H8" />
      <path d="M16 17H8" />
    </svg>
  )
}

export function SettingsIcon({ size = 20, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" />
      <circle cx="12" cy="12" r="3" />
    </svg>
  )
}

export function SearchIcon({ size = 18, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <circle cx="11" cy="11" r="8" />
      <path d="m21 21-4.3-4.3" />
    </svg>
  )
}

export function PlusIcon({ size = 18, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M5 12h14" />
      <path d="M12 5v14" />
    </svg>
  )
}

export function PencilIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z" />
      <path d="m15 5 4 4" />
    </svg>
  )
}

export function TrashIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M3 6h18" />
      <path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6" />
      <path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2" />
      <line x1="10" x2="10" y1="11" y2="17" />
      <line x1="14" x2="14" y1="11" y2="17" />
    </svg>
  )
}

export function EyeIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z" />
      <circle cx="12" cy="12" r="3" />
    </svg>
  )
}

export function EyeOffIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M9.88 9.88a3 3 0 1 0 4.24 4.24" />
      <path d="M10.73 5.08A10.43 10.43 0 0 1 12 5c7 0 10 7 10 7a13.16 13.16 0 0 1-1.67 2.68" />
      <path d="M6.61 6.61A13.526 13.526 0 0 0 2 12s3 7 10 7a9.74 9.74 0 0 0 5.39-1.61" />
      <line x1="2" x2="22" y1="2" y2="22" />
    </svg>
  )
}

export function RefreshIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M21 12a9 9 0 0 0-9-9 9.75 9.75 0 0 0-6.74 2.74L3 8" />
      <path d="M3 3v5h5" />
      <path d="M3 12a9 9 0 0 0 9 9 9.75 9.75 0 0 0 6.74-2.74L21 16" />
      <path d="M16 16h5v5" />
    </svg>
  )
}

export function ChevronRightIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="m9 18 6-6-6-6" />
    </svg>
  )
}

export function ChevronDownIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="m6 9 6 6 6-6" />
    </svg>
  )
}

export function ChevronLeftIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="m15 18-6-6 6-6" />
    </svg>
  )
}

export function CheckIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M20 6 9 17l-5-5" />
    </svg>
  )
}

export function CloseIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M18 6 6 18" />
      <path d="m6 6 12 12" />
    </svg>
  )
}

export function LogOutIcon({ size = 18, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
      <polyline points="16 17 21 12 16 7" />
      <line x1="21" x2="9" y1="12" y2="12" />
    </svg>
  )
}

export function MenuIcon({ size = 20, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <line x1="4" x2="20" y1="12" y2="12" />
      <line x1="4" x2="20" y1="6" y2="6" />
      <line x1="4" x2="20" y1="18" y2="18" />
    </svg>
  )
}

export function CalendarIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <rect width="18" height="18" x="3" y="4" rx="2" />
      <line x1="16" x2="16" y1="2" y2="6" />
      <line x1="8" x2="8" y1="2" y2="6" />
      <line x1="3" x2="21" y1="10" y2="10" />
    </svg>
  )
}

export function PhoneIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z" />
    </svg>
  )
}

export function MailIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <rect width="20" height="16" x="2" y="4" rx="2" />
      <path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7" />
    </svg>
  )
}

export function MapPinIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M20 10c0 4.993-5.539 10.193-7.399 11.799a1 1 0 0 1-1.202 0C9.539 20.193 4 14.993 4 10a8 8 0 0 1 16 0" />
      <circle cx="12" cy="10" r="3" />
    </svg>
  )
}

export function ShieldCheckIcon({ size = 18, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z" />
      <path d="m9 12 2 2 4-4" />
    </svg>
  )
}

export function ClockIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <circle cx="12" cy="12" r="10" />
      <polyline points="12 6 12 12 16 14" />
    </svg>
  )
}

export function TestTypeIcon({ size = 18, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z" />
      <polyline points="14 2 14 8 20 8" />
      <path d="m9 15 2 2 4-4" />
    </svg>
  )
}

export function FlagIcon({ size = 18, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z" />
      <line x1="4" x2="4" y1="22" y2="15" />
    </svg>
  )
}

export function InfoIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <circle cx="12" cy="12" r="10" />
      <path d="M12 16v-4" />
      <path d="M12 8h.01" />
    </svg>
  )
}

export function AlertCircleIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <circle cx="12" cy="12" r="10" />
      <line x1="12" x2="12" y1="8" y2="12" />
      <line x1="12" x2="12.01" y1="16" y2="16" />
    </svg>
  )
}

export function LockIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <rect width="18" height="11" x="3" y="11" rx="2" ry="2" />
      <path d="M7 11V7a5 5 0 0 1 10 0v4" />
    </svg>
  )
}

export function FilterIcon({ size = 16, strokeWidth = 2, ...props }: IconProps) {
  return (
    <svg width={size} height={size} strokeWidth={strokeWidth} {...defaultProps} {...props}>
      <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3" />
    </svg>
  )
}
