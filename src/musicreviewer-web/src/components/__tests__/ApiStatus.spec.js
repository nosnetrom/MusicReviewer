import { describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import ApiStatus from '../ApiStatus.vue'
import { getHealth } from '@/api/health'

vi.mock('@/api/health', () => ({ getHealth: vi.fn() }))

describe('ApiStatus', () => {
  it('shows the API status once the health check resolves', async () => {
    getHealth.mockResolvedValue({ status: 'Healthy', totalDurationMs: 3, checks: [] })

    const wrapper = mount(ApiStatus)
    await flushPromises()

    expect(wrapper.text()).toBe('API status: Healthy')
    expect(wrapper.attributes('data-state')).toBe('ok')
  })

  it('reports when the API is unreachable', async () => {
    getHealth.mockRejectedValue(new Error('network'))

    const wrapper = mount(ApiStatus)
    await flushPromises()

    expect(wrapper.text()).toBe('API unreachable')
    expect(wrapper.attributes('data-state')).toBe('error')
  })
})
