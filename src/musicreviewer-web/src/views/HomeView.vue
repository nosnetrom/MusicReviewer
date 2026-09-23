<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import AppLogo from '@/components/AppLogo.vue'
import GlassSearchField from '@/components/glass/GlassSearchField.vue'
import RecordingGrid from '@/components/music/RecordingGrid.vue'
import { previewRecordings } from '@/data/previewRecordings'

const router = useRouter()
const query = ref('')

const search = (q) => router.push({ name: 'search', query: { q } })
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
          label="Search artists and albums"
          placeholder="Try “Nina Simone”"
          class="hero__search"
          @submit="search"
        />
      </div>
    </section>

    <section aria-labelledby="featured-heading">
      <h2 id="featured-heading">Essential albums</h2>
      <RecordingGrid :recordings="previewRecordings" :linked="false" />
    </section>
  </div>
</template>

<style scoped>
.home {
  display: grid;
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

/* Phones: logo sits centred above the text. */
@media (max-width: 640px) {
  .hero {
    grid-template-columns: 1fr;
    grid-template-areas: 'logo' 'text';
    gap: 0;
    padding-top: 0;
  }

  .hero__logo {
    justify-self: center;
    width: 9.375rem;
  }
}

.hero__lede {
  font-size: var(--font-size-lg);
}

.hero__search {
  margin-top: var(--space-6);
}
</style>
