import type { ReactNode } from 'react'
import {
  DashboardIcon,
  UsersIcon,
  DriversIcon,
  LicenseIcon,
  GlobeIcon,
  ApplicationsIcon,
  FlagIcon,
  AwardIcon,
  TestTypeIcon,
} from './Icons'

export type PageHeroConfigItem = {
  eyebrow: string
  title: string
  description: string
  icon: ReactNode
  badgeText?: string
}

export const PAGE_HERO_CONFIG: Record<string, PageHeroConfigItem> = {
  Dashboard: {
    eyebrow: 'CONTROL CENTER',
    title: 'Welcome to DVLD',
    description: 'Manage people, drivers, and licensing operations from one place.',
    icon: <DashboardIcon size={34} />,
    badgeText: 'D',
  },
  People: {
    eyebrow: 'PEOPLE MANAGEMENT',
    title: 'People Management',
    description: 'Manage and search registered people records in the DVLD database.',
    icon: <UsersIcon size={34} />,
  },
  Drivers: {
    eyebrow: 'DRIVER MANAGEMENT',
    title: 'Driver Management',
    description: 'Browse and search registered drivers, review active licenses, and manage driver profiles.',
    icon: <DriversIcon size={34} />,
  },
  Licenses: {
    eyebrow: 'LICENSING OPERATIONS',
    title: 'License Management',
    description: 'View and search issued driving licenses, filter by status, and inspect license validity.',
    icon: <LicenseIcon size={34} />,
  },
  'International Licenses': {
    eyebrow: 'INTERNATIONAL LICENSING',
    title: 'International License Management',
    description: 'Review issued international driving licenses or prepare a new request for an active local license.',
    icon: <GlobeIcon size={34} />,
  },
  'Local Applications': {
    eyebrow: 'LICENSING OPERATIONS',
    title: 'Local Driving License Applications',
    description: 'Track applications, review progress, and start a new driving license request.',
    icon: <ApplicationsIcon size={34} />,
    badgeText: 'LD',
  },
  Countries: {
    eyebrow: 'SYSTEM CONFIGURATION',
    title: 'Countries',
    description: 'Browse and search nationality country records configured in the DVLD system.',
    icon: <FlagIcon size={34} />,
  },
  'Application Types': {
    eyebrow: 'SYSTEM CONFIGURATION',
    title: 'Application Types',
    description: 'View and manage application type names, processing requirements, and service fees.',
    icon: <ApplicationsIcon size={34} />,
  },
  'License Classes': {
    eyebrow: 'SYSTEM CONFIGURATION',
    title: 'License Classes',
    description: 'Browse license categories, minimum age requirements, validity periods, and associated fees.',
    icon: <AwardIcon size={34} />,
  },
  'Test Types': {
    eyebrow: 'SYSTEM CONFIGURATION',
    title: 'Test Types',
    description: 'Manage test names, descriptions, and official examination fee schedules.',
    icon: <TestTypeIcon size={34} />,
  },
}
