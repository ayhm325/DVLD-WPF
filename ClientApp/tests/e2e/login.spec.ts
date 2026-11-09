import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'

const viewports = [
  { width: 320, height: 720 },
  { width: 390, height: 844 },
  { width: 768, height: 1024 },
  { width: 1440, height: 900 },
]

async function openUnauthenticatedLogin(
  page: Page,
  theme: 'light' | 'dark',
  viewport: { width: number; height: number },
) {
  await page.setViewportSize(viewport)
  await page.addInitScript((initialTheme) => {
    localStorage.setItem('dvld_theme', initialTheme)
  }, theme)
  await page.route('**/Auth/refresh', (route) =>
    route.fulfill({
      status: 401,
      contentType: 'application/problem+json',
      body: JSON.stringify({ title: 'Unauthorized', status: 401 }),
    }),
  )
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Welcome back.' })).toBeVisible()
}

for (const viewport of viewports) {
  test(`login fits ${viewport.width}px in light theme`, async ({ page }) => {
    await openUnauthenticatedLogin(page, 'light', viewport)

    await expect(page.locator('html')).toHaveAttribute('data-theme', 'light')
    await expect(page.getByLabel('Username')).toBeVisible()
    await expect(page.getByLabel('Password', { exact: true })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Sign in to Workspace' })).toBeDisabled()

    const dimensions = await page.evaluate(() => ({
      viewport: window.innerWidth,
      viewportHeight: window.innerHeight,
      document: document.documentElement.scrollWidth,
      body: document.body.scrollWidth,
      legacyRootColorScheme: getComputedStyle(document.documentElement).colorScheme,
      primaryButtonBottom: document.querySelector('.dvld-submit-btn')!.getBoundingClientRect().bottom,
      inputTextSize: Number.parseFloat(getComputedStyle(document.querySelector('.dvld-input')!).fontSize),
      primaryButtonRadius: getComputedStyle(document.querySelector('.dvld-submit-btn')!).borderRadius,
    }))
    expect(dimensions.document).toBeLessThanOrEqual(dimensions.viewport)
    expect(dimensions.body).toBeLessThanOrEqual(dimensions.viewport)
    expect(dimensions.legacyRootColorScheme).toBe('dark')
    if (viewport.width === 390) {
      expect(dimensions.primaryButtonBottom).toBeLessThanOrEqual(dimensions.viewportHeight)
    }
    expect(dimensions.inputTextSize).toBeGreaterThanOrEqual(14)
    expect(dimensions.primaryButtonRadius).toBe('0px')

    await expect(page).toHaveScreenshot(`login-light-${viewport.width}.png`, {
      animations: 'disabled',
      fullPage: true,
    })
  })

  test(`login fits ${viewport.width}px in dark theme`, async ({ page }) => {
    await openUnauthenticatedLogin(page, 'dark', viewport)

    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark')
    const documentWidth = await page.evaluate(() => document.documentElement.scrollWidth)
    expect(documentWidth).toBeLessThanOrEqual(viewport.width)

    await expect(page).toHaveScreenshot(`login-dark-${viewport.width}.png`, {
      animations: 'disabled',
      fullPage: true,
    })
  })
}

test('login interactions preserve password visibility and administrative reset guidance', async ({ page }) => {
  await openUnauthenticatedLogin(page, 'light', viewports[0])

  const password = page.getByLabel('Password', { exact: true })
  await password.fill('not-a-real-password')
  await expect(password).toHaveAttribute('type', 'password')
  await page.getByRole('button', { name: 'Show password' }).click()
  await expect(password).toHaveAttribute('type', 'text')
  await page.getByRole('button', { name: 'Hide password' }).click()
  await expect(password).toHaveAttribute('type', 'password')

  await page.getByRole('button', { name: 'Forgot password?' }).click()
  await expect(page.getByRole('alert')).toContainText('contact your DVLD Systems Administrator')
})

test('login has no serious accessibility violations in both themes', async ({ page }) => {
  for (const theme of ['light', 'dark'] as const) {
    await openUnauthenticatedLogin(page, theme, { width: 390, height: 844 })
    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
      .analyze()

    expect(results.violations.filter((violation) =>
      violation.impact === 'critical' || violation.impact === 'serious',
    )).toEqual([])
  }
})

test('login expresses the Neo-Brutalist identity in both themes', async ({ page }) => {
  for (const theme of ['light', 'dark'] as const) {
    await openUnauthenticatedLogin(page, theme, { width: 1440, height: 900 })
    const styles = await page.evaluate(() => {
      const css = (selector: string) => getComputedStyle(document.querySelector(selector)!)
      const accent = css('.dvld-mission-heading span:last-child')
      return {
        page: css('.dvld-login-root').backgroundColor,
        workspace: css('.dvld-auth-column').backgroundColor,
        formCard: css('.dvld-auth-inner').backgroundColor,
        formBorder: css('.dvld-auth-inner').borderTopWidth,
        formRadius: css('.dvld-auth-inner').borderTopLeftRadius,
        formShadow: css('.dvld-auth-inner').boxShadow,
        headingWeight: css('.dvld-mission-heading').fontWeight,
        headingSize: Number.parseFloat(css('.dvld-mission-heading').fontSize),
        headingLineHeight: Number.parseFloat(css('.dvld-mission-heading').lineHeight),
        bodySize: Number.parseFloat(css('.dvld-auth-subtitle').fontSize),
        accentColor: accent.color,
        accentBackground: accent.backgroundColor,
        inputBorderWidth: css('.dvld-input').borderTopWidth,
        buttonBorderWidth: css('.dvld-submit-btn').borderTopWidth,
        buttonRadius: css('.dvld-submit-btn').borderTopLeftRadius,
        buttonBorderColor: css('.dvld-submit-btn').borderTopColor,
        buttonBackground: css('.dvld-submit-btn').backgroundColor,
        buttonText: css('.dvld-submit-btn').color,
        buttonShadow: css('.dvld-submit-btn').boxShadow,
        inputRadius: css('.dvld-input').borderTopLeftRadius,
        inputBorder: css('.dvld-input').borderTopWidth,
        columns: css('.dvld-login-card').gridTemplateColumns.split(' ').map(Number.parseFloat),
        serviceList: css('.dvld-services-list').display,
      }
    })

    expect(styles.page).toBe(theme === 'light' ? 'rgb(243, 240, 231)' : 'rgb(23, 26, 24)')
    expect(styles.workspace).toBe(theme === 'light' ? 'rgb(243, 240, 231)' : 'rgb(23, 26, 24)')
    expect(styles.formCard).toBe(theme === 'light' ? 'rgb(255, 255, 255)' : 'rgb(32, 35, 33)')
    expect(styles.formBorder).toBe('3px')
    expect(styles.formRadius).toBe('0px')
    expect(styles.formShadow).toContain(theme === 'light' ? '8px 8px 0px rgb(32, 35, 33)' : '8px 8px 0px rgb(183, 229, 43)')
    expect(Number(styles.headingWeight)).toBeGreaterThanOrEqual(800)
    expect(styles.headingSize).toBeGreaterThan(30)
    expect(styles.headingSize).toBeGreaterThan(styles.bodySize * 2)
    expect(styles.headingLineHeight).toBeLessThanOrEqual(styles.headingSize)
    expect(styles.accentColor).toBe('rgb(183, 229, 43)')
    expect(styles.accentBackground).toBe('rgba(0, 0, 0, 0)')
    expect(styles.inputBorderWidth).toBe('2px')
    expect(styles.inputRadius).toBe('0px')
    expect(styles.buttonBorderWidth).toBe('3px')
    expect(styles.buttonRadius).toBe('0px')
    expect(styles.buttonBorderColor).toBe(theme === 'light' ? 'rgb(32, 35, 33)' : 'rgb(243, 240, 231)')
    expect(styles.buttonBackground).toBe('rgb(183, 229, 43)')
    expect(styles.buttonText).toBe(theme === 'light' ? 'rgb(32, 35, 33)' : 'rgb(23, 26, 24)')
    expect(styles.buttonShadow).toContain(theme === 'light' ? '6px 6px 0px rgb(32, 35, 33)' : '6px 6px 0px rgb(243, 240, 231)')
    expect(styles.columns).toHaveLength(2)
    expect(styles.columns[0] / styles.columns[1]).toBeGreaterThan(1.1)
    expect(styles.serviceList).toBe('block')
    await expect(page.getByRole('list', { name: 'DVLD services' }).getByRole('listitem'))
      .toHaveCount(3)
  }
})

test('reduced-motion preference removes visible control transitions', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' })
  await openUnauthenticatedLogin(page, 'light', { width: 390, height: 844 })

  const transitionDurations = await page.getByRole('button', { name: 'Sign in to Workspace' })
    .evaluate((button) => getComputedStyle(button).transitionDuration.split(',').map((duration) => {
      const value = Number.parseFloat(duration)
      return duration.trim().endsWith('ms') ? value / 1000 : value
    }))

  expect(transitionDurations.every((duration) => duration <= 0.00001)).toBe(true)
})
