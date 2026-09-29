<script setup>
import GlassCard from '@/components/glass/GlassCard.vue'
import CoverArt from './CoverArt.vue'

/** Responsive grid of recording cards, with shimmering placeholders while loading. */
const props = defineProps({
  /** @type {import('vue').PropType<import('@/api/catalog').RecordingSummary[]>} */
  recordings: { type: Array, default: () => [] },
  /** Number of placeholder cards to show before the first recordings arrive. */
  placeholders: { type: Number, default: 0 },
  /** Show the artist under each title (off on an artist's own page). */
  showArtist: { type: Boolean, default: true },
})

const subtitle = (r) =>
  [props.showArtist ? (r.artistCredit ?? r.artistName) : null, r.year].filter(Boolean).join(' · ')
</script>

<template>
  <ul class="recording-grid" :aria-busy="props.placeholders > 0 && props.recordings.length === 0">
    <li v-for="recording in props.recordings" :key="recording.mbid">
      <GlassCard
        :title="recording.title"
        :subtitle="subtitle(recording)"
        :to="{ name: 'recording', params: { id: recording.mbid } }"
      >
        <template #media>
          <CoverArt
            :src="recording.coverArtUrl"
            :alt="`${recording.title} cover art`"
            :seed="recording.title"
            :size="250"
          />
        </template>
      </GlassCard>
    </li>
    <template v-if="props.recordings.length === 0">
      <li v-for="n in props.placeholders" :key="`placeholder-${n}`" aria-hidden="true">
        <div class="glass placeholder">
          <div class="placeholder__art shimmer" />
          <div class="placeholder__line shimmer" />
          <div class="placeholder__line placeholder__line--short shimmer" />
        </div>
      </li>
    </template>
  </ul>
</template>

<style scoped>
.recording-grid {
  /* Two columns on phones, filling out to six or more on desktop. */
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(9.5rem, 100%), 1fr));
  gap: var(--space-4);
  margin: 0;
  padding: 0;
  list-style: none;
}

.placeholder {
  padding: var(--space-2);
}

.placeholder__art {
  aspect-ratio: 1;
  border-radius: calc(var(--radius-lg) - var(--space-2));
}

.placeholder__line {
  height: 0.8rem;
  margin: var(--space-3) var(--space-2) 0;
  border-radius: var(--radius-pill);
}

.placeholder__line--short {
  width: 55%;
  margin-bottom: var(--space-2);
}

.shimmer {
  background: linear-gradient(
    100deg,
    var(--color-separator) 30%,
    color-mix(in oklab, var(--color-separator) 40%, transparent) 50%,
    var(--color-separator) 70%
  );
  background-size: 300% 100%;
  animation: shimmer 1.6s ease-in-out infinite;
}

@keyframes shimmer {
  from {
    background-position: 100% 0;
  }
  to {
    background-position: 0 0;
  }
}

@media (prefers-reduced-motion: reduce) {
  .shimmer {
    animation: none;
  }
}
</style>
