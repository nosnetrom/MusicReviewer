import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import GlassButton from '../GlassButton.vue'

const router = createRouter({
  history: createMemoryHistory(),
  routes: [{ path: '/:any(.*)*', component: { template: '<div />' } }],
})

const mountButton = (props = {}) =>
  mount(GlassButton, { props, slots: { default: 'Go' }, global: { plugins: [router] } })

describe('GlassButton', () => {
  it('renders a non-submitting button by default', () => {
    const button = mountButton().get('button')

    expect(button.attributes('type')).toBe('button')
    expect(button.text()).toBe('Go')
  })

  it('renders a router link when given a route', () => {
    const link = mountButton({ to: '/browse' }).get('a')

    expect(link.attributes('href')).toBe('/browse')
  })

  it('renders a plain anchor for external links', () => {
    const link = mountButton({ href: 'https://musicbrainz.org/' }).get('a')

    expect(link.attributes('href')).toBe('https://musicbrainz.org/')
  })

  it('uses glass material only for the secondary variant', () => {
    expect(mountButton({ variant: 'secondary' }).classes()).toContain('glass')
    expect(mountButton({ variant: 'primary' }).classes()).not.toContain('glass')
  })

  it('passes disabled through to the button', () => {
    expect(mountButton({ disabled: true }).get('button').attributes()).toHaveProperty('disabled')
  })
})
