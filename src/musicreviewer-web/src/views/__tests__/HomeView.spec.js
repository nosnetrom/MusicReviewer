import { describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import HomeView from '../HomeView.vue'
import { getHealth } from '@/api/health'

vi.mock('@/api/health', () => ({ getHealth: vi.fn() }))

describe('HomeView', () => {
  it('shows the API status once the health check resolves', async () => {
    getHealth.mockResolvedValue({ status: 'Healthy', totalDurationMs: 3, checks: [] })

    const wrapper = mount(HomeView)
    await flushPromises()

    expect(wrapper.get('[role="status"]').text()).toBe('API status: Healthy')
  })

  it('reports when the API is unreachable', async () => {
    getHealth.mockRejectedValue(new Error('network'))

    const wrapper = mount(HomeView)
    await flushPromises()

    expect(wrapper.get('[role="status"]').text()).toBe('API unreachable')
  })
})
