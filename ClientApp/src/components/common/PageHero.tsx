import type { ReactNode } from 'react'
import './PageHero.css'
import { PAGE_HERO_CONFIG, type PageHeroConfigItem } from './pageHeroConfig'

export type PageHeroProps = {
  pageId?: string
  eyebrow?: string
  title?: string
  description?: string
  icon?: ReactNode
  badgeText?: string
  className?: string
}

/**
 * Reusable dark navy PageHero banner for all standalone DVLD pages.
 * Displays centralized eyebrow, heading, description,
 * and a decorative right-aligned emblem/icon.
 * Action buttons must be placed in content panel headers or toolbars,
 * not inside the hero banner.
 */
export default function PageHero({
  pageId,
  eyebrow,
  title,
  description,
  icon,
  badgeText,
  className = '',
}: PageHeroProps) {
  const config: PageHeroConfigItem | undefined = pageId ? PAGE_HERO_CONFIG[pageId] : undefined

  const finalEyebrow = eyebrow ?? config?.eyebrow ?? ''
  const finalTitle = title ?? config?.title ?? ''
  const finalDescription = description ?? config?.description ?? ''
  const finalIcon = icon ?? config?.icon
  const finalBadgeText = badgeText ?? config?.badgeText

  return (
    <div className={`page-hero ${className}`.trim()}>
      <div className="page-hero-main">
        {finalEyebrow && <p className="page-hero-eyebrow">{finalEyebrow}</p>}
        <h2 className="page-hero-title">{finalTitle}</h2>
        {finalDescription && (
          <p className="page-hero-description">{finalDescription}</p>
        )}
      </div>

      {(finalIcon || finalBadgeText) && (
        <div className="page-hero-mark" aria-hidden="true">
          {finalBadgeText ?? finalIcon}
        </div>
      )}
    </div>
  )
}
