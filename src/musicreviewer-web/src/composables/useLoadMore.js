import { onScopeDispose, ref, shallowRef } from 'vue'

/**
 * A list that loads page by page. `reset()` starts over (e.g. when filters change);
 * `loadMore()` appends the next page. Responses from superseded requests are ignored.
 *
 * @template T
 * @param {(page: { offset: number, signal: AbortSignal }) => Promise<{ items: T[], total: number, hasMore: boolean }>} loadPage
 */
export function useLoadMore(loadPage) {
  /** @type {import('vue').ShallowRef<T[]>} */
  const items = shallowRef([])
  const total = ref(0)
  const hasMore = ref(false)
  const loaded = ref(false)
  const loading = ref(false)
  const loadingMore = ref(false)
  const error = shallowRef(null)

  let controller = null
  let generation = 0

  async function fetchPage(append) {
    controller?.abort()
    controller = new AbortController()
    const run = generation
    const busy = append ? loadingMore : loading
    busy.value = true
    error.value = null

    try {
      const page = await loadPage({
        offset: append ? items.value.length : 0,
        signal: controller.signal,
      })
      if (run !== generation) return
      items.value = append ? [...items.value, ...page.items] : page.items
      total.value = page.total
      hasMore.value = page.hasMore
      loaded.value = true
    } catch (e) {
      if (run !== generation || e?.name === 'AbortError') return
      error.value = e
    } finally {
      if (run === generation) busy.value = false
    }
  }

  function reset() {
    generation++
    loadingMore.value = false
    return fetchPage(false)
  }

  function loadMore() {
    if (!hasMore.value || loading.value || loadingMore.value) return Promise.resolve()
    return fetchPage(true)
  }

  onScopeDispose(() => {
    generation++
    controller?.abort()
  })

  return { items, total, hasMore, loaded, loading, loadingMore, error, reset, loadMore }
}
