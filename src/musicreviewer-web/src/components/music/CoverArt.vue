<script setup>
import { computed, ref, watch } from 'vue'
import AppIcon from '@/components/AppIcon.vue'
import { hueFromString } from '@/utils/color'

/**
 * Square album artwork. Shimmers while loading and falls back to a generated gradient
 * (stable per `seed`) when there is no image or it fails to load.
 */
const props = defineProps({
  src: { type: String, default: null },
  alt: { type: String, default: '' },
  /** Usually the recording title; drives the placeholder colour. */
  seed: { type: String, default: '' },
  /** Cover Art Archive thumbnail width: 250 for grids, 500 for detail pages. */
  size: { type: Number, default: 500 },
  eager: { type: Boolean, default: false },
})

const failed = ref(false)
const loaded = ref(false)
watch(
  () => props.src,
  () => {
    failed.value = false
    loaded.value = false
  },
)

// Archive URLs end in "front-500"; request the size this slot actually needs.
const url = computed(() => props.src?.replace(/\/front-\d+$/, `/front-${props.size}`) ?? null)
const showImage = computed(() => url.value && !failed.value)
const placeholderStyle = computed(() => ({ '--hue': hueFromString(props.seed || props.alt) }))
</script>

<template>
  <div class="cover" :class="{ 'cover--loading': showImage && !loaded }">
    <img
      v-if="showImage"
      :src="url"
      :alt="props.alt"
      :loading="props.eager ? 'eager' : 'lazy'"
      decoding="async"
      class="cover__image"
      @load="loaded = true"
      @error="failed = true"
    />
    <div
      v-else
      class="cover__placeholder"
      :style="placeholderStyle"
      role="img"
      :aria-label="props.alt"
    >
      <AppIcon name="disc" :size="36" />
    </div>
  </div>
</template>

<style scoped>
.cover {
  aspect-ratio: 1;
  overflow: hidden;
  border-radius: inherit;
  background: var(--color-separator);
}

.cover--loading {
  background: linear-gradient(
    100deg,
    var(--color-separator) 30%,
    color-mix(in oklab, var(--color-separator) 40%, transparent) 50%,
    var(--color-separator) 70%
  );
  background-size: 300% 100%;
  animation: cover-shimmer 1.6s ease-in-out infinite;
}

.cover__image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: opacity var(--duration-base) var(--ease-out);
}

.cover--loading .cover__image {
  opacity: 0;
}

.cover__placeholder {
  display: grid;
  place-items: center;
  width: 100%;
  height: 100%;
  color: rgb(255 255 255 / 0.75);
  background:
    radial-gradient(circle at 30% 25%, oklch(0.8 0.14 var(--hue)), transparent 60%),
    linear-gradient(145deg, oklch(0.62 0.18 var(--hue)), oklch(0.38 0.14 calc(var(--hue) + 40)));
}

@keyframes cover-shimmer {
  from {
    background-position: 100% 0;
  }
  to {
    background-position: 0 0;
  }
}

@media (prefers-reduced-motion: reduce) {
  .cover--loading {
    animation: none;
  }
}
</style>
