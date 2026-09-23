import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

const STORAGE_KEY = 'musicreviewer:preferences'

/** @typedef {'system' | 'light' | 'dark'} ThemePreference */

function load() {
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '{}')
  } catch {
    return {}
  }
}

function save(value) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(value))
  } catch {
    // Storage can be unavailable (private mode, blocked site data); preferences are best-effort.
  }
}

/**
 * Per-viewer display preferences, reflected onto <html> as data attributes
 * that tokens.css keys off. "system" leaves the decision to media queries.
 */
export const usePreferencesStore = defineStore('preferences', () => {
  const saved = load()

  /** @type {import('vue').Ref<ThemePreference>} */
  const theme = ref(['light', 'dark'].includes(saved.theme) ? saved.theme : 'system')
  const reduceTransparency = ref(saved.reduceTransparency === true)

  function apply() {
    const root = document.documentElement
    if (theme.value === 'system') delete root.dataset.theme
    else root.dataset.theme = theme.value

    if (reduceTransparency.value) root.dataset.transparency = 'reduced'
    else delete root.dataset.transparency
  }

  watch(
    [theme, reduceTransparency],
    () => {
      apply()
      save({ theme: theme.value, reduceTransparency: reduceTransparency.value })
    },
    { immediate: true },
  )

  /** Cycle system → light → dark → system. */
  function cycleTheme() {
    const order = /** @type {ThemePreference[]} */ (['system', 'light', 'dark'])
    theme.value = order[(order.indexOf(theme.value) + 1) % order.length]
  }

  return { theme, reduceTransparency, cycleTheme }
})
