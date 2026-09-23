<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AppIcon from '@/components/AppIcon.vue'
import AppLogo from '@/components/AppLogo.vue'
import GlassButton from './GlassButton.vue'
import { usePreferencesStore } from '@/stores/preferences'

/**
 * Floating capsule navigation. Top-centred on wide screens, a bottom tab bar on phones.
 * Tightens up once the page scrolls.
 */
const props = defineProps({
  /** @type {import('vue').PropType<{ to: string, label: string, icon: string, exact?: boolean }[]>} */
  links: { type: Array, required: true },
})

const preferences = usePreferencesStore()
const compact = ref(false)

const onScroll = () => (compact.value = window.scrollY > 24)

onMounted(() => {
  onScroll()
  window.addEventListener('scroll', onScroll, { passive: true })
})
onBeforeUnmount(() => window.removeEventListener('scroll', onScroll))

const themeIcon = computed(
  () => ({ system: 'system', light: 'sun', dark: 'moon' })[preferences.theme],
)
const themeLabel = computed(() => `Theme: ${preferences.theme}. Switch theme`)
</script>

<template>
  <nav class="glass glass--2 nav" :class="{ 'nav--compact': compact }" aria-label="Primary">
    <RouterLink to="/" class="nav__brand">
      <AppLogo :size="36" class="nav__logo" />
      <span class="nav__brand-name">MusicReviewer</span>
    </RouterLink>

    <ul class="nav__links">
      <li v-for="link in props.links" :key="link.to">
        <RouterLink
          :to="link.to"
          class="nav__link"
          exact-active-class="is-active"
          :active-class="link.exact ? '' : 'is-active'"
        >
          <AppIcon :name="link.icon" />
          <span class="nav__label">{{ link.label }}</span>
        </RouterLink>
      </li>
    </ul>

    <GlassButton
      variant="plain"
      icon-only
      class="nav__theme"
      :aria-label="themeLabel"
      :title="themeLabel"
      @click="preferences.cycleTheme()"
    >
      <AppIcon :name="themeIcon" />
    </GlassButton>
  </nav>
</template>

<style scoped>
.nav {
  --glass-radius: var(--radius-pill);

  position: fixed;
  z-index: 50;
  top: var(--space-4);
  left: 50%;
  display: flex;
  align-items: center;
  gap: var(--space-4);
  width: min(56rem, calc(100% - 2 * var(--space-4)));
  height: 3.5rem;
  padding: 0 var(--space-2) 0 var(--space-3);
  translate: -50% 0;
  transition:
    width var(--duration-slow) var(--ease-spring),
    height var(--duration-base) var(--ease-out),
    top var(--duration-base) var(--ease-out);
}

.nav--compact {
  top: var(--space-2);
  width: min(44rem, calc(100% - 2 * var(--space-4)));
  height: 3rem;
}

.nav__brand {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  font-weight: 750;
  letter-spacing: var(--tracking-tight);
  color: inherit;
  text-decoration: none;
}

.nav__links {
  display: flex;
  gap: var(--space-1);
  margin: 0 0 0 auto;
  padding: 0;
  list-style: none;
}

.nav__link {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  height: 2.5rem;
  padding: 0 var(--space-4);
  border-radius: var(--radius-pill);
  font-size: var(--font-size-sm);
  font-weight: 600;
  color: var(--color-text-secondary);
  text-decoration: none;
  transition:
    background-color var(--duration-fast) var(--ease-out),
    color var(--duration-fast) var(--ease-out);
}

.nav__link:hover {
  color: var(--color-text);
}

.nav__link.is-active {
  color: var(--color-text);
  background: var(--color-separator);
}

/* Phones: a bottom tab bar with stacked icon + label, brand hidden. */
@media (max-width: 640px) {
  .nav,
  .nav--compact {
    top: auto;
    bottom: max(var(--space-3), env(safe-area-inset-bottom));
    width: calc(100% - 2 * var(--space-4));
    height: 4rem;
    padding: 0 var(--space-2);
  }

  /* Logo only; the name stays available to screen readers. */
  .nav__brand-name {
    position: absolute;
    width: 1px;
    height: 1px;
    overflow: hidden;
    clip-path: inset(50%);
    white-space: nowrap;
  }

  .nav__links {
    flex: 1;
    justify-content: space-around;
    margin: 0;
  }

  .nav__link {
    flex-direction: column;
    gap: 2px;
    height: 3.25rem;
    padding: 0 var(--space-4);
    font-size: 0.6875rem;
  }
}
</style>
