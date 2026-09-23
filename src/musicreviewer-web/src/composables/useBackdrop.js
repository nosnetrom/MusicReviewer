import { onBeforeUnmount, reactive, readonly } from 'vue'

const state = reactive({
  /** CSS color that tints the page backdrop and glass; null uses the theme default. */
  tint: /** @type {string | null} */ (null),
  /** Optional image (e.g. album art) rendered blurred behind everything. */
  image: /** @type {string | null} */ (null),
})

/** Read-only backdrop state, consumed by <AdaptiveBackdrop>. */
export const backdropState = readonly(state)

/**
 * Lets a view set the page backdrop. Values reset when the calling component unmounts,
 * so navigating away never leaves a stale tint behind.
 */
export function useBackdrop() {
  function setBackdrop({ tint = null, image = null } = {}) {
    state.tint = tint
    state.image = image
  }

  onBeforeUnmount(() => setBackdrop())

  return { setBackdrop }
}
