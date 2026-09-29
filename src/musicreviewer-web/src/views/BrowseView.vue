<script setup>
import { computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import EmptyState from '@/components/EmptyState.vue'
import GlassChip from '@/components/glass/GlassChip.vue'
import RecordingGrid from '@/components/music/RecordingGrid.vue'
import { browseRecordings, getGenres } from '@/api/catalog'
import { usePolling } from '@/composables/usePolling'

const decades = ['1950s', '1960s', '1970s', '1980s', '1990s', '2000s', '2010s', '2020s']

const route = useRoute()
const router = useRouter()

// Filters live in the URL (?genre=jazz&decade=1970s) so views are shareable.
const genre = computed(() => (typeof route.query.genre === 'string' ? route.query.genre : null))
const decade = computed(() => (typeof route.query.decade === 'string' ? route.query.decade : null))

const { data: genres } = usePolling(({ signal }) => getGenres({ signal }))
const results = usePolling(({ signal }) =>
  browseRecordings({ genre: genre.value, decade: decade.value }, { signal }),
)

watch([genre, decade], () => results.refresh())

function choose(key, value, selected) {
  router.replace({ query: { ...route.query, [key]: selected ? value : undefined } })
}

const recordings = computed(() => results.data.value?.recordings ?? [])
const loaded = computed(() => results.data.value !== null)
</script>

<template>
  <div class="browse">
    <header>
      <p class="eyebrow">Browse</p>
      <h1 class="browse__title">Explore by genre and era</h1>
    </header>

    <section v-if="genres?.length" aria-labelledby="genres-heading">
      <h2 id="genres-heading" class="browse__label">Genres</h2>
      <div class="chips">
        <GlassChip
          v-for="g in genres"
          :key="g.slug"
          :selected="genre === g.slug"
          @update:selected="choose('genre', g.slug, $event)"
        >
          {{ g.name }}
        </GlassChip>
      </div>
    </section>

    <section aria-labelledby="decades-heading">
      <h2 id="decades-heading" class="browse__label">Decades</h2>
      <div class="chips">
        <GlassChip
          v-for="d in decades"
          :key="d"
          :selected="decade === d"
          @update:selected="choose('decade', d, $event)"
        >
          {{ d }}
        </GlassChip>
      </div>
    </section>

    <EmptyState v-if="loaded && recordings.length === 0" title="Nothing here yet" icon="disc">
      <p>
        No imported albums match these filters. Try another genre or decade, or search for an
        artist.
      </p>
    </EmptyState>
    <RecordingGrid v-else :recordings="recordings" :placeholders="12" />
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
