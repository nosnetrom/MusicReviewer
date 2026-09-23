<script setup>
import GlassCard from '@/components/glass/GlassCard.vue'
import CoverArt from './CoverArt.vue'

/** Responsive grid of recording cards. */
const props = defineProps({
  /** @type {import('vue').PropType<{ id: string, title: string, artist: string, year?: number, coverArtUrl?: string }[]>} */
  recordings: { type: Array, required: true },
  /** Link each card to its recording page. */
  linked: { type: Boolean, default: true },
})
</script>

<template>
  <ul class="recording-grid">
    <li v-for="recording in props.recordings" :key="recording.id">
      <GlassCard
        :title="recording.title"
        :subtitle="[recording.artist, recording.year].filter(Boolean).join(' · ')"
        :to="props.linked ? { name: 'recording', params: { id: recording.id } } : null"
      >
        <template #media>
          <CoverArt
            :src="recording.coverArtUrl"
            :alt="`${recording.title} cover art`"
            :seed="recording.title"
          />
        </template>
      </GlassCard>
    </li>
  </ul>
</template>

<style scoped>
.recording-grid {
  display: grid;
  /* Two columns on phones, filling out to six or more on desktop. */
  grid-template-columns: repeat(auto-fill, minmax(min(9.5rem, 100%), 1fr));
  gap: var(--space-4);
  margin: 0;
  padding: 0;
  list-style: none;
}
</style>
