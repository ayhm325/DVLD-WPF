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
  viewport: { width: number; height: number },
) {
  await page.setViewportSize(viewport)
  await page.route('**/Auth/refresh', (route) =>
    route.fulfill({
      status: 401,
      contentType: 'application/problem+json',
      body: JSON.stringify({ title: 'Unauthorized', status: 401 }),
    }),
  )
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true }))
    .toBeVisible()
}

for (const viewport of viewports) {
  test(`login fits a ${viewport.width}px viewport`, async ({ page }) => {
    await openUnauthenticatedLogin(page, viewport)

    await expect(page.getByLabel('Username')).toBeVisible()
    await expect(page.getByLabel('Password', { exact: true })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Sign in to Workspace' }))
      .toBeDisabled()

    const dimensions = await page.evaluate(() => ({
      viewport: window.innerWidth,
      viewportHeight: window.innerHeight,
      document: document.documentElement.scrollWidth,
      body: document.body.scrollWidth,
      primaryButtonBottom: document.querySelector('.dvld-submit-btn')!
        .getBoundingClientRect().bottom,
      inputTextSize: Number.parseFloat(
        getComputedStyle(document.querySelector('.dvld-input')!).fontSize,
      ),
    }))
    expect(dimensions.document).toBeLessThanOrEqual(dimensions.viewport)
    expect(dimensions.body).toBeLessThanOrEqual(dimensions.viewport)
    expect(dimensions.inputTextSize).toBeGreaterThanOrEqual(14)
    if (viewport.width === 390) {
      expect(dimensions.primaryButtonBottom).toBeLessThanOrEqual(
        dimensions.viewportHeight,
      )
    }

    await expect(page).toHaveScreenshot(`login-current-${viewport.width}.png`, {
      animations: 'disabled',
      fullPage: true,
    })
  })
}

test('login interactions preserve password visibility and reset guidance', async ({ page }) => {
  await openUnauthenticatedLogin(page, { width: 320, height: 720 })

  const password = page.getByLabel('Password', { exact: true })
  await password.fill('not-a-real-password')
  await expect(password).toHaveAttribute('type', 'password')
  await page.getByRole('button', { name: 'Show password' }).click()
  await expect(password).toHaveAttribute('type', 'text')
  await page.getByRole('button', { name: 'Hide password' }).click()
  await expect(password).toHaveAttribute('type', 'password')

  await page.getByRole('button', { name: 'Forgot password?' }).click()
  await expect(page.getByRole('alert')).toContainText(
    'contact your DVLD Systems Administrator',
  )
})

test('login has no serious accessibility violations', async ({ page }) => {
  await openUnauthenticatedLogin(page, { width: 390, height: 844 })
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze()

  expect(results.violations.filter((violation) =>
    violation.impact === 'critical' || violation.impact === 'serious',
  )).toEqual([])
})

test('login presents the current DVLD institutional identity', async ({ page }) => {
  await openUnauthenticatedLogin(page, { width: 1440, height: 900 })

  await expect(page.getByRole('heading', {
    level: 1,
    name: 'Driving forward. Managing smarter.',
  })).toBeVisible()
  await expect(page.getByRole('heading', {
    level: 2,
    name: 'Driver Licensing Management',
  })).toBeVisible()
  await expect(page.getByRole('heading', {
    level: 2,
    name: 'Vehicle Records & Ownership',
  })).toBeVisible()
  await expect(page.getByRole('heading', {
    level: 2,
    name: 'Administrative Authority',
  })).toBeVisible()
})

test('reduced-motion preference removes visible control transitions', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' })
  await openUnauthenticatedLogin(page, { width: 390, height: 844 })

  const transitionDurations = await page
    .getByRole('button', { name: 'Sign in to Workspace' })
    .evaluate((button) =>
      getComputedStyle(button).transitionDuration.split(',').map((duration) => {
        const value = Number.parseFloat(duration)
        return duration.trim().endsWith('ms') ? value / 1000 : value
      }),
    )

  expect(transitionDurations.every((duration) => duration <= 0.00001)).toBe(true)
})
