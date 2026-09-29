<script setup>
import { onBeforeUnmount, onMounted, ref, useTemplateRef, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import EmptyState from '@/components/EmptyState.vue'
import GlassSearchField from '@/components/glass/GlassSearchField.vue'
import ArtistRow from '@/components/music/ArtistRow.vue'
import { searchArtists } from '@/api/catalog'
import { useDebounced } from '@/composables/useDebounced'

const route = useRoute()
const router = useRouter()

const queryFromRoute = () => (typeof route.query.q === 'string' ? route.query.q : '')

const query = ref(queryFromRoute())
const debounced = useDebounced(query, 300)
const field = useTemplateRef('field')

/** @type {import('vue').Ref<{ query: string, artists: import('@/api/catalog').ArtistSummary[], remoteAvailable: boolean } | null>} */
const results = ref(null)
const loading = ref(false)
const failed = ref(false)
let controller = null

async function run(text) {
  controller?.abort()
  const q = text.trim()
  if (q.length < 2) {
    results.value = null
    loading.value = false
    failed.value = false
    return
  }

  const current = (controller = new AbortController())
  loading.value = true
  failed.value = false
  try {
    results.value = await searchArtists(q, { signal: current.signal })
  } catch (e) {
    if (e?.name !== 'AbortError') failed.value = true
  } finally {
    if (controller === current) loading.value = false
  }
}

// Search as the visitor types, and keep the URL shareable.
watch(debounced, (text) => {
  const q = text.trim()
  if (q !== queryFromRoute()) router.replace({ query: q ? { q } : {} })
  run(text)
})

// Back/forward navigation changes the query from outside.
watch(
  () => route.query.q,
  () => {
    if (queryFromRoute() !== query.value.trim()) query.value = queryFromRoute()
  },
)

function submit(q) {
  if (q !== queryFromRoute()) router.replace({ query: { q } })
  run(q)
}

onMounted(() => {
  if (query.value) run(query.value)
  else field.value?.focus()
})
onBeforeUnmount(() => controller?.abort())
</script>

<template>
  <div class="search">
    <h1 class="search__title">Search</h1>
    <GlassSearchField
      ref="field"
      v-model="query"
      size="lg"
      label="Search artists"
      placeholder="Search for an artist or band"
      @submit="submit"
    />

    <EmptyState v-if="!results && !loading && !failed" title="Find an artist" icon="search">
      <p>Search by artist or band name to see their major recordings.</p>
    </EmptyState>

    <EmptyState v-else-if="failed" title="Search isn’t working right now" icon="disc">
      <p>Please try again in a moment.</p>
    </EmptyState>

    <ul
      v-else-if="loading && !results"
      class="search__results"
      aria-busy="true"
      aria-label="Loading results"
    >
      <li v-for="n in 4" :key="n" class="glass search__skeleton" aria-hidden="true" />
    </ul>

    <template v-else-if="results">
      <p v-if="!results.remoteAvailable" class="search__notice" role="status">
        MusicBrainz is busy, so only artists already in the library are shown.
      </p>
      <EmptyState
        v-if="results.artists.length === 0"
        :title="`No artists found for “${results.query}”`"
        icon="disc"
      >
        <p>Check the spelling, or try a shorter name.</p>
      </EmptyState>
      <ul
        v-else
        class="search__results"
        :aria-busy="loading"
        :aria-label="`Results for ${results.query}`"
      >
        <li v-for="artist in results.artists" :key="artist.mbid">
          <ArtistRow :artist="artist" />
        </li>
      </ul>
    </template>
  </div>
</template>

<style scoped>
.search {
  display: grid;
  gap: var(--space-5);
  max-width: 48rem;
}

.search__title {
  margin: 0;
  font-size: var(--font-size-xl);
}

.search__results {
  display: grid;
  gap: var(--space-2);
  margin: 0;
  padding: 0;
  list-style: none;
}

.search__results[aria-busy='true'] {
  opacity: 0.7;
  transition: opacity var(--duration-base) var(--ease-out);
}

.search__skeleton {
  --glass-radius: var(--radius-md);

  height: 4.5rem;
}

.search__notice {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}
</style>
