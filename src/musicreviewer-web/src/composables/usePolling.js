import { onScopeDispose, ref, shallowRef } from 'vue'

/**
 * Loads data, then keeps reloading every `interval` ms until `until(data)` is true —
 * used while the API imports an artist or recording in the background.
 * Stops automatically when the calling component unmounts.
 *
 * @template T
 * @param {(options: { signal: AbortSignal }) => Promise<T>} load
 * @param {{ interval?: number, until?: (data: T) => boolean, immediate?: boolean }} [options]
 *   `immediate: false` waits for the first `refresh()` call.
 */
export function usePolling(load, { interval = 2000, until = () => true, immediate = true } = {}) {
  /** @type {import('vue').ShallowRef<T | null>} */
  const data = shallowRef(null)
  const error = shallowRef(null)
  const loading = ref(false)

  let timer = null
  let controller = null
  let generation = 0

  async function tick(run) {
    controller?.abort()
    controller = new AbortController()
    loading.value = true

    try {
      const value = await load({ signal: controller.signal })
      if (run !== generation) return
      data.value = value
      error.value = null
      if (!until(value)) timer = setTimeout(() => tick(run), interval)
    } catch (e) {
      if (run !== generation || e?.name === 'AbortError') return
      error.value = e
    } finally {
      if (run === generation) loading.value = false
    }
  }

  function stop() {
    generation++
    clearTimeout(timer)
    controller?.abort()
    loading.value = false
  }

  /** Start over, e.g. after a filter changes. Keeps the current data visible until new data arrives. */
  function refresh() {
    stop()
    tick(generation)
  }

  onScopeDispose(stop)
  if (immediate) tick(generation)

  return { data, error, loading, refresh, stop }
}
