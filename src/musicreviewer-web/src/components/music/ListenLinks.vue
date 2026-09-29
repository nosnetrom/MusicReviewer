<script setup>
import { computed } from 'vue'
import AppIcon from '@/components/AppIcon.vue'
import GlassButton from '@/components/glass/GlassButton.vue'
import { listenLinks } from '@/utils/listen'

/** "Listen on" buttons that search streaming services for this album, each in a new tab. */
const props = defineProps({
  artist: { type: String, required: true },
  title: { type: String, required: true },
})

const links = computed(() => listenLinks({ artist: props.artist, title: props.title }))

/** e.g. "Search Pandora for this album (USA only), opens in a new tab". */
const accessibleName = (link) =>
  `Search ${link.label} for this album${link.note ? ` (${link.note})` : ''}, opens in a new tab`
</script>

<template>
  <div class="listen">
    <h2 class="listen__title">Listen</h2>
    <ul class="listen__links">
      <li v-for="link in links" :key="link.service">
        <GlassButton
          :href="link.url"
          target="_blank"
          rel="noopener noreferrer"
          :aria-label="accessibleName(link)"
          class="listen__link"
        >
          <img
            :src="link.icon"
            alt=""
            width="18"
            height="18"
            class="listen__icon"
            :class="`listen__icon--${link.service}`"
          />
          <span>{{ link.label }}</span>
          <span v-if="link.note" class="listen__note">({{ link.note }})</span>
          <AppIcon name="external" :size="14" />
        </GlassButton>
      </li>
    </ul>
    <p class="listen__hint">Opens a search on each service.</p>
  </div>
</template>

<style scoped>
.listen {
  display: grid;
  gap: var(--space-3);
}

.listen__title {
  margin: 0;
  font-size: var(--font-size-lg);
}

.listen__links {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  margin: 0;
  padding: 0;
  list-style: none;
}

.listen__icon {
  flex: none;
  width: 1.125rem;
  height: 1.125rem;
}

/* Amazon's mark is a filled app tile; round it like one. */
.listen__icon--amazon-music {
  border-radius: 4px;
}

/* Pandora's dark blue "P" sits on a small light badge so it stays legible on dark glass. */
.listen__icon--pandora {
  box-sizing: border-box;
  padding: 2px;
  border-radius: 4px;
  background: #fff;
  box-shadow: 0 0 0 1px rgb(0 0 0 / 0.08);
}

.listen__note {
  font-weight: 450;
  color: var(--color-text-secondary);
}

/*
 * Narrow screens: one full-width button per row, and let a note drop to a second line.
 * Buttons don't wrap by default, and "Amazon Music (requires account)" is wider than a phone panel.
 */
@media (max-width: 480px) {
  .listen__links {
    flex-direction: column;
  }

  .listen__link {
    flex-wrap: wrap;
    justify-content: flex-start;
    width: 100%;
    padding-block: var(--space-2);
    line-height: 1.3;
    white-space: normal;
    text-align: left;
  }
}

.listen__hint {
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-tertiary);
}
</style>
