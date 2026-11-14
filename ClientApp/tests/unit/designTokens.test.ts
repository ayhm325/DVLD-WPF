import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

describe('design token source', () => {
  it('defines the active palette and maps shared utilities to its tokens', () => {
    const tokenSource = readFileSync(resolve(process.cwd(), 'src/index.css'), 'utf8')

    expect(tokenSource).toContain('--accent: #3b82f6;')
    expect(tokenSource).toContain('--bg: #0b1120;')
    expect(tokenSource).toContain('--dvld-primary: var(--accent);')
    expect(tokenSource).toContain('--color-primary: var(--accent);')
    expect(tokenSource).not.toContain('--dvld-design-bg-page:')
  })
})
