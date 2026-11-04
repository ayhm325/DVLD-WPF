import type { ReactNode } from 'react'
import {
  Users,
  Car,
  IdCard,
  Globe,
  ClipboardList,
  Flag,
  Award,
  ClipboardCheck,
  UserCheck,
  Fingerprint,
  Gauge,
  ShieldCheck,
  CalendarCheck,
  CalendarClock,
  CalendarDays,
  Clock,
  Compass,
  FileCheck,
  FileText,
  CheckCircle2,
  Globe2,
  MapPin,
  Layers,
  FileCog,
  Sparkles,
  Shield,
  Eye,
  FileSignature,
  User as UserIcon,
  UserPlus,
  RotateCcw,
  Copy,
  Lock,
  ShieldAlert,
} from 'lucide-react'

export type FloatingIconColor = 'blue' | 'emerald' | 'purple' | 'amber' | 'cyan' | 'rose'
export type FloatingIconPosition = 'left' | 'top-right' | 'bottom-right'

export interface FloatingIconItem {
  icon: ReactNode
  color?: FloatingIconColor
  position?: FloatingIconPosition
}

export interface PageHeroConfigItem {
  eyebrow: string
  title: string
  highlight?: string
  description: string
  illustrationTag: string
  illustrationIcon: ReactNode
  floatingIcons: FloatingIconItem[]
  /** Backward-compatible icon */
  icon?: ReactNode
  /** Backward-compatible badge text */
  badgeText?: string
}

