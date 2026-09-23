import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import GlassSearchField from '../GlassSearchField.vue'

function mountField(modelValue = '') {
  const wrapper = mount(GlassSearchField, {
    props: {
      modelValue,
      'onUpdate:modelValue': (value) => wrapper.setProps({ modelValue: value }),
    },
  })
  return wrapper
}

describe('GlassSearchField', () => {
  it('labels the input for assistive technology', () => {
    const wrapper = mountField()
    const input = wrapper.get('input')

    expect(wrapper.get(`label[for="${input.attributes('id')}"]`).text()).toBe('Search')
    expect(wrapper.get('form').attributes('role')).toBe('search')
  })

  it('emits the trimmed query on submit', async () => {
    const wrapper = mountField()

    await wrapper.get('input').setValue('  Nina Simone ')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('submit')).toEqual([['Nina Simone']])
  })

  it('does not submit a blank query', async () => {
    const wrapper = mountField('   ')

    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('submit')).toBeUndefined()
  })

  it('shows a clear button only when there is text, and clears on click', async () => {
    const wrapper = mountField()
    expect(wrapper.find('button[aria-label="Clear search"]').exists()).toBe(false)

    await wrapper.get('input').setValue('Prince')
    await wrapper.get('button[aria-label="Clear search"]').trigger('click')

    expect(wrapper.props('modelValue')).toBe('')
  })
})
