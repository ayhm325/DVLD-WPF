import { useState, useEffect, useMemo, useRef } from 'react'
import {
  Search,
  LayoutDashboard,
  Users,
  Car,
  CreditCard,
  FileText,
  Globe,
  Layers,
  Award,
  ClipboardCheck,
  Flag,
  CornerDownLeft,
  X,
} from 'lucide-react'
import { isPagePermitted } from '../../utils/permissions'

interface CommandItem {
  id: string
  page: string
  title: string
  description: string
  category: 'Pages' | 'Administration'
  icon: typeof LayoutDashboard
  keywords: string[]
}

const COMMAND_ITEMS: CommandItem[] = [
  {
    id: 'dashboard',
    page: 'Dashboard',
    title: 'Dashboard Overview',
    description: 'System statistics, quick metrics, and real-time operations',
    category: 'Pages',
    icon: LayoutDashboard,
    keywords: ['dashboard', 'stats', 'home', 'metrics', 'overview'],
  },
  {
    id: 'people',
    page: 'People',
    title: 'People Registry',
    description: 'Manage citizen records, personal details, and profiles',
    category: 'Pages',
    icon: Users,
    keywords: ['people', 'person', 'citizens', 'national', 'id', 'registry'],
  },
  {
    id: 'drivers',
    page: 'Drivers',
    title: 'Driver Records',
    description: 'View registered drivers and active driving licenses',
    category: 'Pages',
    icon: Car,
    keywords: ['drivers', 'driver', 'motorist', 'chauffeur'],
  },
  {
    id: 'licenses',
    page: 'Licenses',
    title: 'Driver Licenses',
    description: 'Issued driving credentials, renewals, and replacements',
    category: 'Pages',
    icon: CreditCard,
    keywords: ['licenses', 'license', 'driving license', 'permit'],
  },
  {
    id: 'local-applications',
    page: 'Local Applications',
    title: 'Local Driving Applications',
    description: 'New license applications, test appointments, and issuance',
    category: 'Pages',
    icon: FileText,
    keywords: ['applications', 'local', 'apply', 'test', 'exam'],
  },
  {
    id: 'international-licenses',
    page: 'International Licenses',
    title: 'International Driving Permits',
    description: 'Issue and verify international travel driving permits',
    category: 'Pages',
    icon: Globe,
    keywords: ['international', 'permit', 'idp', 'foreign', 'travel'],
  },
  {
    id: 'application-types',
    page: 'Application Types',
    title: 'Application Types & Fees',
    description: 'Manage application categories, requirements, and fee schedules',
    category: 'Administration',
    icon: Layers,
    keywords: ['application types', 'fees', 'pricing', 'types'],
  },
  {
    id: 'license-classes',
    page: 'License Classes',
    title: 'License Classes & Categories',
    description: 'Configure vehicle categories, validity lengths, and minimum ages',
    category: 'Administration',
    icon: Award,
    keywords: ['license classes', 'classes', 'categories', 'vehicles'],
  },
  {
    id: 'test-types',
    page: 'Test Types',
    title: 'Test Types & Procedures',
    description: 'Vision, Theory, and Practical driving examination settings',
    category: 'Administration',
    icon: ClipboardCheck,
    keywords: ['tests', 'test types', 'vision', 'theory', 'practical', 'exam'],
  },
  {
    id: 'countries',
    page: 'Countries',
    title: 'Countries & Nationalities',
    description: 'Lookup country list, international codes, and citizenship data',
    category: 'Administration',
    icon: Flag,
    keywords: ['countries', 'country', 'nations', 'citizenship'],
  },
]

interface CommandPaletteProps {
  isOpen: boolean
  onClose: () => void
  onNavigate: (page: string) => void
  userRole?: string
}

