<script setup>
import { computed, ref, watch } from 'vue'
import AppIcon from '@/components/AppIcon.vue'
import { hueFromString } from '@/utils/color'

/**
 * Square album artwork. Falls back to a generated gradient (stable per `seed`)
 * when there is no image or it fails to load.
 */
const props = defineProps({
  src: { type: String, default: null },
  alt: { type: String, default: '' },
  /** Usually the recording title; drives the placeholder colour. */
  seed: { type: String, default: '' },
  eager: { type: Boolean, default: false },
})

const failed = ref(false)
watch(
  () => props.src,
  () => (failed.value = false),
)

const showImage = computed(() => props.src && !failed.value)
const placeholderStyle = computed(() => ({ '--hue': hueFromString(props.seed || props.alt) }))
</script>

<template>
  <div class="cover">
    <img
      v-if="showImage"
      :src="props.src"
      :alt="props.alt"
      :loading="props.eager ? 'eager' : 'lazy'"
      decoding="async"
      class="cover__image"
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

.cover__image {
  width: 100%;
  height: 100%;
  object-fit: cover;
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
</style>
