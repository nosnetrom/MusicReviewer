import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import ListenLinks from '../ListenLinks.vue'

describe('ListenLinks', () => {
  const wrapper = mount(ListenLinks, {
    props: { artist: 'Nina Simone', title: 'I Put a Spell on You' },
  })
  const links = wrapper.findAll('a')

  it('renders a div with one link per service, with the notes visible', () => {
    expect(wrapper.element.tagName).toBe('DIV')
    expect(links).toHaveLength(3)
    expect(links[0].text()).toContain('YouTube Music')
    expect(links[1].find('.listen__note').text()).toBe('(USA only)')
    expect(links[2].find('.listen__note').text()).toBe('(requires account)')
  })

  it('starts each button with its service icon, hidden from screen readers', () => {
    for (const link of links) {
      const icon = link.find('img')
      expect(icon.exists()).toBe(true)
      expect(icon.attributes('alt')).toBe('')
      expect(link.element.firstElementChild).toBe(icon.element)
    }
    // Vite inlines these small images as data URIs, so check each button has its own icon.
    const sources = links.map((a) => a.find('img').attributes('src'))
    expect(sources.every(Boolean)).toBe(true)
    expect(new Set(sources).size).toBe(3)
  })

  it('names each link for screen readers, including that it opens a new tab', () => {
    expect(links.map((a) => a.attributes('aria-label'))).toEqual([
      'Search YouTube Music for this album, opens in a new tab',
      'Search Pandora for this album (USA only), opens in a new tab',
      'Search Amazon Music for this album (requires account), opens in a new tab',
    ])
  })

  it('opens every link in a new tab without exposing this page', () => {
    for (const link of links) {
      expect(link.attributes('target')).toBe('_blank')
      expect(link.attributes('rel')).toBe('noopener noreferrer')
      expect(link.attributes('href')).toMatch(/^https:\/\//)
    }
  })
})
