import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import CoverArt from '../CoverArt.vue'

const src = 'https://coverartarchive.org/release-group/abc/front-500'

describe('CoverArt', () => {
  it('requests the thumbnail size for its slot and shimmers until loaded', async () => {
    const wrapper = mount(CoverArt, { props: { src, alt: 'Blue cover art', size: 250 } })
    const img = wrapper.get('img')

    expect(img.attributes('src')).toBe('https://coverartarchive.org/release-group/abc/front-250')
    expect(wrapper.classes()).toContain('cover--loading')

    await img.trigger('load')
    expect(wrapper.classes()).not.toContain('cover--loading')
  })

  it('falls back to a generated placeholder when the image fails', async () => {
    const wrapper = mount(CoverArt, { props: { src, alt: 'Blue cover art', seed: 'Blue' } })

    await wrapper.get('img').trigger('error')

    expect(wrapper.find('img').exists()).toBe(false)
    expect(wrapper.get('[role="img"]').attributes('aria-label')).toBe('Blue cover art')
  })
})
