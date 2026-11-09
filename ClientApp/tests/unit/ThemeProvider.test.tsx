import '@testing-library/jest-dom/vitest'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { ThemeProvider } from '../../src/theme/ThemeProvider'
import { useTheme } from '../../src/theme/useTheme'
import ThemeToggle from '../../src/components/auth/ThemeToggle'

function ThemeControls() {
  const { theme, toggleTheme } = useTheme()

  return (
    <button type="button" onClick={toggleTheme}>
      {theme}
    </button>
  )
}

describe('DVLD theme foundation', () => {
  beforeEach(() => {
    window.localStorage.clear()
    document.documentElement.removeAttribute('data-theme')
  })

  afterEach(() => {
    cleanup()
  })

  it('restores an explicitly saved theme and persists a later toggle', async () => {
    window.localStorage.setItem('dvld_theme', 'light')
    const user = userEvent.setup()

    render(
      <ThemeProvider>
        <ThemeControls />
      </ThemeProvider>,
    )

    expect(screen.getByRole('button')).toHaveTextContent('light')
    expect(document.documentElement).toHaveAttribute('data-theme', 'light')

    await user.click(screen.getByRole('button'))

    expect(screen.getByRole('button')).toHaveTextContent('dark')
    expect(document.documentElement).toHaveAttribute('data-theme', 'dark')
    expect(window.localStorage.getItem('dvld_theme')).toBe('dark')
  })

  it('exposes the next theme and changes it through the theme toggle control', async () => {
    const user = userEvent.setup()

    render(
      <ThemeProvider>
        <ThemeToggle />
      </ThemeProvider>,
    )

    const toggle = screen.getByRole('button', { name: 'Switch to dark theme' })
    await user.click(toggle)

    expect(screen.getByRole('button', { name: 'Switch to light theme' })).toBeVisible()
    expect(document.documentElement).toHaveAttribute('data-theme', 'dark')
    expect(window.localStorage.getItem('dvld_theme')).toBe('dark')
  })

  it('defines scoped light and dark token sets without overriding legacy palette variables', () => {
    const tokenSource = readFileSync(
      resolve(process.cwd(), 'src/design-system/tokens.css'),
      'utf8',
    )

    expect(tokenSource).toContain('--dvld-design-bg-page: #F3F0E7')
    expect(tokenSource).toContain(':root[data-theme="dark"]')
    expect(tokenSource).toContain('--dvld-design-brand-lime: #B7E52B')
    expect(tokenSource).not.toMatch(/--dvld-radius-(sm|md|lg):/)
    expect(tokenSource).not.toContain('--dvld-border-strong:')
    expect(tokenSource).not.toContain('color-scheme:')
    expect(tokenSource).not.toContain('--accent:')
    expect(tokenSource).not.toContain('--bg:')
  })
})
