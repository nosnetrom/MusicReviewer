<script setup>
import { computed } from 'vue'
import { backdropState } from '@/composables/useBackdrop'

/**
 * Full-viewport layer behind all content. Paints a soft colour field derived from the
 * current tint (usually sampled from album art) so the glass above has something to refract.
 */
const style = computed(() => (backdropState.tint ? { '--backdrop-tint': backdropState.tint } : {}))
</script>

<template>
  <div class="backdrop" aria-hidden="true" :style="style">
    <Transition name="backdrop-image">
      <img
        v-if="backdropState.image"
        :key="backdropState.image"
        :src="backdropState.image"
        alt=""
        class="backdrop__image"
      />
    </Transition>
    <div class="backdrop__field" />
  </div>
</template>

<!-- Not scoped: must respond to data attributes on <html>. Class names are unique to this component. -->
<style>
.backdrop {
  position: fixed;
  z-index: -1;
  inset: 0;
  overflow: hidden;
  background: var(--color-bg);
  transition: --backdrop-tint var(--duration-slow) var(--ease-out);
}

.backdrop__field {
  position: absolute;
  inset: -20%;
  background:
    radial-gradient(
      45% 40% at 18% 12%,
      color-mix(in oklab, var(--backdrop-tint) 75%, var(--color-bg)),
      transparent 70%
    ),
    radial-gradient(
      40% 38% at 86% 18%,
      color-mix(in oklab, oklch(from var(--backdrop-tint) l c calc(h + 55)) 60%, var(--color-bg)),
      transparent 70%
    ),
    radial-gradient(
      55% 45% at 60% 95%,
      color-mix(in oklab, oklch(from var(--backdrop-tint) l c calc(h - 45)) 55%, var(--color-bg)),
      transparent 72%
    );
  filter: blur(40px);
  animation: backdrop-drift 38s ease-in-out infinite alternate;
}

.backdrop__image {
  position: absolute;
  inset: -15%;
  width: 130%;
  height: 130%;
  object-fit: cover;
  opacity: 0.5;
  filter: blur(80px) saturate(1.6);
}

.backdrop-image-enter-active,
.backdrop-image-leave-active {
  transition: opacity var(--duration-slow) var(--ease-out);
}

.backdrop-image-enter-from,
.backdrop-image-leave-to {
  opacity: 0;
}

@keyframes backdrop-drift {
  to {
    transform: translate3d(3%, -2%, 0) rotate(4deg) scale(1.05);
  }
}

@media (prefers-reduced-motion: reduce) {
  .backdrop__field {
    animation: none;
  }
}

/* Reduced transparency: a plain page background, no colour field or artwork. */
@media (prefers-reduced-transparency: reduce) {
  .backdrop__field,
  .backdrop__image {
    display: none;
  }
}

:root[data-transparency='reduced'] .backdrop__field,
:root[data-transparency='reduced'] .backdrop__image {
  display: none;
}
</style>
