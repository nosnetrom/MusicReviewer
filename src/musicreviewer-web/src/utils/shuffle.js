/**
 * Returns a function that orders lists randomly but stably: each key gets one random rank the
 * first time it is seen, so re-ordering a refreshed list keeps earlier items where they were
 * and slots new ones in at random.
 *
 * @template T
 * @param {(item: T) => string} keyOf
 * @param {() => number} [random]
 * @returns {(items: T[]) => T[]} a shuffled copy; the input is left as it is
 */
export function createStableShuffle(keyOf, random = Math.random) {
  const ranks = new Map()
  const rankOf = (item) => {
    const key = keyOf(item)
    if (!ranks.has(key)) ranks.set(key, random())
    return ranks.get(key)
  }
  // Rank in list order first; sort() visits items in an unspecified order.
  return (items) =>
    items
      .map((item) => ({ item, rank: rankOf(item) }))
      .sort((a, b) => a.rank - b.rank)
      .map(({ item }) => item)
}
