<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppLogo from '@/components/AppLogo.vue'
import EmptyState from '@/components/EmptyState.vue'
import GlassSearchField from '@/components/glass/GlassSearchField.vue'
import ArtistRow from '@/components/music/ArtistRow.vue'
import RecordingGrid from '@/components/music/RecordingGrid.vue'
import { getFeatured, getSearchSuggestions } from '@/api/catalog'
import { usePolling } from '@/composables/usePolling'
import { pickSuggestionForVisit } from '@/utils/suggestions'

const router = useRouter()
const query = ref('')

const search = (q) => router.push({ name: 'search', query: { q } })

// One example musician per visit, picked from the catalog with jazz weighted up.
const placeholder = ref('Try “Nina Simone”')
usePolling(async ({ signal }) => {
  const name = pickSuggestionForVisit(await getSearchSuggestions({ signal }))
  if (name) placeholder.value = `Try “${name}”`
})

// While the featured artists are still being imported, keep checking for new albums.
const { data: featured, error } = usePolling(({ signal }) => getFeatured({ signal }), {
  interval: 10000,
  until: (f) => f.recordings.length >= 12,
})

const recordings = computed(() => featured.value?.recordings ?? [])
const artists = computed(() => featured.value?.artists ?? [])
const loaded = computed(() => featured.value !== null || error.value !== null)
</script>

<template>
  <div class="home">
    <section class="hero">
      <AppLogo :size="375" alt="MusicReviewer logo" class="hero__logo" />
      <div class="hero__text">
        <p class="eyebrow">Discover</p>
        <h1>The recordings that shaped music.</h1>
        <p class="hero__lede text-secondary">
          Search famous musicians, explore their major albums, and read the story behind each one.
        </p>
        <GlassSearchField
          v-model="query"
          size="lg"
          label="Search artists"
          :placeholder="placeholder"
          class="hero__search"
          @submit="search"
        />
      </div>
    </section>

    <section aria-labelledby="featured-heading">
      <h2 id="featured-heading">Essential albums</h2>
      <EmptyState
        v-if="loaded && recordings.length === 0"
        title="The shelves are being stocked"
        icon="disc"
      >
        <p>
          Featured albums appear here as artists are imported. Search for any artist to explore now.
        </p>
      </EmptyState>
      <RecordingGrid v-else :recordings="recordings" :placeholders="12" />
    </section>

    <section v-if="artists.length" aria-labelledby="artists-heading">
      <h2 id="artists-heading">Featured artists</h2>
      <ul class="home__artists">
        <li v-for="artist in artists" :key="artist.mbid">
          <ArtistRow :artist="artist" />
        </li>
      </ul>
    </section>
  </div>
</template>

<style scoped>
/* minmax(0, 1fr) rather than the implicit auto column, so no section can grow past the screen. */
.home {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: var(--space-8);
}

/*
 * Text on the left, logo in the upper right. The logo's column is sized to the logo and
 * the text column absorbs any squeeze, so the logo never collapses at in-between widths.
 */
.hero {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  grid-template-areas: 'text logo';
  align-items: start;
  gap: var(--space-5);
  padding-top: var(--space-6);
}

.hero__text {
  grid-area: text;
  max-width: 44rem;
}

.hero__logo {
  grid-area: logo;
  justify-self: end;
  width: clamp(10.9375rem, 31.25vw, 23.4375rem);
  /* Override the global img max-width so the grid can't compress the logo to zero. */
  max-width: none;
  height: auto;
  filter: drop-shadow(0 18px 32px rgb(0 0 0 / 0.18));
}

.hero__lede {
  font-size: var(--font-size-lg);
}

.hero__search {
  margin-top: var(--space-6);
}

.home__artists {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(20rem, 100%), 1fr));
  gap: var(--space-2);
  margin: 0;
  padding: 0;
  list-style: none;
}

/* Let rows shrink so long names and descriptions truncate instead of widening the list. */
.home__artists > li {
  min-width: 0;
}

/* Phones: logo sits centred above the text. */
@media (max-width: 640px) {
  .hero {
    grid-template-columns: minmax(0, 1fr);
    grid-template-areas: 'logo' 'text';
    gap: 0;
    padding-top: 0;
  }

  .hero__logo {
    justify-self: center;
    width: 9.375rem;
  }
}
</style>
