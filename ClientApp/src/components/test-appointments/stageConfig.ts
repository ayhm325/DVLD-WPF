import type { LucideIcon } from 'lucide-react'
import { Eye, FileSignature, Car } from 'lucide-react'

export interface StageItemConfig {
  id: 1 | 2 | 3
  name: string
  shortName: string
  icon: LucideIcon
  requiredPasses: number
  color: string
  bgColor: string
  borderColor: string
}

export const STAGE_CONFIG: Record<1 | 2 | 3, StageItemConfig> = {
  1: {
    id: 1,
    name: 'Vision Test',
    shortName: 'Vision',
    icon: Eye,
    requiredPasses: 0,
    color: 'text-cyan-400',
    bgColor: 'bg-cyan-500/10',
    borderColor: 'border-cyan-500/30',
  },
  2: {
    id: 2,
    name: 'Written (Theory) Test',
    shortName: 'Written',
    icon: FileSignature,
    requiredPasses: 1,
    color: 'text-indigo-400',
    bgColor: 'bg-indigo-500/10',
    borderColor: 'border-indigo-500/30',
  },
  3: {
    id: 3,
    name: 'Practical (Street) Test',
    shortName: 'Street',
    icon: Car,
    requiredPasses: 2,
    color: 'text-emerald-400',
    bgColor: 'bg-emerald-500/10',
    borderColor: 'border-emerald-500/30',
  },
}
