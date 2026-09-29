/** How much more often jazz artists are suggested than others. */
export const JAZZ_WEIGHT = 3

const LAST_SHOWN_KEY = 'musicreviewer:last-suggestion'

/**
 * Picks one artist name at random for the search field's example, weighted so jazz artists
 * come up `jazzWeight` times as often. Names in `exclude` are skipped unless nothing else is left.
 *
 * @param {{ name: string, genres: string[] }[]} artists
 * @param {{ exclude?: string[], jazzWeight?: number, random?: () => number }} [options]
 * @returns {string | null} null when there are no artists
 */
export function pickSuggestion(
  artists,
  { exclude = [], jazzWeight = JAZZ_WEIGHT, random = Math.random } = {},
) {
  const named = artists.filter((a) => a.name)
  const pool = named.some((a) => !exclude.includes(a.name))
    ? named.filter((a) => !exclude.includes(a.name))
    : named
  if (pool.length === 0) return null

  const weight = (a) => (a.genres?.includes('jazz') ? jazzWeight : 1)
  let target = random() * pool.reduce((sum, a) => sum + weight(a), 0)
  for (const artist of pool) {
    target -= weight(artist)
    if (target < 0) return artist.name
  }
  return pool[pool.length - 1].name
}

/**
 * Picks the example for this visit, avoiding the one shown last time (remembered for the
 * browser session) so each visit to Home shows a different musician.
 *
 * @param {{ name: string, genres: string[] }[]} artists
 * @param {Storage | undefined} [storage]
 */
export function pickSuggestionForVisit(artists, storage = globalThis.sessionStorage) {
  let last = null
  try {
    last = storage?.getItem(LAST_SHOWN_KEY) ?? null
  } catch {
    // Storage can be blocked (private mode); repeats are then merely possible, not harmful.
  }

  const name = pickSuggestion(artists, { exclude: last ? [last] : [] })

  try {
    if (name) storage?.setItem(LAST_SHOWN_KEY, name)
  } catch {
    // See above.
  }
  return name
}
