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
  constructor(status, problem, retryAfter) {
    super(problem?.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
    const seconds = Number(retryAfter)
    this.retryAfterMs = Number.isFinite(seconds) ? Math.max(0, seconds * 1000) : null
  }
}

export function friendlyApiMessage(error, fallback) {
  if (error?.status === 429)
    return "You're making requests too quickly. Please wait a moment and try again."
  if (error?.status === 503) return 'The catalog is busy right now. Please try again shortly.'
  return fallback
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
    throw new ApiError(response.status, body, response.headers.get('Retry-After'))
  }

  return body
}
