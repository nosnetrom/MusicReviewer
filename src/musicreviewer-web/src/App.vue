<script setup>
import { RouterLink, RouterView } from 'vue-router'
import AdaptiveBackdrop from '@/components/glass/AdaptiveBackdrop.vue'
import GlassNavBar from '@/components/glass/GlassNavBar.vue'
import GlassToastRegion from '@/components/glass/GlassToastRegion.vue'
import ApiStatus from '@/components/ApiStatus.vue'
import { usePreferencesStore } from '@/stores/preferences'

// Instantiating the store applies saved theme/transparency preferences to <html>.
usePreferencesStore()

const links = [
  { to: '/', label: 'Home', icon: 'home', exact: true },
  { to: '/browse', label: 'Browse', icon: 'browse' },
  { to: '/search', label: 'Search', icon: 'search' },
]

const isDev = import.meta.env.DEV
</script>

<template>
  <AdaptiveBackdrop />
  <a class="skip-link" href="#main">Skip to content</a>
  <GlassNavBar :links="links" />

  <main id="main" class="app-main" tabindex="-1">
    <RouterView v-slot="{ Component, route }">
      <Transition name="page" mode="out-in">
        <component :is="Component" :key="route.path" />
      </Transition>
    </RouterView>
  </main>

  <footer class="app-footer">
    <p>
      Discography data from <a href="https://musicbrainz.org/">MusicBrainz</a>. Summaries from
      <a href="https://www.wikipedia.org/">Wikipedia</a>, available under
      <a href="https://creativecommons.org/licenses/by-sa/4.0/">CC BY-SA 4.0</a>.
    </p>
    <div class="app-footer__meta">
      <ApiStatus />
      <RouterLink v-if="isDev" to="/design">Design system</RouterLink>
    </div>
  </footer>

  <GlassToastRegion />
</template>

<style scoped>
.skip-link {
  position: fixed;
  z-index: 100;
  top: var(--space-2);
  left: var(--space-2);
  padding: var(--space-2) var(--space-4);
  border-radius: var(--radius-pill);
  color: var(--color-accent-contrast);
  background: var(--color-accent);
  translate: 0 -200%;
}

.skip-link:focus {
  translate: 0;
}

.app-main {
  width: min(72rem, 100% - 2 * var(--space-4));
  min-height: 70vh;
  margin: 0 auto;
  padding-top: calc(3.5rem + var(--space-7));
  outline: none;
}

.app-footer {
  width: min(72rem, 100% - 2 * var(--space-4));
  margin: var(--space-8) auto 0;
  padding: var(--space-5) 0 var(--space-6);
  border-top: 1px solid var(--color-separator);
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.app-footer a {
  color: inherit;
}

.app-footer__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-4);
}

.page-enter-active,
.page-leave-active {
  transition:
    opacity var(--duration-base) var(--ease-out),
    transform var(--duration-base) var(--ease-out);
}

.page-enter-from {
  opacity: 0;
  transform: translateY(8px);
}

.page-leave-to {
  opacity: 0;
}

/* Leave room for the bottom tab bar on phones. */
@media (max-width: 640px) {
  .app-main {
    padding-top: var(--space-6);
  }

  .app-footer {
    padding-bottom: calc(4rem + var(--space-7));
  }
}
</style>