export const PAGE_HERO_CONFIG: Record<string, PageHeroConfigItem> = {
  Dashboard: {
    eyebrow: 'CONTROL CENTER',
    title: 'Welcome to',
    highlight: 'DVLD',
    description: 'Manage people, drivers, and licensing operations from one place.',
    illustrationTag: 'DVLD-ID',
    illustrationIcon: <UserIcon className="size-6" />,
    floatingIcons: [
      {
        icon: <Users className="size-7" />,
        color: 'blue',
        position: 'left',
      },
      {
        icon: <Car className="size-6" />,
        color: 'emerald',
        position: 'top-right',
      },
      {
        icon: <IdCard className="size-6" />,
        color: 'purple',
        position: 'bottom-right',
      },
    ],
  },
  People: {
    eyebrow: 'PEOPLE MANAGEMENT',
    title: 'People',
    highlight: 'Directory',
    description: 'Manage and search registered citizen and resident records in the DVLD database.',
    illustrationTag: 'CITIZEN-ID',
    illustrationIcon: <UserCheck className="size-6" />,
    floatingIcons: [
      {
        icon: <Users className="size-7" />,
        color: 'blue',
        position: 'left',
      },
      {
        icon: <UserCheck className="size-6" />,
        color: 'cyan',
        position: 'top-right',
      },
      {
        icon: <Fingerprint className="size-6" />,
        color: 'purple',
        position: 'bottom-right',
      },
    ],
  },
  'Add Person': {
    eyebrow: 'DIRECTORY REGISTRATION',
    title: 'Register New',
    highlight: 'Person',
    description: 'Create a new citizen or resident identity record with demographic and contact details.',
    illustrationTag: 'NEW-CITIZEN',
    illustrationIcon: <UserPlus className="size-6" />,
    floatingIcons: [
      {
        icon: <Users className="size-7" />,
        color: 'blue',
        position: 'left',
      },
      {
        icon: <UserPlus className="size-6" />,
        color: 'cyan',
        position: 'top-right',
      },
      {
        icon: <Fingerprint className="size-6" />,
        color: 'purple',
        position: 'bottom-right',
      },
    ],
  },
  Drivers: {
    eyebrow: 'DRIVER MANAGEMENT',
    title: 'Driver',
    highlight: 'Management',
    description: 'Browse and search registered drivers, review active licenses, and manage driver profiles.',
    illustrationTag: 'DRIVER-REG',
    illustrationIcon: <Car className="size-6" />,
    floatingIcons: [
      {
        icon: <Car className="size-7" />,
        color: 'emerald',
        position: 'left',
      },
      {
        icon: <Gauge className="size-6" />,
        color: 'blue',
        position: 'top-right',
      },
      {
        icon: <IdCard className="size-6" />,
        color: 'purple',
        position: 'bottom-right',
      },
    ],
  },
  Licenses: {
    eyebrow: 'LICENSING OPERATIONS',
    title: 'License',
    highlight: 'Management',
    description: 'View and search issued driving licenses, filter by status, and inspect license validity.',
    illustrationTag: 'DL-PERMIT',
    illustrationIcon: <IdCard className="size-6" />,
    floatingIcons: [
      {
        icon: <IdCard className="size-7" />,
        color: 'purple',
        position: 'left',
      },
      {
        icon: <ShieldCheck className="size-6" />,
        color: 'emerald',
        position: 'top-right',
      },
      {
        icon: <CalendarCheck className="size-6" />,
        color: 'cyan',
        position: 'bottom-right',
      },
    ],
  },
  'Person License History': {
    eyebrow: 'DRIVER PORTFOLIO',
    title: 'Person License',
    highlight: 'History',
    description: 'Comprehensive driving license portfolio, card credentials, and historical records for this person.',
    illustrationTag: 'LICENSE-HISTORY',
    illustrationIcon: <IdCard className="size-6" />,
    floatingIcons: [
      {
        icon: <IdCard className="size-7" />,
        color: 'cyan',
        position: 'left',
      },
      {
        icon: <Award className="size-6" />,
        color: 'blue',
        position: 'top-right',
      },
      {
        icon: <Globe className="size-6" />,
        color: 'purple',
        position: 'bottom-right',
      },
    ],
  },
  'Local Applications': {
    eyebrow: 'LICENSING OPERATIONS',
    title: 'Local Driving License',
    highlight: 'Applications',
    description: 'Track applications, review exam progress, and start a new driving license request.',
    illustrationTag: 'APP-RECORD',
    illustrationIcon: <ClipboardList className="size-6" />,
    floatingIcons: [
      {
        icon: <ClipboardList className="size-7" />,
        color: 'amber',
        position: 'left',
      },
      {
        icon: <FileText className="size-6" />,
        color: 'blue',
        position: 'top-right',
      },
      {
        icon: <CheckCircle2 className="size-6" />,
        color: 'emerald',
        position: 'bottom-right',
      },
    ],
  },
  'Test Appointments': {
    eyebrow: 'EXAMINATION OPERATIONS',
    title: 'Test',
    highlight: 'Appointments',
    description: 'Track, schedule, and review driving examination appointments across all lifecycle states in Jordan facility time.',
    illustrationTag: 'TEST-APPT',
    illustrationIcon: <CalendarDays className="size-6" />,
    floatingIcons: [
      {
        icon: <Clock className="size-7" />,
        color: 'cyan',
        position: 'left',
      },
      {
        icon: <ClipboardCheck className="size-6" />,
        color: 'emerald',
        position: 'top-right',
      },
      {
        icon: <CalendarDays className="size-6" />,
        color: 'blue',
        position: 'bottom-right',
      },
    ],
  },
  'Appointment Details': {
    eyebrow: 'EXAMINATION OPERATIONS / AUDIT',
    title: 'Appointment',
    highlight: 'Details',
    description: 'Comprehensive driving examination record and lifecycle audit in Jordan facility time.',
    illustrationTag: 'APPT-AUDIT',
    illustrationIcon: <CalendarDays className="size-6" />,
    floatingIcons: [
      {
        icon: <Clock className="size-7" />,
        color: 'cyan',
        position: 'left',
      },
      {
        icon: <Award className="size-6" />,
        color: 'emerald',
        position: 'top-right',
      },
      {
        icon: <CalendarDays className="size-6" />,
        color: 'blue',
        position: 'bottom-right',
      },
    ],
  },
  'Manage Test Appointments': {
    eyebrow: 'EXAMINATION OPERATIONS',
    title: 'Test Appointments',
    highlight: 'History',
    description: 'Complete chronological examination history and appointment management across Vision, Written, and Practical tests.',
    illustrationTag: 'APPT-TIMELINE',
    illustrationIcon: <CalendarClock className="size-6" />,
    floatingIcons: [
      {
        icon: <Clock className="size-7" />,
        color: 'cyan',
        position: 'left',
      },
      {
        icon: <CalendarClock className="size-6" />,
        color: 'purple',
        position: 'top-right',
      },
      {
        icon: <CalendarDays className="size-6" />,
        color: 'blue',
        position: 'bottom-right',
      },
    ],
  },
  'Written Test Appointments': {
    eyebrow: 'EXAMINATION OPERATIONS',
    title: 'Written Test',
    highlight: 'Appointments',
    description: 'Inspect and manage official examination appointments, historical attempts, and outcome logs.',
    illustrationTag: 'WRITTEN-APPT',
    illustrationIcon: <FileSignature className="size-6" />,
    floatingIcons: [
      {
        icon: <Clock className="size-7" />,
        color: 'cyan',
        position: 'left',
      },
      {
        icon: <FileSignature className="size-6" />,
        color: 'purple',
        position: 'top-right',
      },
      {
        icon: <CalendarDays className="size-6" />,
        color: 'blue',
        position: 'bottom-right',
      },
    ],
  },
  'Take Examination': {
    eyebrow: 'EXAMINATION OPERATIONS',
    title: 'Take',
    highlight: 'Examination',
    description: 'Record the official examination outcome, evaluation decisions, and examiner notes in Jordan facility time.',
    illustrationTag: 'TAKE-EXAM',
    illustrationIcon: <Award className="size-6" />,
    floatingIcons: [
      {
        icon: <Award className="size-7" />,
        color: 'cyan',
        position: 'left',
      },
      {
        icon: <CheckCircle2 className="size-6" />,
        color: 'emerald',
        position: 'top-right',
      },
      {
        icon: <Clock className="size-6" />,
        color: 'blue',
        position: 'bottom-right',
      },
    ],
  },
  'Schedule Appointment': {
    eyebrow: 'EXAMINATION OPERATIONS',
    title: 'Schedule Test',
    highlight: 'Appointment',
    description: 'Reserve an official driving test slot in accordance with Jordan Licensing Department regulations.',
    illustrationTag: 'APPT-SCHEDULE',
    illustrationIcon: <CalendarCheck className="size-6" />,
    floatingIcons: [
      {
        icon: <CalendarCheck className="size-7" />,
        color: 'cyan',
        position: 'left',
      },
      {
        icon: <Clock className="size-6" />,
        color: 'emerald',
        position: 'top-right',
      },
      {
        icon: <ClipboardCheck className="size-6" />,
        color: 'blue',
        position: 'bottom-right',
      },
    ],
  },
  'International Licenses': {
    eyebrow: 'INTERNATIONAL LICENSING',
    title: 'International License',
    highlight: 'Directory',
    description: 'Review issued international driving licenses or prepare a new request for an active local license.',
    illustrationTag: 'INTL-PERMIT',
    illustrationIcon: <Globe className="size-6" />,
    floatingIcons: [
      {
        icon: <Globe className="size-7" />,
        color: 'cyan',
        position: 'left',
      },
      {
        icon: <Compass className="size-6" />,
        color: 'purple',
        position: 'top-right',
      },
      {
        icon: <FileCheck className="size-6" />,
        color: 'blue',
        position: 'bottom-right',
      },
    ],
  },
  'Issue International License': {
    eyebrow: 'LICENSE MANAGEMENT / ISSUE',
    title: 'Issue International',
    highlight: 'License',
    description: 'Find an eligible local driving license, review its details, then issue the international permit.',
    illustrationTag: 'ISSUE-INTL',
    illustrationIcon: <Globe className="size-6" />,
    floatingIcons: [
      {
        icon: <Globe className="size-7" />,
        color: 'cyan',
        position: 'left',
      },
      {
        icon: <Award className="size-6" />,
        color: 'emerald',
        position: 'top-right',
      },
      {
        icon: <Sparkles className="size-6" />,
        color: 'purple',
        position: 'bottom-right',
      },
    ],
  },
  Countries: {
    eyebrow: 'SYSTEM CONFIGURATION',
    title: 'Countries',
    highlight: 'Directory',
    description: 'Browse and search nationality country records configured in the DVLD system.',
    illustrationTag: 'NATION-CFG',
    illustrationIcon: <Flag className="size-6" />,
    floatingIcons: [
      {
        icon: <Flag className="size-7" />,
        color: 'blue',
        position: 'left',
      },
      {
        icon: <Globe2 className="size-6" />,
        color: 'cyan',
        position: 'top-right',
      },
      {
        icon: <MapPin className="size-6" />,
        color: 'emerald',
        position: 'bottom-right',
      },
    ],
  },
  'Application Types': {
    eyebrow: 'SYSTEM CONFIGURATION',
    title: 'Application',
    highlight: 'Types',
    description: 'View and manage application type names, processing requirements, and service fees.',
    illustrationTag: 'TYPE-CFG',
    illustrationIcon: <Layers className="size-6" />,
    floatingIcons: [
      {
        icon: <Layers className="size-7" />,
        color: 'purple',
        position: 'left',
      },
      {
        icon: <FileCog className="size-6" />,
        color: 'blue',
        position: 'top-right',
      },
      {
        icon: <Sparkles className="size-6" />,
        color: 'emerald',
        position: 'bottom-right',
      },
    ],
  },
  'License Classes': {
    eyebrow: 'SYSTEM CONFIGURATION',
    title: 'License',
    highlight: 'Classes',
    description: 'Browse license categories, minimum age requirements, validity periods, and associated fees.',
    illustrationTag: 'CLASS-CFG',
    illustrationIcon: <Award className="size-6" />,
    floatingIcons: [
      {
        icon: <Award className="size-7" />,
        color: 'amber',
        position: 'left',
      },
      {
        icon: <Car className="size-6" />,
        color: 'emerald',
        position: 'top-right',
      },
      {
        icon: <Shield className="size-6" />,
        color: 'purple',
        position: 'bottom-right',
      },
    ],
  },
  'Test Types': {
    eyebrow: 'SYSTEM CONFIGURATION',
    title: 'Test',
    highlight: 'Types',
    description: 'Manage test names, examination requirements, and official fee schedules.',
    illustrationTag: 'TEST-CFG',
    illustrationIcon: <ClipboardCheck className="size-6" />,
    floatingIcons: [
      {
        icon: <ClipboardCheck className="size-7" />,
        color: 'emerald',
        position: 'left',
      },
      {
        icon: <Eye className="size-6" />,
        color: 'blue',
        position: 'top-right',
      },
      {
        icon: <FileSignature className="size-6" />,
        color: 'purple',
        position: 'bottom-right',
      },
    ],
  },
  'Renew License': {
    eyebrow: 'LICENSE LIFECYCLE',
    title: 'Renew Driving',
    highlight: 'License',
    description: 'Renew an expired driving license according to Jordan facility time rules and official class validity lengths.',
    illustrationTag: 'DL-RENEWAL',
    illustrationIcon: <RotateCcw className="size-6" />,
    floatingIcons: [
      {
        icon: <RotateCcw className="size-7" />,
        color: 'cyan',
        position: 'left',
      },
      {
        icon: <IdCard className="size-6" />,
        color: 'blue',
        position: 'top-right',
      },
      {
        icon: <CalendarCheck className="size-6" />,
        color: 'emerald',
        position: 'bottom-right',
      },
    ],
  },
  'Replace License': {
    eyebrow: 'LICENSE LIFECYCLE',
    title: 'Replace Driving',
    highlight: 'License',
    description: 'Issue a replacement driving license for lost or damaged credentials with original expiration date retention.',
    illustrationTag: 'DL-REPLACE',
    illustrationIcon: <Copy className="size-6" />,
    floatingIcons: [
      {
        icon: <Copy className="size-7" />,
        color: 'purple',
        position: 'left',
      },
      {
        icon: <IdCard className="size-6" />,
        color: 'cyan',
        position: 'top-right',
      },
      {
        icon: <ShieldCheck className="size-6" />,
        color: 'blue',
        position: 'bottom-right',
      },
    ],
  },
  'Detain License': {
    eyebrow: 'ENFORCEMENT & COMPLIANCE',
    title: 'Detain Driving',
    highlight: 'License',
    description: 'Record an official license detention, set fine fees, and immediately transition the license to inactive state.',
    illustrationTag: 'DL-DETAIN',
    illustrationIcon: <ShieldAlert className="size-6" />,
    floatingIcons: [
      {
        icon: <ShieldAlert className="size-7" />,
        color: 'rose',
        position: 'left',
      },
      {
        icon: <Lock className="size-6" />,
        color: 'amber',
        position: 'top-right',
      },
      {
        icon: <IdCard className="size-6" />,
        color: 'blue',
        position: 'bottom-right',
      },
    ],
  },
  'Detained Licenses': {
    eyebrow: 'COMPLIANCE & RECOVERY',
    title: 'Detained Licenses',
    highlight: 'Registry',
    description: 'Manage active and released detained driving licenses, monitor outstanding fine fees, and process license releases.',
    illustrationTag: 'DL-DETAINED-LIST',
    illustrationIcon: <Lock className="size-6" />,
    floatingIcons: [
      {
        icon: <Lock className="size-7" />,
        color: 'amber',
        position: 'left',
      },
      {
        icon: <ShieldAlert className="size-6" />,
        color: 'rose',
        position: 'top-right',
      },
      {
        icon: <CheckCircle2 className="size-6" />,
        color: 'emerald',
        position: 'bottom-right',
      },
    ],
  },
}
