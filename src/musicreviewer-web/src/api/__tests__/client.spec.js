import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, getJson } from '../client'

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
