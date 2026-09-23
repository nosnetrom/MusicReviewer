/**
 * Base URL of the API. Empty in local development (Vite proxies /api);
 * set VITE_API_BASE_URL to the App Service URL for deployed builds.
 */
const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

export class ApiError extends Error {
  /**
   * @param {number} status HTTP status code
   * @param {object | null} problem RFC 7807 problem details, when the API returned one
   */
  constructor(status, problem) {
    super(problem?.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

/**
 * GET a JSON resource from the API.
 * @template T
 * @param {string} path Path beginning with /api
 * @param {{ signal?: AbortSignal }} [options]
 * @returns {Promise<T>}
 */
export async function getJson(path, { signal } = {}) {
  const response = await fetch(`${baseUrl}${path}`, {
    headers: { Accept: 'application/json' },
    signal,
  })

  const body = await response.json().catch(() => null)

  if (!response.ok) {
    throw new ApiError(response.status, body)
  }

  return body
}
