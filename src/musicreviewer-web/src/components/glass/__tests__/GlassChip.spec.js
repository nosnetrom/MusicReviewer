import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import GlassChip from '../GlassChip.vue'

describe('GlassChip', () => {
  it('toggles its pressed state on click', async () => {
    const wrapper = mount(GlassChip, {
      props: {
        selected: false,
        'onUpdate:selected': (value) => wrapper.setProps({ selected: value }),
      },
      slots: { default: 'Jazz' },
    })
    const button = wrapper.get('button')
    expect(button.attributes('aria-pressed')).toBe('false')

    await button.trigger('click')
    expect(button.attributes('aria-pressed')).toBe('true')
    expect(button.classes()).toContain('chip--selected')

    await button.trigger('click')
    expect(button.attributes('aria-pressed')).toBe('false')
  })
})
