import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { dismissToast, showToast, toastList } from '../useToast'

describe('useToast', () => {
  beforeEach(() => vi.useFakeTimers())

  afterEach(() => {
    ;[...toastList].forEach((t) => dismissToast(t.id))
    vi.useRealTimers()
  })

  it('adds a toast and removes it after its duration', () => {
    showToast('Saved', { durationMs: 1000 })
    expect(toastList.map((t) => t.message)).toEqual(['Saved'])

    vi.advanceTimersByTime(1000)
    expect(toastList).toHaveLength(0)
  })

  it('keeps a toast with no duration until dismissed', () => {
    const id = showToast('Sticky', { tone: 'error', durationMs: 0 })
    vi.advanceTimersByTime(60_000)
    expect(toastList[0]).toMatchObject({ message: 'Sticky', tone: 'error' })

    dismissToast(id)
    expect(toastList).toHaveLength(0)
  })
})
