import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick } from 'vue'
import { usePolling } from '../usePolling'

const flush = async () => {
  await Promise.resolve()
  await nextTick()
}

describe('usePolling', () => {
  let scope

  beforeEach(() => {
    vi.useFakeTimers()
    scope = effectScope()
  })

  afterEach(() => {
    scope.stop()
    vi.useRealTimers()
  })

  it('reloads on an interval until the condition is met', async () => {
    const statuses = ['syncing', 'syncing', 'ready']
    const load = vi.fn(async () => ({ status: statuses[load.mock.calls.length - 1] }))
    const poll = scope.run(() =>
      usePolling(load, { interval: 2000, jitterRatio: 0, until: (d) => d.status === 'ready' }),
    )

    await flush()
    expect(poll.data.value.status).toBe('syncing')

    await vi.advanceTimersByTimeAsync(4000)
    await vi.advanceTimersByTimeAsync(2000)
    expect(poll.data.value.status).toBe('ready')
    expect(load).toHaveBeenCalledTimes(3)

    await vi.advanceTimersByTimeAsync(10_000)
    expect(load).toHaveBeenCalledTimes(3)
  })

  it('stops when its component scope is disposed', async () => {
    const load = vi.fn(async () => ({ status: 'syncing' }))
    scope.run(() => usePolling(load, { interval: 1000, jitterRatio: 0, until: () => false }))
    await flush()

    scope.stop()
    await vi.advanceTimersByTimeAsync(5000)

    expect(load).toHaveBeenCalledTimes(1)
  })

  it('waits for refresh() when not immediate, and restarts on refresh', async () => {
    const load = vi.fn(async () => 'ok')
    const poll = scope.run(() => usePolling(load, { immediate: false }))
    await flush()
    expect(load).not.toHaveBeenCalled()

    poll.refresh()
    await flush()
    poll.refresh()
    await flush()

    expect(load).toHaveBeenCalledTimes(2)
    expect(poll.data.value).toBe('ok')
  })

  it('retries transient errors with exponential delay', async () => {
    const failure = Object.assign(new Error('nope'), { status: 503 })
    const load = vi.fn().mockRejectedValueOnce(failure).mockResolvedValue({ status: 'ready' })
    const poll = scope.run(() => usePolling(load, { interval: 1000, jitterRatio: 0 }))

    await flush()
    await vi.advanceTimersByTimeAsync(1000)

    expect(poll.error.value).toBeNull()
    expect(poll.data.value).toEqual({ status: 'ready' })
    expect(load).toHaveBeenCalledTimes(2)
  })

  it('waits at least the server Retry-After duration', async () => {
    const failure = Object.assign(new Error('slow down'), { status: 429, retryAfterMs: 5000 })
    const load = vi.fn().mockRejectedValueOnce(failure).mockResolvedValue({ status: 'ready' })
    const poll = scope.run(() => usePolling(load, { interval: 1000, jitterRatio: 0 }))

    await flush()
    await vi.advanceTimersByTimeAsync(4999)
    expect(load).toHaveBeenCalledTimes(1)
    await vi.advanceTimersByTimeAsync(1)

    expect(poll.data.value).toEqual({ status: 'ready' })
    expect(load).toHaveBeenCalledTimes(2)
  })
})
