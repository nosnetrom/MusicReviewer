/**
 * Average the visible pixels of RGBA data, skipping transparent and near-black/near-white
 * pixels, which would otherwise wash out the tint of most album covers.
 * @param {Uint8ClampedArray} data RGBA bytes, as from CanvasRenderingContext2D.getImageData
 * @returns {{ r: number, g: number, b: number } | null} null when no usable pixels exist
 */
export function averageColor(data) {
  let r = 0
  let g = 0
  let b = 0
  let count = 0

  for (let i = 0; i < data.length; i += 4) {
    const [pr, pg, pb, pa] = [data[i], data[i + 1], data[i + 2], data[i + 3]]
    if (pa < 128) continue
    const max = Math.max(pr, pg, pb)
    const min = Math.min(pr, pg, pb)
    if (max < 24 || min > 232) continue
    r += pr
    g += pg
    b += pb
    count++
  }

  if (count === 0) return null
  return { r: Math.round(r / count), g: Math.round(g / count), b: Math.round(b / count) }
}

/**
 * Sample an image's dominant color by drawing it to a small canvas.
 * Requires the image host to allow CORS (the Cover Art Archive does).
 * @param {string} url
 * @returns {Promise<string | null>} a CSS rgb() color, or null if it could not be read
 */
export async function dominantColor(url) {
  try {
    const img = new Image()
    img.crossOrigin = 'anonymous'
    img.decoding = 'async'
    img.src = url
    await img.decode()

    const size = 32
    const canvas = document.createElement('canvas')
    canvas.width = size
    canvas.height = size
    const ctx = canvas.getContext('2d', { willReadFrequently: true })
    if (!ctx) return null
    ctx.drawImage(img, 0, 0, size, size)

    const color = averageColor(ctx.getImageData(0, 0, size, size).data)
    return color && `rgb(${color.r} ${color.g} ${color.b})`
  } catch {
    return null
  }
}

/**
 * A stable hue (0–359) for a string, used for placeholder artwork until real covers load.
 * @param {string} seed
 */
export function hueFromString(seed) {
  let hash = 0
  for (const char of seed) hash = (hash * 31 + char.codePointAt(0)) | 0
  return Math.abs(hash) % 360
}
