<script setup>
import { computed } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import AppIcon from '@/components/AppIcon.vue'
import AppLogo from '@/components/AppLogo.vue'

/**
 * "← [logo] MusicReviewer" link back to Home, in the top-left corner on phones.
 * The nav bar moves to the bottom on phones, so this gives every page a familiar way back
 * at the top. On Home itself it is just the brand, with no arrow and no link.
 * Hidden on wider screens, where the nav bar has the brand.
 */
const route = useRoute()
const resolved = computed(() => route.name !== undefined)
const onHome = computed(() => route.name === 'home')
</script>

<template>
  <div v-if="resolved && onHome" class="mobile-home-link">
    <AppLogo :size="28" class="mobile-home-link__logo" />
    <span class="mobile-home-link__name">MusicReviewer</span>
  </div>
  <RouterLink v-else-if="resolved" to="/" class="mobile-home-link">
    <AppIcon name="arrowLeft" :size="20" class="mobile-home-link__arrow" />
    <AppLogo :size="28" class="mobile-home-link__logo" />
    <span class="mobile-home-link__name">MusicReviewer</span>
  </RouterLink>
</template>

<style scoped>
.mobile-home-link {
  display: none;
}

@media (max-width: 640px) {
  .mobile-home-link {
    display: inline-flex;
    align-items: center;
    gap: var(--space-2);
    /* Clear the notch or status bar on phones that draw under it (viewport-fit=cover). */
    margin: max(var(--space-2), env(safe-area-inset-top)) 0 0 var(--space-4);
    padding: 2px var(--space-2) 2px 0;
    font-weight: 750;
    letter-spacing: var(--tracking-tight);
    color: var(--color-text);
    text-decoration: none;
    border-radius: var(--radius-sm);
  }

  .mobile-home-link__arrow {
    color: var(--color-text-secondary);
    transition: transform var(--duration-fast) var(--ease-out);
  }

  .mobile-home-link:active .mobile-home-link__arrow {
    transform: translateX(-2px);
  }

  .mobile-home-link__logo {
    border-radius: 7px;
  }
}
</style>
