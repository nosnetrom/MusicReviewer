<script setup>
import { computed, watch } from 'vue'
import { RouterLink } from 'vue-router'
import EmptyState from '@/components/EmptyState.vue'
import SyncNotice from '@/components/SyncNotice.vue'
import GlassButton from '@/components/glass/GlassButton.vue'
import GlassPanel from '@/components/glass/GlassPanel.vue'
import CoverArt from '@/components/music/CoverArt.vue'
import CreditList from '@/components/music/CreditList.vue'
import ListenLinks from '@/components/music/ListenLinks.vue'
import TrackList from '@/components/music/TrackList.vue'
import WikipediaSummary from '@/components/music/WikipediaSummary.vue'
import { getRecording, isSettled } from '@/api/catalog'
import { friendlyApiMessage } from '@/api/client'
import { useCoverBackdrop } from '@/composables/useCoverBackdrop'
import { usePolling } from '@/composables/usePolling'
import { wikipediaUrl } from '@/utils/format'

const props = defineProps({
  /** MusicBrainz release group ID. */
  id: { type: String, required: true },
})

const {
  data: recording,
  error,
  refresh,
} = usePolling(({ signal }) => getRecording(props.id, { signal }), {
  interval: 2000,
  until: (r) => isSettled(r.detailsStatus),
})

const kind = computed(() => {
  const r = recording.value
  if (!r) return ''
  if (r.category === 'live') return 'Live album'
  if (r.category === 'compilation') return 'Compilation'
  return r.primaryType === 'ep' ? 'EP' : 'Album'
})

const meta = computed(() =>
  [recording.value?.year, recording.value?.label].filter(Boolean).join(' · '),
)

watch(recording, (r) => {
  if (r) document.title = `${r.title} · MusicReviewer`
})

useCoverBackdrop(
  () => recording.value && { coverUrl: recording.value.coverArtUrl, seed: recording.value.title },
)
</script>

<template>
  <EmptyState v-if="error?.status === 404" title="Recording not found" icon="disc">
    <p>MusicBrainz doesn’t have a recording with this ID.</p>
    <GlassButton variant="primary" to="/search">Search artists</GlassButton>
  </EmptyState>

  <EmptyState v-else-if="error && !recording" title="MusicBrainz is unavailable" icon="disc">
    <p>{{ friendlyApiMessage(error, 'We couldn’t load this recording right now.') }}</p>
    <GlassButton variant="primary" @click="refresh">Try again</GlassButton>
  </EmptyState>

  <div v-else-if="!recording" class="recording" aria-busy="true">
    <div class="recording__cover"><CoverArt :seed="props.id" alt="" /></div>
    <p class="text-secondary">Loading recording…</p>
  </div>

  <article v-else class="recording">
    <div class="recording__cover">
      <CoverArt
        :src="recording.coverArtUrl"
        :alt="`${recording.title} cover art`"
        :seed="recording.title"
        eager
      />
    </div>

    <div class="recording__details">
      <p class="eyebrow">{{ kind }}</p>
      <h1 class="recording__title">{{ recording.title }}</h1>
      <p class="recording__artist">
        <RouterLink :to="{ name: 'artist', params: { id: recording.artistMbid } }">
          {{ recording.artistCredit ?? recording.artistName }}
        </RouterLink>
      </p>
      <p v-if="meta" class="recording__meta">{{ meta }}</p>

      <GlassPanel class="recording__panel">
        <h2 class="recording__panel-title">About this recording</h2>
        <WikipediaSummary v-if="recording.summary" :summary="recording.summary" />
        <p
          v-else-if="recording.wikipediaTitle && !isSettled(recording.detailsStatus)"
          class="text-secondary"
          aria-busy="true"
        >
          Fetching the summary from Wikipedia…
        </p>
        <!-- Linked, but Wikipedia had no usable lead (for example, a disambiguation page). -->
        <a
          v-else-if="recording.wikipediaTitle"
          :href="wikipediaUrl(recording.wikipediaTitle)"
          target="_blank"
          rel="noopener"
        >
          Read “{{ recording.wikipediaTitle }}” on Wikipedia
        </a>
        <p v-else class="text-secondary">No Wikipedia article is linked to this recording.</p>
      </GlassPanel>

      <GlassPanel class="recording__panel">
        <ListenLinks
          :artist="recording.artistCredit ?? recording.artistName"
          :title="recording.title"
        />
      </GlassPanel>

      <GlassPanel class="recording__panel">
        <h2 class="recording__panel-title">Tracks</h2>
        <SyncNotice
          :status="recording.detailsStatus"
          syncing-text="Importing tracks and personnel…"
          failed-text="We couldn’t import the track list."
          @retry="refresh"
        />
        <TrackList v-if="recording.tracks.length" :tracks="recording.tracks" />
        <p v-else-if="recording.detailsStatus === 'ready'" class="text-secondary">
          No track list is available.
        </p>
      </GlassPanel>

      <GlassPanel v-if="recording.credits.length" class="recording__panel">
        <h2 class="recording__panel-title">Personnel</h2>
        <CreditList :groups="recording.credits" />
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
  position: sticky;
  top: calc(3.5rem + var(--space-6));
  border-radius: var(--radius-lg);
  overflow: hidden;
  box-shadow: var(--glass-shadow-3);
}

.recording__title {
  font-size: clamp(2rem, 1.4rem + 2.6vw, 3.5rem);
}

.recording__artist {
  margin-bottom: var(--space-1);
  font-size: var(--font-size-lg);
  font-weight: 600;
}

.recording__artist a {
  color: inherit;
}

.recording__meta {
  color: var(--color-text-secondary);
}

.recording__panel {
  display: grid;
  gap: var(--space-3);
  margin-top: var(--space-5);
  padding: var(--space-5);
}

.recording__panel-title {
  margin: 0;
  font-size: var(--font-size-lg);
}

.recording__panel :deep(.sync-notice) {
  justify-self: start;
}

@media (max-width: 760px) {
  .recording {
    grid-template-columns: minmax(0, 1fr);
    gap: var(--space-5);
  }

  .recording__cover {
    position: static;
    max-width: 18rem;
  }
}
</style>
