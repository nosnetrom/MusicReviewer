import { reactive, readonly } from 'vue'

/**
 * @typedef {object} Toast
 * @property {number} id
 * @property {string} message
 * @property {'info' | 'error'} tone
 */

const toasts = reactive(/** @type {Toast[]} */ ([]))
let nextId = 1

export const toastList = readonly(toasts)

export function dismissToast(id) {
  const index = toasts.findIndex((t) => t.id === id)
  if (index !== -1) toasts.splice(index, 1)
}

/**
 * Show a transient message in the app's toast region.
 * @param {string} message
 * @param {{ tone?: Toast['tone'], durationMs?: number }} [options]
 * @returns {number} toast id
 */
export function showToast(message, { tone = 'info', durationMs = 4000 } = {}) {
  const id = nextId++
  toasts.push({ id, message, tone })
  if (durationMs > 0) setTimeout(() => dismissToast(id), durationMs)
  return id
}

export function useToast() {
  return { showToast, dismissToast }
}
