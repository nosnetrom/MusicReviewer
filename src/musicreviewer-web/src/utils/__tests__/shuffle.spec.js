import { describe, expect, it } from 'vitest'
import { createStableShuffle } from '../shuffle'

const albums = (...ids) => ids.map((mbid) => ({ mbid }))
const ids = (list) => list.map((a) => a.mbid)

/** Hands out the given numbers in turn, standing in for Math.random. */
const sequence = (...values) => {
  let i = 0
  return () => values[i++]
}

describe('createStableShuffle', () => {
  it('orders items by a random rank per item, without changing the input', () => {
    const shuffle = createStableShuffle((a) => a.mbid, sequence(0.7, 0.1, 0.4))
    const input = albums('a', 'b', 'c')

    expect(ids(shuffle(input))).toEqual(['b', 'c', 'a'])
    expect(ids(input)).toEqual(['a', 'b', 'c'])
  })

  it('keeps the same order when the list is refreshed, and slots new items in', () => {
    const shuffle = createStableShuffle((a) => a.mbid, sequence(0.7, 0.1, 0.4, 0.2))
    shuffle(albums('a', 'b', 'c'))

    expect(ids(shuffle(albums('c', 'a', 'b')))).toEqual(['b', 'c', 'a'])
    expect(ids(shuffle(albums('a', 'b', 'c', 'd')))).toEqual(['b', 'd', 'c', 'a'])
  })

  it('gives each shuffler its own order', () => {
    const first = createStableShuffle((a) => a.mbid, sequence(0.1, 0.9))
    const second = createStableShuffle((a) => a.mbid, sequence(0.9, 0.1))

    expect(ids(first(albums('a', 'b')))).toEqual(['a', 'b'])
    expect(ids(second(albums('a', 'b')))).toEqual(['b', 'a'])
  })
})
