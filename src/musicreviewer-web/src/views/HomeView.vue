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
  <section>
    <h1>Discover the recordings that matter.</h1>
    <p>Search famous musicians and explore summaries of their major albums.</p>

    <p class="api-status" role="status" :data-state="state">
      <template v-if="state === 'loading'">Checking API…</template>
      <template v-else-if="state === 'ok'">API status: {{ apiStatus }}</template>
      <template v-else>API unreachable</template>
    </p>
  </section>
</template>

<style scoped>
.api-status {
  font-size: 0.875rem;
  opacity: 0.75;
}

.api-status[data-state='error'] {
  color: #c62828;
  opacity: 1;
}
</style>
