<script setup>
import AppIcon from '@/components/AppIcon.vue'
import GlassPanel from '@/components/glass/GlassPanel.vue'

const props = defineProps({
  title: { type: String, required: true },
  icon: { type: String, default: 'sparkle' },
})
</script>

<template>
  <GlassPanel class="empty-state">
    <AppIcon :name="props.icon" :size="32" class="empty-state__icon" />
    <h2 class="empty-state__title">{{ props.title }}</h2>
    <div class="empty-state__body"><slot /></div>
  </GlassPanel>
</template>

<style scoped>
.empty-state {
  display: grid;
  justify-items: center;
  gap: var(--space-2);
  padding: var(--space-7) var(--space-5);
  text-align: center;
}

.empty-state__icon {
  color: var(--color-accent);
}

.empty-state__title {
  margin: 0;
  font-size: var(--font-size-lg);
}

.empty-state__body {
  max-width: 32rem;
  color: var(--color-text-secondary);
}

.empty-state__body :deep(p:last-child) {
  margin-bottom: 0;
}
</style>
