import { useState, useEffect } from 'react'
import { User } from 'lucide-react'
import { getPersonPhotoBlob } from '../../services/peopleService'

export type PersonPhotoSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl' | '2xl'

export interface PersonPhotoProps {
  personId?: number | null
  hasPhoto?: boolean
  photoKey?: string | null
  localPreviewUrl?: string | null
  fullName?: string | null
  size?: PersonPhotoSize
  className?: string
  alt?: string
  accessToken?: string | null
  onClick?: () => void
}

const sizeClasses: Record<PersonPhotoSize, { container: string; icon: string; text: string }> = {
  xs: { container: 'size-7 rounded-full', icon: 'size-3.5', text: 'text-xs' },
  sm: { container: 'size-9 rounded-full', icon: 'size-4', text: 'text-sm' },
  md: { container: 'size-12 rounded-full', icon: 'size-6', text: 'text-base' },
  lg: { container: 'size-20 rounded-xl', icon: 'size-9', text: 'text-2xl' },
  xl: { container: 'size-28 rounded-2xl', icon: 'size-12', text: 'text-3xl' },
  '2xl': {
    container: 'w-[180px] h-[210px] sm:w-[208px] sm:h-[240px] rounded-2xl',
    icon: 'size-20',
    text: 'text-5xl',
  },
}

export default function PersonPhoto({
  personId,
  hasPhoto,
  photoKey,
  localPreviewUrl,
  fullName,
  size = 'md',
  className = '',
  alt = 'Person photo',
  accessToken,
  onClick,
}: PersonPhotoProps) {
  const [photoState, setPhotoState] = useState<{
    personId: number | null
    photoKey?: string | null
    url: string | null
    error: boolean
  }>({
    personId: null,
    photoKey: null,
    url: null,
    error: false,
  })

  // Use token from props, localStorage, or sessionStorage (AuthSession)
  const token =
    accessToken ??
    (typeof window !== 'undefined'
      ? localStorage.getItem('access_token') ||
        localStorage.getItem('dvld_token') ||
        (() => {
          try {
            const raw = sessionStorage.getItem('dvld_auth_session')
            return raw ? (JSON.parse(raw) as { accessToken?: string }).accessToken ?? null : null
          } catch {
            return null
          }
        })()
      : null)

  const shouldFetch = Boolean(token && personId && hasPhoto !== false && !localPreviewUrl)
  const isLoadedForCurrent =
    photoState.personId === personId && (photoKey === undefined || photoState.photoKey === photoKey)
  const loading = shouldFetch && !isLoadedForCurrent && !photoState.error
  const blobUrl = shouldFetch && isLoadedForCurrent ? photoState.url : null
  const error = isLoadedForCurrent ? photoState.error : false

  useEffect(() => {
    if (!shouldFetch || !personId || !token) {
      return
    }

    let isMounted = true
    let currentObjectUrl: string | null = null

    getPersonPhotoBlob(token, personId, photoKey)
      .then((blob) => {
        if (!isMounted) return
        currentObjectUrl = URL.createObjectURL(blob)
        setPhotoState({
          personId,
          photoKey: photoKey ?? null,
          url: currentObjectUrl,
          error: false,
        })
      })
      .catch(() => {
        if (!isMounted) return
        setPhotoState({
          personId,
          photoKey: photoKey ?? null,
          url: null,
          error: true,
        })
      })

    return () => {
      isMounted = false
      if (currentObjectUrl) {
        URL.revokeObjectURL(currentObjectUrl)
      }
    }
  }, [shouldFetch, personId, photoKey, token])

  const effectiveUrl = localPreviewUrl || blobUrl
  const initial = fullName?.trim() ? fullName.trim().charAt(0).toUpperCase() : null
  const { container, icon, text } = sizeClasses[size]

  if (effectiveUrl && !error) {
    return (
      <div
        onClick={onClick}
        className={`relative shrink-0 overflow-hidden border border-cyan-500/20 bg-slate-900/60 shadow-md ${container} ${
          onClick ? 'cursor-pointer hover:opacity-95 transition-opacity' : ''
        } ${className}`}
      >
        <img
          src={effectiveUrl}
          alt={alt}
          className="size-full object-cover object-top"
          onError={() => setPhotoState((prev) => ({ ...prev, error: true }))}
        />
        {loading && (
          <div className="absolute inset-0 flex items-center justify-center bg-slate-950/40 backdrop-blur-xs">
            <div className="size-4 animate-spin rounded-full border-2 border-cyan-400 border-t-transparent" />
          </div>
        )}
      </div>
    )
  }

  // Fallback presentation
  return (
    <div
      onClick={onClick}
      className={`relative flex shrink-0 items-center justify-center border border-cyan-500/20 bg-slate-900/60 font-semibold text-cyan-400 shadow-md ${container} ${
        onClick ? 'cursor-pointer hover:opacity-95 transition-opacity' : ''
      } ${className}`}
    >
      {loading ? (
        <div className="size-4 animate-spin rounded-full border-2 border-cyan-400 border-t-transparent" />
      ) : initial ? (
        <span className={`${text} select-none`}>{initial}</span>
      ) : (
        <User className={`${icon} text-slate-500`} />
      )}
    </div>
  )
}
