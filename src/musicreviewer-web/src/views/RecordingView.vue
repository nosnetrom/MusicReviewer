<script setup>
import { onMounted } from 'vue'
import GlassPanel from '@/components/glass/GlassPanel.vue'
import CoverArt from '@/components/music/CoverArt.vue'
import { useBackdrop } from '@/composables/useBackdrop'
import { hueFromString } from '@/utils/color'

const props = defineProps({
  id: { type: String, required: true },
})

const { setBackdrop } = useBackdrop()

// Until real cover art is available, tint the page from the same seed as the placeholder cover.
onMounted(() => setBackdrop({ tint: `oklch(0.62 0.18 ${hueFromString(props.id)})` }))
</script>

<template>
  <article class="recording">
    <div class="recording__cover">
      <CoverArt :seed="props.id" alt="Cover art" eager />
    </div>

    <div class="recording__details">
      <p class="eyebrow">Album</p>
      <h1 class="recording__title">Recording</h1>
      <p class="text-secondary">ID: {{ props.id }}</p>

      <GlassPanel class="recording__summary">
        <h2 class="recording__summary-title">About this recording</h2>
        <p class="text-secondary">
          A summary from Wikipedia will appear here once the catalog is connected.
        </p>
        <p class="recording__attribution">
          Summary from Wikipedia, available under
          <a href="https://creativecommons.org/licenses/by-sa/4.0/">CC BY-SA 4.0</a>.
        </p>
      </GlassPanel>
    </div>
  </article>
</template>

<style scoped>
.recording {
  display: grid;
  grid-template-columns: minmax(0, 22rem) minmax(0, 1fr);
  gap: var(--space-7);
  align-items: start;
}

.recording__cover {
  border-radius: var(--radius-lg);
  overflow: hidden;
  box-shadow: var(--glass-shadow-3);
}

.recording__title {
  font-size: var(--font-size-display);
}

.recording__summary {
  margin-top: var(--space-6);
  padding: var(--space-5);
}

.recording__summary-title {
  font-size: var(--font-size-lg);
}

.recording__attribution {
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-tertiary);
}

@media (max-width: 760px) {
  .recording {
    grid-template-columns: 1fr;
    gap: var(--space-5);
  }

  .recording__cover {
    max-width: 18rem;
  }
}
</style>
