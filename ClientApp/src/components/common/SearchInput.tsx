import type { CSSProperties } from 'react'
import { Search, X } from 'lucide-react'

interface SearchInputProps {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  ariaLabel?: string
  className?: string
  style?: CSSProperties
  id?: string
  autoFocus?: boolean
  disabled?: boolean
}

export default function SearchInput({
  value,
  onChange,
  placeholder = 'Search...',
  ariaLabel = 'Search',
  className = '',
  style,
  id,
  autoFocus,
  disabled = false,
}: SearchInputProps) {
  return (
    <div className={`search-wrapper ${className}`.trim()} style={style}>
      <span className="search-icon-left text-slate-400" aria-hidden="true">
        <Search size={16} />
      </span>
      <input
        id={id}
        type="text"
        className="search-input"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        aria-label={ariaLabel}
        autoFocus={autoFocus}
        disabled={disabled}
      />
      {value && !disabled && (
        <button
          type="button"
          className="search-clear-btn"
          onClick={() => onChange('')}
          aria-label="Clear search"
        >
          <X size={14} />
        </button>
      )}
    </div>
  )
}
