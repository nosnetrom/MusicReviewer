import { getJson } from './client'

/**
 * @typedef {'notSynced' | 'syncing' | 'ready' | 'failed'} SyncStatus
 *
 * @typedef {object} ArtistSummary
 * @property {string} mbid
 * @property {string} name
 * @property {string | null} disambiguation
 * @property {string} type
 * @property {string | null} country
 * @property {number | null} beginYear
 * @property {number | null} endYear
 * @property {boolean} isImported
 *
 * @typedef {object} RecordingSummary
 * @property {string} mbid
 * @property {string} title
 * @property {string | null} artistCredit
 * @property {string} artistMbid
 * @property {string} artistName
 * @property {string} primaryType
 * @property {string} category
 * @property {number | null} year
 * @property {string | null} coverArtUrl
 * @property {boolean} hasWikipediaArticle
 *
 * The plain-text lead of a Wikipedia article (CC BY-SA 4.0; attribute when shown).
 * @typedef {object} WikipediaSummary
 * @property {string} title
 * @property {string} url
 * @property {number} revisionId
 * @property {string} fetchedUtc
 * @property {string[]} paragraphs
 */

/** @typedef {{ signal?: AbortSignal }} RequestOptions */

const query = (params) => {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, value)
  }
  const text = search.toString()
  return text ? `?${text}` : ''
}

/** @returns {Promise<{ query: string, artists: ArtistSummary[], remoteAvailable: boolean }>} */
export const searchArtists = (q, options) => getJson(`/api/search${query({ q })}`, options)

/** @returns {Promise<{ name: string, genres: string[] }[]>} imported artists, for example searches */
export const getSearchSuggestions = (options) => getJson('/api/search/suggestions', options)

/** @returns {Promise<ArtistSummary & { syncStatus: SyncStatus, wikipediaTitle: string | null, summary: WikipediaSummary | null }>} plus genres, styles and counts */
export const getArtist = (mbid, options) => getJson(`/api/artists/${mbid}`, options)

/**
 * @param {string} mbid
 * @param {{ type?: 'studio' | 'ep' | 'live' | 'compilation' | 'all', sort?: 'notability' | 'date' }} [filters]
 * @param {{ offset?: number, limit?: number }} [page]
 * @returns {Promise<{ filter: string, syncStatus: SyncStatus, recordings: RecordingSummary[], offset: number, total: number, hasMore: boolean }>}
 */
export const getArtistRecordings = (mbid, { type, sort, offset, limit } = {}, options) =>
  getJson(`/api/artists/${mbid}/recordings${query({ type, sort, offset, limit })}`, options)

/** @returns {Promise<RecordingSummary & { detailsStatus: SyncStatus, wikipediaTitle: string | null, summary: WikipediaSummary | null }>} plus tracks and credits */
export const getRecording = (mbid, options) => getJson(`/api/recordings/${mbid}`, options)

/** @returns {Promise<{ artists: ArtistSummary[], recordings: RecordingSummary[] }>} */
export const getFeatured = (options) => getJson('/api/browse/featured', options)

/** @returns {Promise<{ name: string, slug: string, artistCount: number }[]>} */
export const getGenres = (options) => getJson('/api/genres', options)

/**
 * One page of studio albums, each artist's top three first. Pages hold up to 60 albums.
 * @param {{ genre?: string | null, decade?: string | null, offset?: number, limit?: number }} [filters]
 * @returns {Promise<{ genre: string | null, decade: string | null, recordings: RecordingSummary[], offset: number, total: number, hasMore: boolean }>}
 */
export const browseRecordings = ({ genre, decade, offset, limit } = {}, options) =>
  getJson(
    `/api/browse/recordings${query({ genre, decade, offset: offset || undefined, limit })}`,
    options,
  )

/** True once an import has finished, successfully or not. */
export const isSettled = (status) => status === 'ready' || status === 'failed'
