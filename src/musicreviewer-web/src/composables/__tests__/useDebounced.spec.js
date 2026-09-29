import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick, ref } from 'vue'
import { useDebounced } from '../useDebounced'

describe('useDebounced', () => {
  let scope

  beforeEach(() => {
    vi.useFakeTimers()
    scope = effectScope()
  })

  afterEach(() => {
    scope.stop()
    vi.useRealTimers()
  })

  it('only updates once the source stops changing', async () => {
    const source = ref('')
    const debounced = scope.run(() => useDebounced(source, 300))

    for (const text of ['m', 'mi', 'mil', 'miles']) {
      source.value = text
      await nextTick()
      vi.advanceTimersByTime(100)
    }
    expect(debounced.value).toBe('')

    vi.advanceTimersByTime(300)
    expect(debounced.value).toBe('miles')
  })
})
