<script setup>
import { onMounted, ref } from 'vue'
import { getHealth } from '@/api/health'

/** @type {import('vue').Ref<'loading' | 'ok' | 'error'>} */
const state = ref('loading')
const apiStatus = ref('')

onMounted(async () => {
  try {
    const health = await getHealth()
    apiStatus.value = health.status
    state.value = 'ok'
  } catch {
    state.value = 'error'
  }
})
</script>

<template>
  <p class="api-status" role="status" :data-state="state">
    <span class="api-status__dot" aria-hidden="true" />
    <template v-if="state === 'loading'">Checking API…</template>
    <template v-else-if="state === 'ok'">API status: {{ apiStatus }}</template>
    <template v-else>API unreachable</template>
  </p>
</template>

<style scoped>
.api-status {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.api-status__dot {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 50%;
  background: var(--color-text-tertiary);
}

.api-status[data-state='ok'] .api-status__dot {
  background: oklch(0.72 0.17 150);
}

.api-status[data-state='error'] .api-status__dot {
  background: var(--color-danger);
}
</style>
