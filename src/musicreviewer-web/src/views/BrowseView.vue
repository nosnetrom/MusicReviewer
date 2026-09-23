<script setup>
import { reactive } from 'vue'
import GlassChip from '@/components/glass/GlassChip.vue'
import RecordingGrid from '@/components/music/RecordingGrid.vue'
import { decades, genres, previewRecordings } from '@/data/previewRecordings'

const selectedGenres = reactive(new Set())
const selectedDecades = reactive(new Set())

function toggle(set, value, on) {
  if (on) set.add(value)
  else set.delete(value)
}
</script>

<template>
  <div class="browse">
    <header>
      <p class="eyebrow">Browse</p>
      <h1 class="browse__title">Explore by genre and era</h1>
    </header>

    <section aria-labelledby="genres-heading">
      <h2 id="genres-heading" class="browse__label">Genres</h2>
      <div class="chips">
        <GlassChip
          v-for="genre in genres"
          :key="genre"
          :selected="selectedGenres.has(genre)"
          @update:selected="toggle(selectedGenres, genre, $event)"
        >
          {{ genre }}
        </GlassChip>
      </div>
    </section>

    <section aria-labelledby="decades-heading">
      <h2 id="decades-heading" class="browse__label">Decades</h2>
      <div class="chips">
        <GlassChip
          v-for="decade in decades"
          :key="decade"
          :selected="selectedDecades.has(decade)"
          @update:selected="toggle(selectedDecades, decade, $event)"
        >
          {{ decade }}
        </GlassChip>
      </div>
    </section>

    <RecordingGrid :recordings="previewRecordings" :linked="false" />
  </div>
</template>

<style scoped>
.browse {
  display: grid;
  gap: var(--space-6);
}

.browse__title {
  font-size: var(--font-size-xl);
}

.browse__label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.chips {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}
</style>
