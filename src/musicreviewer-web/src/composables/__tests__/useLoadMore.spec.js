import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope } from 'vue'
import { useLoadMore } from '../useLoadMore'

// A fake catalog of 7 items served 3 at a time.
const catalog = ['a', 'b', 'c', 'd', 'e', 'f', 'g']
const pages = vi.fn(async ({ offset }) => ({
  items: catalog.slice(offset, offset + 3),
  total: catalog.length,
  hasMore: offset + 3 < catalog.length,
}))

describe('useLoadMore', () => {
  let scope

  beforeEach(() => {
    pages.mockClear()
    scope = effectScope()
  })
  afterEach(() => scope.stop())

  it('appends pages until there are no more', async () => {
    const list = scope.run(() => useLoadMore(pages))

    await list.reset()
    expect(list.items.value).toEqual(['a', 'b', 'c'])
    expect(list.hasMore.value).toBe(true)

    await list.loadMore()
    await list.loadMore()
    expect(list.items.value).toEqual(catalog)
    expect(list.hasMore.value).toBe(false)
    expect(pages.mock.calls.map(([page]) => page.offset)).toEqual([0, 3, 6])

    await list.loadMore()
    expect(pages).toHaveBeenCalledTimes(3)
  })

  it('starts over on reset, discarding appended pages', async () => {
    const list = scope.run(() => useLoadMore(pages))
    await list.reset()
    await list.loadMore()

    await list.reset()

    expect(list.items.value).toEqual(['a', 'b', 'c'])
    expect(pages.mock.calls.at(-1)[0].offset).toBe(0)
  })

  it('ignores a slow page that was superseded by a reset', async () => {
    let releaseSlow
    const slow = new Promise((resolve) => (releaseSlow = resolve))
    const loader = vi
      .fn()
      .mockImplementationOnce(() => slow)
      .mockResolvedValue({ items: ['fresh'], total: 1, hasMore: false })
    const list = scope.run(() => useLoadMore(loader))

    const stale = list.reset()
    await list.reset()
    releaseSlow({ items: ['stale'], total: 1, hasMore: false })
    await stale

    expect(list.items.value).toEqual(['fresh'])
  })

  it('keeps loaded items and exposes the error when a page fails', async () => {
    const loader = vi
      .fn()
      .mockResolvedValueOnce({ items: ['a'], total: 2, hasMore: true })
      .mockRejectedValueOnce(new Error('offline'))
    const list = scope.run(() => useLoadMore(loader))

    await list.reset()
    await list.loadMore()

    expect(list.items.value).toEqual(['a'])
    expect(list.error.value?.message).toBe('offline')
    expect(list.hasMore.value).toBe(true)
  })
})
