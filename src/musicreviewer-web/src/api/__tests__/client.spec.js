import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, friendlyApiMessage, getJson } from '../client'

const jsonResponse = (status, body) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })

describe('getJson', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('returns the parsed body for a successful response', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(200, { status: 'Healthy' })))

    await expect(getJson('/api/health')).resolves.toEqual({ status: 'Healthy' })
    expect(fetch).toHaveBeenCalledWith('/api/health', expect.any(Object))
  })

  it('throws an ApiError carrying the problem details for a failed response', async () => {
    const problem = { title: 'Not Found', status: 404 }
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(404, problem)))

    const error = await getJson('/api/artists/missing').catch((e) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(404)
    expect(error.message).toBe('Not Found')
    expect(error.problem).toEqual(problem)
  })
})

describe('friendlyApiMessage', () => {
  it('explains rate limiting and service saturation in user-facing language', () => {
    expect(friendlyApiMessage({ status: 429 }, 'Fallback')).toMatch(/wait a moment/i)
    expect(friendlyApiMessage({ status: 503 }, 'Fallback')).toMatch(/catalog is busy/i)
  })

  it('uses the supplied fallback for unrelated errors', () => {
    expect(friendlyApiMessage({ status: 404 }, 'Not found')).toBe('Not found')
  })
})
