<script setup>
import { computed } from 'vue'
import { formatDuration, formatRuntime } from '@/utils/format'

/** Numbered track list, split into discs when there is more than one. */
const props = defineProps({
  /** @type {import('vue').PropType<{ disc: number, position: number, title: string, durationMs: number | null }[]>} */
  tracks: { type: Array, required: true },
})

const discs = computed(() => {
  const byDisc = new Map()
  for (const track of props.tracks) {
    if (!byDisc.has(track.disc)) byDisc.set(track.disc, [])
    byDisc.get(track.disc).push(track)
  }
  return [...byDisc.entries()].sort(([a], [b]) => a - b).map(([disc, tracks]) => ({ disc, tracks }))
})

const runtime = computed(() => {
  const total = props.tracks.reduce((sum, t) => sum + (t.durationMs ?? 0), 0)
  return total > 0 ? formatRuntime(total) : ''
})
</script>

<template>
  <div class="tracks">
    <section v-for="{ disc, tracks } in discs" :key="disc" class="tracks__disc">
      <h3 v-if="discs.length > 1" class="tracks__disc-title">Disc {{ disc }}</h3>
      <ol class="tracks__list">
        <li v-for="track in tracks" :key="`${disc}-${track.position}`" class="tracks__item">
          <span class="tracks__number">{{ track.position }}</span>
          <span class="tracks__title">{{ track.title }}</span>
          <span class="tracks__duration">{{ formatDuration(track.durationMs) }}</span>
        </li>
      </ol>
    </section>
    <p v-if="runtime" class="tracks__runtime">
      {{ props.tracks.length }} {{ props.tracks.length === 1 ? 'track' : 'tracks' }} · {{ runtime }}
    </p>
  </div>
</template>

<style scoped>
.tracks__disc + .tracks__disc {
  margin-top: var(--space-5);
}

.tracks__disc-title {
  margin-bottom: var(--space-2);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.tracks__list {
  margin: 0;
  padding: 0;
  list-style: none;
}

.tracks__item {
  display: grid;
  grid-template-columns: 2rem 1fr auto;
  align-items: baseline;
  gap: var(--space-3);
  padding: var(--space-3) 0;
  border-bottom: 1px solid var(--color-separator);
}

.tracks__number,
.tracks__duration {
  font-size: var(--font-size-sm);
  font-variant-numeric: tabular-nums;
  color: var(--color-text-tertiary);
}

.tracks__number {
  text-align: right;
}

.tracks__runtime {
  margin: var(--space-3) 0 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}
</style>
