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
  action?: ReactNode
  className?: string
  children?: ReactNode
}

/**
 * Reusable dark navy PageHero banner for all standalone DVLD pages.
 * Displays centralized eyebrow, heading, description, action slots,
 * and a decorative right-aligned emblem/icon.
 */
export default function PageHero({
  pageId,
  eyebrow,
  title,
  description,
  icon,
  badgeText,
  action,
  className = '',
  children,
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
        {action && <div className="page-hero-actions">{action}</div>}
        {children}
      </div>

      {(finalIcon || finalBadgeText) && (
        <div className="page-hero-mark" aria-hidden="true">
          {finalBadgeText ?? finalIcon}
        </div>
      )}
    </div>
  )
}
