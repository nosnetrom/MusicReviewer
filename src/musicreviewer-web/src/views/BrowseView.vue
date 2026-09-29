<script setup>
import { computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import EmptyState from '@/components/EmptyState.vue'
import GlassButton from '@/components/glass/GlassButton.vue'
import GlassChip from '@/components/glass/GlassChip.vue'
import RecordingGrid from '@/components/music/RecordingGrid.vue'
import { browseRecordings, getGenres } from '@/api/catalog'
import { useLoadMore } from '@/composables/useLoadMore'
import { usePolling } from '@/composables/usePolling'

const decades = ['1950s', '1960s', '1970s', '1980s', '1990s', '2000s', '2010s', '2020s']

const route = useRoute()
const router = useRouter()

// Filters live in the URL (?genre=jazz&decade=1970s) so views are shareable.
const genre = computed(() => (typeof route.query.genre === 'string' ? route.query.genre : null))
const decade = computed(() => (typeof route.query.decade === 'string' ? route.query.decade : null))

const { data: genres } = usePolling(({ signal }) => getGenres({ signal }))

const albums = useLoadMore(async ({ offset, signal }) => {
  const page = await browseRecordings(
    { genre: genre.value, decade: decade.value, offset },
    { signal },
  )
  return { items: page.recordings, total: page.total, hasMore: page.hasMore }
})

watch([genre, decade], () => albums.reset(), { immediate: true })

function choose(key, value, selected) {
  router.replace({ query: { ...route.query, [key]: selected ? value : undefined } })
}

const remaining = computed(() => albums.total.value - albums.items.value.length)
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

    <EmptyState
      v-if="albums.error.value && !albums.loaded.value"
      title="Couldn’t load albums"
      icon="disc"
    >
      <p>Please try again in a moment.</p>
      <GlassButton variant="primary" @click="albums.reset()">Try again</GlassButton>
    </EmptyState>

    <EmptyState
      v-else-if="albums.loaded.value && albums.items.value.length === 0"
      title="Nothing here yet"
      icon="disc"
    >
      <p>
        No imported albums match these filters. Try another genre or decade, or search for an
        artist.
      </p>
    </EmptyState>

    <template v-else>
      <RecordingGrid :recordings="albums.items.value" :placeholders="12" />

      <div v-if="albums.hasMore.value || albums.error.value" class="browse__more">
        <p v-if="albums.error.value" class="browse__error" role="alert">
          Couldn’t load more albums. Please try again.
        </p>
        <GlassButton
          size="lg"
          :disabled="albums.loadingMore.value"
          :aria-busy="albums.loadingMore.value"
          @click="albums.loadMore()"
        >
          {{ albums.loadingMore.value ? 'Loading…' : `Load more (${remaining} more albums)` }}
        </GlassButton>
      </div>
    </template>
  </div>
</template>

<style scoped>
.browse {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
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

.browse__more {
  display: grid;
  justify-items: center;
  gap: var(--space-3);
}

.browse__error {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-danger);
}
</style>
