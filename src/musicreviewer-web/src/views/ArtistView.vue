<script setup>
import { computed, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import EmptyState from '@/components/EmptyState.vue'
import SyncNotice from '@/components/SyncNotice.vue'
import GlassButton from '@/components/glass/GlassButton.vue'
import GlassChip from '@/components/glass/GlassChip.vue'
import GlassPanel from '@/components/glass/GlassPanel.vue'
import RecordingGrid from '@/components/music/RecordingGrid.vue'
import WikipediaSummary from '@/components/music/WikipediaSummary.vue'
import { getArtist, getArtistRecordings, isSettled } from '@/api/catalog'
import { friendlyApiMessage } from '@/api/client'
import { useCoverBackdrop } from '@/composables/useCoverBackdrop'
import { usePolling } from '@/composables/usePolling'
import { formatArtistType, formatYears, wikipediaUrl } from '@/utils/format'

const props = defineProps({
  /** MusicBrainz artist ID. */
  id: { type: String, required: true },
})

const filters = [
  { value: 'studio', label: 'Albums', empty: 'No studio albums found.' },
  { value: 'ep', label: 'EPs', empty: 'No EPs found.' },
  { value: 'live', label: 'Live', empty: 'No live albums found.' },
  { value: 'compilation', label: 'Compilations', empty: 'No compilations found.' },
]
const filter = ref('studio')
const sort = ref('notability')

const artist = usePolling(({ signal }) => getArtist(props.id, { signal }), {
  interval: 3000,
  until: (a) => isSettled(a.syncStatus),
})

// The artist request creates the artist on first view, so recordings wait for it.
const recordings = usePolling(
  ({ signal }) =>
    getArtistRecordings(props.id, { type: filter.value, sort: sort.value }, { signal }),
  { interval: 2000, until: (r) => isSettled(r.syncStatus), immediate: false },
)

let recordingsStarted = false
watch(artist.data, (a) => {
  if (a && !recordingsStarted) {
    recordingsStarted = true
    recordings.refresh()
  }
  if (a) document.title = `${a.name} · MusicReviewer`
})

watch([filter, sort], () => recordings.refresh())

function retry() {
  artist.refresh()
  if (recordingsStarted) recordings.refresh()
}

const meta = computed(() => {
  const a = artist.data.value
  return a
    ? [formatArtistType(a.type), a.country, formatYears(a.beginYear, a.endYear)]
        .filter(Boolean)
        .join(' · ')
    : ''
})

// Specific MusicBrainz genres, minus any that just repeat a broad genre chip ("jazz" beside Jazz).
const styles = computed(() => {
  const a = artist.data.value
  if (!a?.styles) return []
  const broad = new Set(a.genres.map((g) => g.name.toLowerCase()))
  return a.styles.filter((s) => !broad.has(s.toLowerCase()))
})

const list = computed(() => recordings.data.value?.recordings ?? [])
const status = computed(
  () => recordings.data.value?.syncStatus ?? artist.data.value?.syncStatus ?? 'syncing',
)
const currentFilter = computed(() => filters.find((f) => f.value === filter.value))
const errorStatus = computed(() => artist.error.value?.status)
const artistErrorMessage = computed(() =>
  friendlyApiMessage(artist.error.value, 'We couldn’t load this artist right now.'),
)
const recordingsErrorMessage = computed(() =>
  friendlyApiMessage(recordings.error.value, 'We couldn’t load these recordings right now.'),
)

useCoverBackdrop(() => {
  const top = list.value[0]
  if (top) return { coverUrl: top.coverArtUrl, seed: top.title }
  return artist.data.value ? { seed: artist.data.value.name } : null
})
</script>

<template>
  <EmptyState v-if="errorStatus === 404" title="Artist not found" icon="disc">
    <p>MusicBrainz doesn’t have an artist with this ID.</p>
    <GlassButton variant="primary" to="/search">Search artists</GlassButton>
  </EmptyState>

  <EmptyState
    v-else-if="artist.error.value && !artist.data.value"
    title="MusicBrainz is unavailable"
    icon="disc"
  >
    <p>{{ artistErrorMessage }}</p>
    <GlassButton variant="primary" @click="retry">Try again</GlassButton>
  </EmptyState>

  <div v-else class="artist">
    <GlassPanel :level="2" radius="xl" class="artist__hero">
      <template v-if="artist.data.value">
        <p class="eyebrow">{{ formatArtistType(artist.data.value.type) || 'Artist' }}</p>
        <h1 class="artist__name">{{ artist.data.value.name }}</h1>
        <p v-if="artist.data.value.disambiguation" class="artist__disambiguation">
          {{ artist.data.value.disambiguation }}
        </p>
        <p v-if="meta" class="artist__meta">{{ meta }}</p>

        <ul v-if="artist.data.value.genres.length" class="artist__genres" aria-label="Genres">
          <li v-for="genre in artist.data.value.genres" :key="genre.slug">
            <RouterLink
              :to="{ name: 'browse', query: { genre: genre.slug } }"
              class="glass artist__genre"
            >
              {{ genre.name }}
            </RouterLink>
          </li>
        </ul>
        <p v-if="styles.length" class="artist__styles">
          <span class="artist__styles-label">Styles</span> {{ styles.join(' · ') }}
        </p>

        <WikipediaSummary
          v-if="artist.data.value.summary"
          :summary="artist.data.value.summary"
          class="artist__bio"
        />
        <a
          v-else-if="artist.data.value.wikipediaTitle"
          :href="wikipediaUrl(artist.data.value.wikipediaTitle)"
          class="artist__wikipedia"
          target="_blank"
          rel="noopener"
        >
          Read about {{ artist.data.value.name }} on Wikipedia
        </a>
      </template>
      <div v-else class="artist__hero-placeholder" aria-busy="true">Loading artist…</div>
    </GlassPanel>

    <section aria-labelledby="recordings-heading" class="artist__recordings">
      <div class="artist__toolbar">
        <h2 id="recordings-heading" class="artist__section-title">Major recordings</h2>
        <div class="artist__controls">
          <div class="artist__filters" role="group" aria-label="Recording type">
            <GlassChip
              v-for="option in filters"
              :key="option.value"
              :selected="filter === option.value"
              @update:selected="filter = option.value"
            >
              {{ option.label }}
            </GlassChip>
          </div>
          <div class="artist__sort" role="group" aria-label="Sort">
            <GlassButton
              size="sm"
              :variant="sort === 'notability' ? 'secondary' : 'plain'"
              :aria-pressed="sort === 'notability'"
              @click="sort = 'notability'"
            >
              Most notable
            </GlassButton>
            <GlassButton
              size="sm"
              :variant="sort === 'date' ? 'secondary' : 'plain'"
              :aria-pressed="sort === 'date'"
              @click="sort = 'date'"
            >
              By year
            </GlassButton>
          </div>
        </div>
      </div>

      <SyncNotice
        :status="status"
        :syncing-text="
          list.length
            ? 'Still importing from MusicBrainz — more albums are on the way…'
            : 'Importing albums from MusicBrainz…'
        "
        class="artist__notice"
        @retry="retry"
      />
      <div v-if="recordings.error.value" class="artist__load-error" role="alert">
        <p>{{ recordingsErrorMessage }}</p>
        <GlassButton size="sm" @click="recordings.refresh">Try again</GlassButton>
      </div>

      <EmptyState
        v-if="status === 'ready' && list.length === 0"
        :title="currentFilter.empty"
        icon="disc"
      />
      <RecordingGrid v-else :recordings="list" :placeholders="12" :show-artist="false" />
    </section>
  </div>
</template>

<style scoped>
.artist {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: var(--space-6);
}

.artist__hero {
  padding: var(--space-6);
}

.artist__name {
  font-size: var(--font-size-display);
}

.artist__disambiguation {
  margin-bottom: var(--space-1);
  font-size: var(--font-size-lg);
  color: var(--color-text-secondary);
}

.artist__meta {
  font-size: var(--font-size-sm);
  color: var(--color-text-tertiary);
}

.artist__genres {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  margin: var(--space-4) 0;
  padding: 0;
  list-style: none;
}

.artist__genre {
  --glass-radius: var(--radius-pill);

  display: inline-block;
  padding: var(--space-1) var(--space-3);
  font-size: var(--font-size-sm);
  font-weight: 550;
  color: inherit;
  text-decoration: none;
}

.artist__styles {
  margin: calc(-1 * var(--space-2)) 0 var(--space-4);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.artist__styles-label {
  margin-right: var(--space-1);
  font-size: var(--font-size-xs);
  font-weight: 650;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-text-tertiary);
}

.artist__wikipedia {
  font-size: var(--font-size-sm);
  font-weight: 550;
}

.artist__bio {
  margin-top: var(--space-2);
}

.artist__hero-placeholder {
  min-height: 8rem;
  color: var(--color-text-secondary);
}

.artist__recordings {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: var(--space-4);
}

.artist__toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
}

.artist__section-title {
  margin: 0;
}

.artist__controls,
.artist__filters,
.artist__sort {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-2);
}

.artist__controls {
  gap: var(--space-4);
}

.artist__notice {
  justify-self: start;
}
</style>
