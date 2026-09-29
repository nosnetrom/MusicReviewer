import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import TrackList from '../TrackList.vue'

const kindOfBlue = [
  { disc: 1, position: 1, title: 'So What', durationMs: 545000 },
  { disc: 1, position: 2, title: 'Freddie Freeloader', durationMs: 576000 },
  { disc: 1, position: 3, title: 'Blue in Green', durationMs: 329000 },
]

describe('TrackList', () => {
  it('lists tracks with durations and a total runtime', () => {
    const wrapper = mount(TrackList, { props: { tracks: kindOfBlue } })

    const items = wrapper.findAll('li')
    expect(items).toHaveLength(3)
    expect(items[0].text()).toContain('So What')
    expect(items[0].text()).toContain('9:05')
    expect(wrapper.text()).toContain('3 tracks · 24 min')
    expect(wrapper.find('h3').exists()).toBe(false)
  })

  it('splits multi-disc releases, in disc order', () => {
    const wrapper = mount(TrackList, {
      props: {
        tracks: [
          { disc: 2, position: 1, title: 'Disc two opener', durationMs: null },
          { disc: 1, position: 1, title: 'Disc one opener', durationMs: null },
        ],
      },
    })

    expect(wrapper.findAll('h3').map((h) => h.text())).toEqual(['Disc 1', 'Disc 2'])
    // No durations known: no runtime line.
    expect(wrapper.text()).not.toContain('min')
  })
})
