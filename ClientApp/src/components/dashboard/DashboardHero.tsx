import PageHero from '../common/PageHero'

export interface DashboardHeroProps {
  eyebrow?: string
  titlePrefix?: string
  titleHighlight?: string
  description?: string
  className?: string
}

export default function DashboardHero({
  eyebrow,
  titlePrefix,
  titleHighlight,
  description,
  className = '',
}: DashboardHeroProps) {
  return (
    <PageHero
      pageId="Dashboard"
      eyebrow={eyebrow}
      title={titlePrefix}
      highlight={titleHighlight}
      description={description}
      className={className}
    />
  )
}
