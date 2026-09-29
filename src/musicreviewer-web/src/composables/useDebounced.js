import { onScopeDispose, readonly, ref, watch } from 'vue'

/**
 * A read-only copy of `source` that only updates after it has stopped changing for `delay` ms.
 * @template T
 * @param {import('vue').Ref<T>} source
 * @param {number} [delay]
 * @returns {Readonly<import('vue').Ref<T>>}
 */
export function useDebounced(source, delay = 300) {
  const debounced = ref(source.value)
  let timer = null

  watch(source, (value) => {
    clearTimeout(timer)
    timer = setTimeout(() => (debounced.value = value), delay)
  })

  onScopeDispose(() => clearTimeout(timer))

  return readonly(debounced)
}
