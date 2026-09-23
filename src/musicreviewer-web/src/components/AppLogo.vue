<script setup>
import logoUrl from '@/assets/logo.svg'

/** The MusicReviewer brand mark (Victrola with a Wi-Fi signal). */
const props = defineProps({
  size: { type: [Number, String], default: 32 },
  /** Leave empty when adjacent text already names the brand. */
  alt: { type: String, default: '' },
})
</script>

<template>
  <img
    :src="logoUrl"
    :alt="props.alt"
    :width="props.size"
    :height="props.size"
    class="app-logo"
    decoding="async"
  />
</template>

<style scoped>
.app-logo {
  flex: none;
  object-fit: contain;
}
</style>
