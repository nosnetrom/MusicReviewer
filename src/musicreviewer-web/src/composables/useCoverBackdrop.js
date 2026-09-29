import { watch } from 'vue'
import { useBackdrop } from './useBackdrop'
import { dominantColor, hueFromString } from '@/utils/color'

/**
 * Tints the page backdrop from album art: the cover's dominant colour when it can be read,
 * otherwise a stable hue derived from `seed` (matching the placeholder cover).
 * @param {() => { coverUrl?: string | null, seed?: string } | null} source
 */
export function useCoverBackdrop(source) {
  const { setBackdrop } = useBackdrop()
  let latest = 0

  watch(
    source,
    async (value) => {
      const run = ++latest
      if (!value) return

      const fallback = value.seed ? `oklch(0.62 0.18 ${hueFromString(value.seed)})` : null
      setBackdrop({ tint: fallback, image: value.coverUrl ?? null })

      if (value.coverUrl) {
        const color = await dominantColor(value.coverUrl)
        if (run === latest && color) setBackdrop({ tint: color, image: value.coverUrl })
      }
    },
    { immediate: true },
  )
}
