import { describe, expect, it } from 'vitest'
import { listenLinks } from '../listen'

describe('listenLinks', () => {
  const byService = (album) => Object.fromEntries(listenLinks(album).map((l) => [l.service, l]))

  it('searches each service for the artist and album', () => {
    const links = byService({ artist: 'Miles Davis', title: 'Kind of Blue' })

    expect(links['youtube-music'].url).toBe(
      'https://music.youtube.com/search?q=Miles%20Davis%20Kind%20of%20Blue',
    )
    expect(links.pandora.url).toBe(
      'https://www.pandora.com/search/Miles%20Davis%20Kind%20of%20Blue/all',
    )
    expect(links['amazon-music'].url).toBe(
      'https://music.amazon.com/search/Miles%20Davis%20Kind%20of%20Blue',
    )
  })

  it('carries the notes for Pandora and Amazon Music', () => {
    const links = byService({ artist: 'A', title: 'B' })

    expect(links.pandora.note).toBe('USA only')
    expect(links['amazon-music'].note).toBe('requires account')
    expect(links['youtube-music'].note).toBeNull()
  })

  it('keeps slashes and special characters from breaking the URLs', () => {
    const links = byService({ artist: 'AC/DC', title: 'Back in Black & More?' })

    expect(links.pandora.url).toBe(
      'https://www.pandora.com/search/AC%20DC%20Back%20in%20Black%20%26%20More%3F/all',
    )
    expect(links['amazon-music'].url).not.toContain('AC/DC')
  })
})
