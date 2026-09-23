import { describe, expect, it } from 'vitest'
import { averageColor, hueFromString } from '../color'

const pixels = (...rgba) => new Uint8ClampedArray(rgba.flat())

describe('averageColor', () => {
  it('averages opaque mid-tone pixels', () => {
    const data = pixels([200, 40, 40, 255], [100, 60, 40, 255])

    expect(averageColor(data)).toEqual({ r: 150, g: 50, b: 40 })
  })

  it('ignores transparent, near-black and near-white pixels', () => {
    const data = pixels(
      [10, 200, 10, 0], // transparent
      [5, 5, 5, 255], // near black
      [250, 250, 250, 255], // near white
      [30, 90, 200, 255],
    )

    expect(averageColor(data)).toEqual({ r: 30, g: 90, b: 200 })
  })

  it('returns null when nothing usable remains', () => {
    expect(averageColor(pixels([0, 0, 0, 255], [255, 255, 255, 255]))).toBeNull()
  })
})

describe('hueFromString', () => {
  it('is stable and within 0–359', () => {
    const hue = hueFromString('Kind of Blue')

    expect(hueFromString('Kind of Blue')).toBe(hue)
    expect(hue).toBeGreaterThanOrEqual(0)
    expect(hue).toBeLessThan(360)
  })

  it('varies between inputs', () => {
    expect(hueFromString('Abbey Road')).not.toBe(hueFromString('Rumours'))
  })
})
