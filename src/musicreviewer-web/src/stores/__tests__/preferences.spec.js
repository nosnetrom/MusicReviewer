import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import { usePreferencesStore } from '../preferences'

const KEY = 'musicreviewer:preferences'
const root = document.documentElement

describe('preferences store', () => {
  beforeEach(() => {
    localStorage.clear()
    delete root.dataset.theme
    delete root.dataset.transparency
    setActivePinia(createPinia())
  })

  afterEach(() => vi.restoreAllMocks())

  it('defaults to the system theme with no data attributes', () => {
    const store = usePreferencesStore()

    expect(store.theme).toBe('system')
    expect(root.dataset.theme).toBeUndefined()
    expect(root.dataset.transparency).toBeUndefined()
  })

  it('applies and persists an explicit theme and reduced transparency', async () => {
    const store = usePreferencesStore()

    store.theme = 'dark'
    store.reduceTransparency = true
    await nextTick()

    expect(root.dataset.theme).toBe('dark')
    expect(root.dataset.transparency).toBe('reduced')
    expect(JSON.parse(localStorage.getItem(KEY))).toEqual({
      theme: 'dark',
      reduceTransparency: true,
    })
  })

  it('restores saved preferences and ignores invalid values', () => {
    localStorage.setItem(KEY, JSON.stringify({ theme: 'sepia', reduceTransparency: true }))

    const store = usePreferencesStore()

    expect(store.theme).toBe('system')
    expect(store.reduceTransparency).toBe(true)
  })

  it('cycles system → light → dark → system', () => {
    const store = usePreferencesStore()
    const seen = []
    for (let i = 0; i < 3; i++) {
      store.cycleTheme()
      seen.push(store.theme)
    }

    expect(seen).toEqual(['light', 'dark', 'system'])
  })

  it('still works when storage is unavailable', async () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked')
    })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked')
    })

    const store = usePreferencesStore()
    store.theme = 'light'
    await nextTick()

    expect(root.dataset.theme).toBe('light')
  })
})
