<script setup>
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import AppIcon from '@/components/AppIcon.vue'
import { hueFromString } from '@/utils/color'
import { formatArtistType, formatYears } from '@/utils/format'

/** One artist in search results or lists; links to the artist page. */
const props = defineProps({
  /** @type {import('vue').PropType<import('@/api/catalog').ArtistSummary>} */
  artist: { type: Object, required: true },
})

const initials = computed(() =>
  props.artist.name
    .replace(/^the\s+/i, '')
    .split(/\s+/)
    .slice(0, 2)
    .map((word) => word[0])
    .join('')
    .toUpperCase(),
)

const meta = computed(() =>
  [
    formatArtistType(props.artist.type),
    props.artist.country,
    formatYears(props.artist.beginYear, props.artist.endYear),
  ]
    .filter(Boolean)
    .join(' · '),
)
</script>

<template>
  <RouterLink
    :to="{ name: 'artist', params: { id: props.artist.mbid } }"
    class="glass glass--interactive artist-row"
  >
    <span
      class="artist-row__avatar"
      :style="{ '--hue': hueFromString(props.artist.name) }"
      aria-hidden="true"
    >
      {{ initials }}
    </span>
    <span class="artist-row__text">
      <span class="artist-row__name">{{ props.artist.name }}</span>
      <span v-if="props.artist.disambiguation" class="artist-row__disambiguation">
        {{ props.artist.disambiguation }}
      </span>
      <span v-if="meta" class="artist-row__meta">{{ meta }}</span>
    </span>
    <span v-if="props.artist.isImported" class="artist-row__badge">In library</span>
    <AppIcon name="chevronRight" :size="18" class="artist-row__chevron" />
  </RouterLink>
</template>

<style scoped>
.artist-row {
  --glass-radius: var(--radius-md);

  display: flex;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-3) var(--space-4) var(--space-3) var(--space-3);
  color: inherit;
  text-decoration: none;
}

.artist-row__avatar {
  display: grid;
  place-items: center;
  flex: none;
  width: 3rem;
  height: 3rem;
  border-radius: 50%;
  font-weight: 700;
  font-size: var(--font-size-sm);
  color: white;
  background: linear-gradient(
    145deg,
    oklch(0.65 0.16 var(--hue)),
    oklch(0.42 0.14 calc(var(--hue) + 40))
  );
}

.artist-row__text {
  display: grid;
  flex: 1;
  min-width: 0;
}

.artist-row__name {
  font-weight: 650;
}

.artist-row__disambiguation,
.artist-row__meta {
  overflow: hidden;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.artist-row__meta {
  font-size: var(--font-size-xs);
  color: var(--color-text-tertiary);
}

.artist-row__badge {
  flex: none;
  padding: var(--space-1) var(--space-3);
  border-radius: var(--radius-pill);
  font-size: var(--font-size-xs);
  font-weight: 600;
  color: var(--color-accent);
  background: color-mix(in oklab, var(--color-accent) 14%, transparent);
}

.artist-row__chevron {
  flex: none;
  color: var(--color-text-tertiary);
}

@media (max-width: 480px) {
  .artist-row__badge {
    display: none;
  }
}
</style>
