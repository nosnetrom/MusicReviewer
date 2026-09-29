import { describe, expect, it } from 'vitest'
import { JAZZ_WEIGHT, pickSuggestion, pickSuggestionForVisit } from '../suggestions'

/** Deterministic pseudo-random numbers (mulberry32). */
function seeded(seed) {
  return () => {
    seed |= 0
    seed = (seed + 0x6d2b79f5) | 0
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed)
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}

/** A minimal in-memory Storage. */
function memoryStorage() {
  const data = new Map()
  return { getItem: (k) => data.get(k) ?? null, setItem: (k, v) => data.set(k, String(v)) }
}

const artists = [
  { name: 'Miles Davis', genres: ['jazz'] },
  { name: 'Keith Jarrett', genres: ['jazz'] },
  { name: 'Johnny Cash', genres: ['country'] },
  { name: 'Radiohead', genres: ['rock'] },
  { name: 'Aretha Franklin', genres: ['soul-rnb'] },
  { name: 'Joni Mitchell', genres: ['folk', 'pop', 'jazz'] },
]

describe('pickSuggestion', () => {
  it('picks jazz artists about three times as often as others', () => {
    const random = seeded(42)
    const counts = {}
    for (let i = 0; i < 30000; i++) {
      const name = pickSuggestion(artists, { random })
      counts[name] = (counts[name] ?? 0) + 1
    }

    const jazz = (counts['Miles Davis'] + counts['Keith Jarrett'] + counts['Joni Mitchell']) / 3
    const other = (counts['Johnny Cash'] + counts.Radiohead + counts['Aretha Franklin']) / 3
    expect(jazz / other).toBeGreaterThan(JAZZ_WEIGHT * 0.9)
    expect(jazz / other).toBeLessThan(JAZZ_WEIGHT * 1.1)
  })

  it('skips excluded names unless nothing else is left', () => {
    const random = seeded(3)
    for (let i = 0; i < 100; i++) {
      expect(pickSuggestion(artists, { exclude: ['Miles Davis'], random })).not.toBe('Miles Davis')
    }
    expect(pickSuggestion([{ name: 'Only One', genres: [] }], { exclude: ['Only One'] })).toBe(
      'Only One',
    )
  })

  it('returns null with no artists', () => {
    expect(pickSuggestion([])).toBeNull()
  })
})

describe('pickSuggestionForVisit', () => {
  it('never shows the same musician on two visits in a row', () => {
    const storage = memoryStorage()
    const visits = Array.from({ length: 200 }, () => pickSuggestionForVisit(artists, storage))

    visits.slice(1).forEach((name, i) => expect(name).not.toBe(visits[i]))
  })

  it('still picks a musician when storage is blocked', () => {
    const blocked = {
      getItem: () => {
        throw new Error('blocked')
      },
      setItem: () => {
        throw new Error('blocked')
      },
    }

    expect(artists.map((a) => a.name)).toContain(pickSuggestionForVisit(artists, blocked))
  })
})
