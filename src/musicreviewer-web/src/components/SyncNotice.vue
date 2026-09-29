<script setup>
import GlassButton from '@/components/glass/GlassButton.vue'
import GlassPanel from '@/components/glass/GlassPanel.vue'

/** Shows that data is being imported in the background, or that the import failed. */
const props = defineProps({
  /** @type {import('vue').PropType<import('@/api/catalog').SyncStatus>} */
  status: { type: String, required: true },
  syncingText: { type: String, default: 'Importing from MusicBrainz…' },
  failedText: { type: String, default: 'We couldn’t import this from MusicBrainz.' },
})

defineEmits(['retry'])
</script>

<template>
  <GlassPanel
    v-if="props.status === 'syncing' || props.status === 'notSynced' || props.status === 'failed'"
    radius="pill"
    class="sync-notice"
    :class="{ 'sync-notice--failed': props.status === 'failed' }"
    role="status"
  >
    <template v-if="props.status === 'failed'">
      <span>{{ props.failedText }}</span>
      <GlassButton size="sm" @click="$emit('retry')">Try again</GlassButton>
    </template>
    <template v-else>
      <span class="sync-notice__spinner" aria-hidden="true" />
      <span>{{ props.syncingText }}</span>
    </template>
  </GlassPanel>
</template>

<style scoped>
.sync-notice {
  display: inline-flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-2) var(--space-4);
  font-size: var(--font-size-sm);
  font-weight: 550;
}

.sync-notice--failed {
  --glass-tint: var(--color-danger);
}

.sync-notice__spinner {
  width: 1rem;
  height: 1rem;
  border: 2px solid var(--color-separator);
  border-top-color: var(--color-accent);
  border-radius: 50%;
  animation: spin 0.9s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(1turn);
  }
}

@media (prefers-reduced-motion: reduce) {
  .sync-notice__spinner {
    animation-duration: 3s;
  }
}
</style>
