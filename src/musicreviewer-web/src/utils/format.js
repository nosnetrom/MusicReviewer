/**
 * "9:05" for 545000 ms; "1:02:03" past an hour; "" when unknown.
 * @param {number | null | undefined} ms
 */
export function formatDuration(ms) {
  if (!ms || ms < 0) return ''
  const total = Math.round(ms / 1000)
  const hours = Math.floor(total / 3600)
  const minutes = Math.floor((total % 3600) / 60)
  const seconds = String(total % 60).padStart(2, '0')
  return hours ? `${hours}:${String(minutes).padStart(2, '0')}:${seconds}` : `${minutes}:${seconds}`
}

/**
 * "45 min" or "1 hr 12 min" for a total running time.
 * @param {number} ms
 */
export function formatRuntime(ms) {
  const minutes = Math.round(ms / 60000)
  if (minutes < 60) return `${minutes} min`
  const rest = minutes % 60
  return `${Math.floor(minutes / 60)} hr${rest ? ` ${rest} min` : ''}`
}

/**
 * "1926–1991", "1943–" (still active), or "" when unknown.
 * @param {number | null} begin
 * @param {number | null} end
 */
export function formatYears(begin, end) {
  if (!begin && !end) return ''
  return `${begin ?? '?'}–${end ?? ''}`
}

const artistTypes = {
  person: 'Artist',
  group: 'Band',
  orchestra: 'Orchestra',
  choir: 'Choir',
  character: 'Character',
}

/** Human label for a MusicBrainz artist type. */
export const formatArtistType = (type) => artistTypes[type] ?? ''

/** Link to an English Wikipedia article by title. */
export const wikipediaUrl = (title) =>
  `https://en.wikipedia.org/wiki/${encodeURIComponent(title.replaceAll(' ', '_'))}`
