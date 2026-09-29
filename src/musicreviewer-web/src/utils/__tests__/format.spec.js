import { describe, expect, it } from 'vitest'
import {
  formatArtistType,
  formatDuration,
  formatRuntime,
  formatYears,
  wikipediaUrl,
} from '../format'

describe('formatDuration', () => {
  it.each([
    [545000, '9:05'],
    [59_400, '0:59'],
    [3_723_000, '1:02:03'],
    [null, ''],
    [0, ''],
  ])('%s ms → "%s"', (ms, expected) => expect(formatDuration(ms)).toBe(expected))
})

describe('formatRuntime', () => {
  it('uses minutes under an hour and hours above', () => {
    expect(formatRuntime(2_709_000)).toBe('45 min')
    expect(formatRuntime(3_600_000)).toBe('1 hr')
    expect(formatRuntime(4_320_000)).toBe('1 hr 12 min')
  })
})

describe('formatYears', () => {
  it('shows a range, an open range for active artists, or nothing', () => {
    expect(formatYears(1926, 1991)).toBe('1926–1991')
    expect(formatYears(1943, null)).toBe('1943–')
    expect(formatYears(null, null)).toBe('')
  })
})

describe('formatArtistType', () => {
  it('labels people and groups', () => {
    expect(formatArtistType('person')).toBe('Artist')
    expect(formatArtistType('group')).toBe('Band')
    expect(formatArtistType('other')).toBe('')
  })
})

describe('wikipediaUrl', () => {
  it('builds an English Wikipedia link from a title', () => {
    expect(wikipediaUrl("What's Going On (Marvin Gaye album)")).toBe(
      "https://en.wikipedia.org/wiki/What's_Going_On_(Marvin_Gaye_album)",
    )
  })
})
