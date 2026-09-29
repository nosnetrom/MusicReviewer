import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import WikipediaSummary from '../WikipediaSummary.vue'

const summary = (paragraphs) => ({
  title: 'Kind of Blue',
  url: 'https://en.wikipedia.org/wiki/Kind_of_Blue',
  revisionId: 1,
  fetchedUtc: '2026-09-29T12:00:00Z',
  paragraphs,
})

const visibleParagraphs = (wrapper) =>
  wrapper.findAll('p').filter((p) => p.isVisible() && !p.classes('wiki-summary__attribution'))

describe('WikipediaSummary', () => {
  it('shows two paragraphs when the first is short, and hides the rest behind Read more', async () => {
    const wrapper = mount(WikipediaSummary, {
      props: { summary: summary(['Short one.', 'Second.', 'Third.', 'Fourth.']) },
      attachTo: document.body,
    })

    expect(visibleParagraphs(wrapper).map((p) => p.text())).toEqual(['Short one.', 'Second.'])

    const toggle = wrapper.get('button')
    expect(toggle.text()).toBe('Read more')
    expect(toggle.attributes('aria-expanded')).toBe('false')
    expect(document.getElementById(toggle.attributes('aria-controls'))).not.toBeNull()

    await toggle.trigger('click')
    expect(visibleParagraphs(wrapper)).toHaveLength(4)
    expect(toggle.text()).toBe('Show less')
    expect(toggle.attributes('aria-expanded')).toBe('true')
    wrapper.unmount()
  })

  it('shows only the first paragraph when it is long', () => {
    const long = 'x'.repeat(450)
    const wrapper = mount(WikipediaSummary, {
      props: { summary: summary([long, 'Second.']) },
      attachTo: document.body,
    })

    expect(visibleParagraphs(wrapper).map((p) => p.text())).toEqual([long])
    expect(wrapper.find('button').exists()).toBe(true)
    wrapper.unmount()
  })

  it('has no toggle when everything fits', () => {
    const wrapper = mount(WikipediaSummary, { props: { summary: summary(['One.', 'Two.']) } })
    expect(wrapper.find('button').exists()).toBe(false)
  })

  it('credits the article and the CC BY-SA licence', () => {
    const wrapper = mount(WikipediaSummary, { props: { summary: summary(['One.']) } })
    const attribution = wrapper.get('.wiki-summary__attribution')

    expect(attribution.text()).toBe(
      'Excerpt from Wikipedia’s article “Kind of Blue”, available under CC BY-SA 4.0.',
    )
    const [article, licence] = attribution.findAll('a')
    expect(article.attributes('href')).toBe('https://en.wikipedia.org/wiki/Kind_of_Blue')
    expect(licence.attributes('href')).toBe('https://creativecommons.org/licenses/by-sa/4.0/')
    expect(licence.attributes('rel')).toContain('license')
  })
})
