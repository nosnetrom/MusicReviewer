import { getJson } from './client'

/**
 * @typedef {object} HealthReport
 * @property {'Healthy' | 'Degraded' | 'Unhealthy'} status
 * @property {number} totalDurationMs
 * @property {{ name: string, status: string, durationMs: number }[]} checks
 */

/** @returns {Promise<HealthReport>} */
export const getHealth = (options) => getJson('/api/health', options)
