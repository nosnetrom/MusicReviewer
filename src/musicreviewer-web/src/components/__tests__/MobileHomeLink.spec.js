import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import MobileHomeLink from '../MobileHomeLink.vue'

const Page = { template: '<div />' }

async function mountAt(path) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'home', component: Page },
      { path: '/browse', name: 'browse', component: Page },
      { path: '/artists/:id', name: 'artist', component: Page },
    ],
  })
  await router.push(path)
  await router.isReady()
  return mount(MobileHomeLink, { global: { plugins: [router] } })
}

describe('MobileHomeLink', () => {
  it('links back to Home with an arrow, the logo and the site name', async () => {
    const wrapper = await mountAt('/artists/abc')
    const link = wrapper.get('a')

    expect(link.attributes('href')).toBe('/')
    expect(link.text()).toBe('MusicReviewer')
    expect(link.find('svg').exists()).toBe(true) // arrow
    expect(link.find('img.app-logo').exists()).toBe(true)
  })

  it('is a link on other pages', async () => {
    expect((await mountAt('/browse')).find('a').exists()).toBe(true)
  })

  it('shows just the logo and site name on Home, without the arrow or a link', async () => {
    const wrapper = await mountAt('/')
    const brand = wrapper.get('.mobile-home-link')

    expect(brand.element.tagName).toBe('DIV')
    expect(brand.text()).toBe('MusicReviewer')
    expect(brand.find('img.app-logo').exists()).toBe(true)
    expect(brand.find('svg').exists()).toBe(false)
    expect(wrapper.find('a').exists()).toBe(false)
  })
})
