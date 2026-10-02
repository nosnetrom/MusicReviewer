import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { browseRecordings, getArtistRecordings, isSettled, searchArtists } from '../catalog'

describe('catalog api', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValue(new Response('{}', { headers: { 'Content-Type': 'application/json' } })),
    )
  })
  afterEach(() => vi.unstubAllGlobals())

  const calledUrl = () => fetch.mock.calls[0][0]

  it('encodes the search query', async () => {
    await searchArtists('Guns N’ Roses & friends')
    expect(calledUrl()).toBe('/api/search?q=Guns+N%E2%80%99+Roses+%26+friends')
  })

  it('omits empty filters', async () => {
    await getArtistRecordings('abc', { type: 'live' })
    expect(calledUrl()).toBe('/api/artists/abc/recordings?type=live')

    fetch.mockClear()
    await browseRecordings({ genre: null, decade: '' })
    expect(calledUrl()).toBe('/api/browse/recordings')
  })

  it('requests a page of artist recordings', async () => {
    await getArtistRecordings('abc', { type: 'studio', sort: 'date', offset: 60, limit: 60 })

    expect(calledUrl()).toBe('/api/artists/abc/recordings?type=studio&sort=date&offset=60&limit=60')
  })

  it('treats ready and failed imports as settled', () => {
    expect(isSettled('ready')).toBe(true)
    expect(isSettled('failed')).toBe(true)
    expect(isSettled('syncing')).toBe(false)
    expect(isSettled('notSynced')).toBe(false)
  })
})
