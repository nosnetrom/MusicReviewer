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
export function usePolling(
  load,
  {
    interval = 2000,
    maxInterval = 30_000,
    jitterRatio = 0.2,
    until = () => true,
    immediate = true,
  } = {},
) {
  /** @type {import('vue').ShallowRef<T | null>} */
  const data = shallowRef(null)
  const error = shallowRef(null)
  const loading = ref(false)

  let timer = null
  let controller = null
  let generation = 0
  let attempt = 0

  function nextDelay(error) {
    const exponential = Math.min(interval * 2 ** attempt, maxInterval)
    attempt++
    const jitter = exponential * (1 - jitterRatio + Math.random() * jitterRatio * 2)
    return Math.max(Math.round(jitter), error?.retryAfterMs ?? 0)
  }

  function schedule(run, error) {
    timer = setTimeout(() => tick(run), nextDelay(error))
  }

  async function tick(run) {
    controller?.abort()
    controller = new AbortController()
    loading.value = true

    try {
      const value = await load({ signal: controller.signal })
      if (run !== generation) return
      data.value = value
      error.value = null
      if (!until(value)) schedule(run)
    } catch (e) {
      if (run !== generation || e?.name === 'AbortError') return
      error.value = e
      if (e?.status === 429 || e?.status >= 500 || e?.status == null) schedule(run, e)
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
    attempt = 0
    tick(generation)
  }

  onScopeDispose(stop)
  if (immediate) tick(generation)

  return { data, error, loading, refresh, stop }
}