export default function CommandPalette({
  isOpen,
  onClose,
  onNavigate,
  userRole,
}: CommandPaletteProps) {
  const [query, setQuery] = useState('')
  const [selectedIndex, setSelectedIndex] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)
  const listRef = useRef<HTMLDivElement>(null)

  // Filter items strictly by role permissions and search query
  const filteredItems = useMemo(() => {
    const permitted = COMMAND_ITEMS.filter((item) =>
      isPagePermitted(item.page, userRole),
    )

    const normalizedQuery = query.trim().toLowerCase()
    if (!normalizedQuery) return permitted

    return permitted.filter((item) => {
      const matchTitle = item.title.toLowerCase().includes(normalizedQuery)
      const matchDesc = item.description.toLowerCase().includes(normalizedQuery)
      const matchKeywords = item.keywords.some((k) =>
        k.toLowerCase().includes(normalizedQuery),
      )
      return matchTitle || matchDesc || matchKeywords
    })
  }, [query, userRole])

  const handleQueryChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setQuery(e.target.value)
    setSelectedIndex(0)
  }

  const handleClose = () => {
    setQuery('')
    setSelectedIndex(0)
    onClose()
  }

  // Focus input automatically and immediately when opened
  useEffect(() => {
    if (isOpen) {
      inputRef.current?.focus()
      const raf = requestAnimationFrame(() => {
        inputRef.current?.focus()
        inputRef.current?.select()
      })
      const timer = setTimeout(() => {
        inputRef.current?.focus()
      }, 0)
      return () => {
        cancelAnimationFrame(raf)
        clearTimeout(timer)
      }
    }
  }, [isOpen])

  // Keyboard navigation within the modal
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if ((e.ctrlKey || e.metaKey) && (e.key.toLowerCase() === 'k' || e.code === 'KeyK')) {
      e.preventDefault()
      e.stopPropagation()
      handleClose()
      return
    }

    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setSelectedIndex((prev) =>
        prev < filteredItems.length - 1 ? prev + 1 : 0,
      )
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setSelectedIndex((prev) =>
        prev > 0 ? prev - 1 : filteredItems.length - 1,
      )
    } else if (e.key === 'Enter') {
      e.preventDefault()
      if (filteredItems[selectedIndex]) {
        onNavigate(filteredItems[selectedIndex].page)
        handleClose()
      }
    } else if (e.key === 'Escape') {
      e.preventDefault()
      handleClose()
    }
  }

  // Scroll active item into view
  useEffect(() => {
    if (listRef.current) {
      const activeElement = listRef.current.querySelector(
        `[data-index="${selectedIndex}"]`,
      ) as HTMLElement | null
      if (activeElement) {
        activeElement.scrollIntoView({ block: 'nearest' })
      }
    }
  }, [selectedIndex])

  if (!isOpen) return null

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center pt-20 sm:pt-28 px-4 bg-black/75 backdrop-blur-sm animate-in fade-in duration-150"
      onClick={handleClose}
      role="dialog"
      aria-modal="true"
      aria-label="Command Navigation Palette"
    >
      <div
        className="w-full max-w-xl overflow-hidden rounded-2xl bg-[#101A2D] border border-[#263750] shadow-2xl ring-1 ring-white/10 animate-in zoom-in-95 duration-150"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
      >
        {/* Search Input Bar */}
        <div className="flex items-center gap-3 px-4 py-3.5 border-b border-[#263750] bg-[#162238]/60">
          <Search className="size-5 text-blue-400 shrink-0" aria-hidden="true" />
          <input
            ref={inputRef}
            type="text"
            autoFocus
            className="flex-1 bg-transparent border-0 outline-none text-slate-100 placeholder:text-slate-400 text-sm font-medium"
            placeholder="Type a command or jump to page... (e.g. People, Licenses, Settings)"
            value={query}
            onChange={handleQueryChange}
            autoComplete="off"
            spellCheck="false"
          />
          {query ? (
            <button
              type="button"
              onClick={() => {
                setQuery('')
                setSelectedIndex(0)
              }}
              className="p-1 rounded-md text-slate-400 hover:text-white hover:bg-[#1C2B45] transition-colors"
              aria-label="Clear search"
            >
              <X className="size-4" />
            </button>
          ) : (
            <kbd className="hidden sm:inline-flex items-center px-2 py-0.5 text-[10px] font-semibold text-slate-400 bg-[#0B1120] border border-[#263750] rounded">
              ESC
            </kbd>
          )}
        </div>

        {/* Results List */}
        <div
          ref={listRef}
          className="max-h-80 overflow-y-auto p-2 scrollbar-thin scrollbar-thumb-[#263750] scrollbar-track-transparent"
        >
          {filteredItems.length === 0 ? (
            <div className="py-12 px-4 text-center">
              <p className="text-sm font-medium text-slate-300">
                No matching pages or actions found
              </p>
              <p className="text-xs text-slate-400 mt-1">
                Try searching for &quot;Dashboard&quot;, &quot;People&quot;, or &quot;Fees&quot;
              </p>
            </div>
          ) : (
            <div className="space-y-1">
              {filteredItems.map((item, index) => {
                const IconComponent = item.icon
                const isSelected = index === selectedIndex

                return (
                  <button
                    key={item.id}
                    type="button"
                    data-index={index}
                    onClick={() => {
                      onNavigate(item.page)
                      handleClose()
                    }}
                    onMouseEnter={() => setSelectedIndex(index)}
                    className={`w-full flex items-center justify-between gap-3 px-3 py-2.5 rounded-xl text-left transition-all duration-100 ${
                      isSelected
                        ? 'bg-[#1C2B45] text-white shadow-sm ring-1 ring-blue-500/30'
                        : 'text-slate-300 hover:bg-[#162238]/80'
                    }`}
                  >
                    <div className="flex items-center gap-3 min-w-0">
                      <div
                        className={`flex size-9 shrink-0 items-center justify-center rounded-lg border transition-colors ${
                          isSelected
                            ? 'bg-blue-600 text-white border-blue-400/40 shadow-sm'
                            : 'bg-[#162238] text-blue-400 border-[#263750]'
                        }`}
                      >
                        <IconComponent className="size-4.5" />
                      </div>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-bold truncate text-white">
                            {item.title}
                          </span>
                          <span
                            className={`text-[10px] font-semibold px-2 py-0.5 rounded-full border ${
                              item.category === 'Administration'
                                ? 'bg-amber-500/15 text-amber-300 border-amber-500/30'
                                : 'bg-blue-500/15 text-blue-300 border-blue-500/30'
                            }`}
                          >
                            {item.category}
                          </span>
                        </div>
                        <p className="text-xs text-slate-400 truncate mt-0.5">
                          {item.description}
                        </p>
                      </div>
                    </div>

                    <div className="shrink-0 flex items-center gap-2">
                      {isSelected && (
                        <span className="flex items-center gap-1 text-[11px] font-medium text-blue-300">
                          <span>Select</span>
                          <CornerDownLeft className="size-3" />
                        </span>
                      )}
                    </div>
                  </button>
                )
              })}
            </div>
          )}
        </div>

        {/* Command Palette Footer */}
        <div className="flex items-center justify-between px-4 py-2.5 bg-[#0B1120] border-t border-[#263750] text-[11px] text-slate-400">
          <div className="flex items-center gap-3">
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 bg-[#162238] border border-[#263750] rounded text-[10px] text-slate-300">
                ↑
              </kbd>
              <kbd className="px-1.5 py-0.5 bg-[#162238] border border-[#263750] rounded text-[10px] text-slate-300">
                ↓
              </kbd>
              <span>to navigate</span>
            </span>
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 bg-[#162238] border border-[#263750] rounded text-[10px] text-slate-300">
                ↵
              </kbd>
              <span>to select</span>
            </span>
          </div>
          <span className="text-slate-400">
            Press <kbd className="px-1 bg-[#162238] border border-[#263750] rounded text-[10px]">Ctrl</kbd> + <kbd className="px-1 bg-[#162238] border border-[#263750] rounded text-[10px]">K</kbd> anywhere
          </span>
        </div>
      </div>
    </div>
  )
}
