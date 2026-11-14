import { useState, useEffect } from 'react'
import type { KeyboardEvent } from 'react'
import { User } from 'lucide-react'
import { getPersonPhotoBlob } from '../../services/peopleService'
import { getAccessToken } from '../../utils/authTokenStore'
import { cn } from '@/lib/utils'

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

function PhotoLoadingIndicator() {
  return (
    <span className="flex size-4 items-center justify-center" aria-hidden="true">
      <span className="size-4 animate-spin rounded-full border-2 border-primary border-t-transparent motion-reduce:animate-none" />
    </span>
  )
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

  // Access tokens are held in memory only; never read them from web storage.
  const token = accessToken ?? getAccessToken()

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
  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (!onClick || (event.key !== 'Enter' && event.key !== ' ')) return
    event.preventDefault()
    onClick()
  }

  if (effectiveUrl && !error) {
    return (
      <div
        onClick={onClick}
        onKeyDown={handleKeyDown}
        role={onClick ? 'button' : undefined}
        tabIndex={onClick ? 0 : undefined}
        aria-label={onClick ? alt : undefined}
        aria-busy={loading || undefined}
        className={cn(
          'relative shrink-0 overflow-hidden border border-primary/20 bg-surface shadow-md',
          container,
          onClick && 'cursor-pointer hover:opacity-95 transition-opacity',
          onClick && 'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background',
          className,
        )}
      >
        <img
          src={effectiveUrl}
          alt={onClick ? '' : alt}
          className="size-full object-cover object-top"
          onError={() => setPhotoState((prev) => ({ ...prev, error: true }))}
        />
        {loading && (
          <div className="absolute inset-0 flex items-center justify-center bg-background/60 backdrop-blur-xs">
            <PhotoLoadingIndicator />
          </div>
        )}
      </div>
    )
  }

  // Fallback presentation
  return (
    <div
      onClick={onClick}
      onKeyDown={handleKeyDown}
      role={onClick ? 'button' : 'img'}
      aria-label={alt}
      aria-busy={loading || undefined}
      tabIndex={onClick ? 0 : undefined}
      className={cn(
        'relative flex shrink-0 items-center justify-center border border-primary/20 bg-surface font-semibold text-primary shadow-md',
        container,
        onClick && 'cursor-pointer hover:opacity-95 transition-opacity',
        onClick && 'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background',
        className,
      )}
    >
      {loading ? (
        <PhotoLoadingIndicator />
      ) : initial ? (
        <span className={`${text} select-none`} aria-hidden="true">{initial}</span>
      ) : (
        <User className={`${icon} text-muted-foreground`} aria-hidden="true" />
      )}
    </div>
  )
}
