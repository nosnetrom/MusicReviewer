<script setup>
import EmptyState from '@/components/EmptyState.vue'
import GlassPanel from '@/components/glass/GlassPanel.vue'

const props = defineProps({
  id: { type: String, required: true },
})
</script>

<template>
  <div class="artist">
    <GlassPanel :level="2" radius="xl" class="artist__hero">
      <p class="eyebrow">Artist</p>
      <h1 class="artist__name">Artist profile</h1>
      <p class="text-secondary">ID: {{ props.id }}</p>
    </GlassPanel>

    <EmptyState title="Major recordings" icon="disc">
      <p>This artist's ranked discography will appear here once the catalog is connected.</p>
    </EmptyState>
  </div>
</template>

<style scoped>
.artist {
  display: grid;
  gap: var(--space-6);
}

.artist__hero {
  padding: var(--space-6);
}

.artist__name {
  font-size: var(--font-size-xl);
}
</style>
