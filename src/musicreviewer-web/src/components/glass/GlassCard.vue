<script setup>
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import GlassPanel from './GlassPanel.vue'

/**
 * Media card: artwork on top, title and subtitle below. Becomes a link when `to` is set.
 * Artwork goes in the `media` slot so callers control aspect ratio and loading.
 */
const props = defineProps({
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
  to: { type: [String, Object], default: null },
  tint: { type: String, default: null },
})

const as = computed(() => (props.to ? RouterLink : 'article'))
</script>

<template>
  <GlassPanel
    :as="as"
    :to="props.to ?? undefined"
    :tint="props.tint"
    :interactive="!!props.to"
    radius="lg"
    class="card"
  >
    <div v-if="$slots.media" class="card__media">
      <slot name="media" />
    </div>
    <div class="card__body">
      <h3 class="card__title">{{ props.title }}</h3>
      <p v-if="props.subtitle" class="card__subtitle">{{ props.subtitle }}</p>
      <slot />
    </div>
  </GlassPanel>
</template>

<style scoped>
.card {
  display: flex;
  flex-direction: column;
  padding: var(--space-2);
  color: inherit;
  text-decoration: none;
}

.card__media {
  overflow: hidden;
  border-radius: calc(var(--radius-lg) - var(--space-2));
}

.card__body {
  padding: var(--space-3) var(--space-2) var(--space-2);
}

.card__title {
  margin: 0;
  font-size: var(--font-size-md);
  font-weight: 650;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.card__subtitle {
  margin: var(--space-1) 0 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}
</style>
