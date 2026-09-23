<script setup>
/** Toggleable filter chip (genre, decade, release type). */
const selected = defineModel('selected', { type: Boolean, default: false })
</script>

<template>
  <button
    type="button"
    class="glass chip"
    :class="{ 'chip--selected': selected }"
    :aria-pressed="selected"
    @click="selected = !selected"
  >
    <slot />
  </button>
</template>

<style scoped>
.chip {
  --glass-radius: var(--radius-pill);

  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  height: 2.125rem;
  padding: 0 var(--space-4);
  border: 0;
  font-size: var(--font-size-sm);
  font-weight: 550;
  cursor: pointer;
}

.chip:active {
  transform: scale(0.96);
}

.chip--selected {
  color: var(--color-accent-contrast);
  background: var(--color-accent);
}

.chip--selected::before,
.chip--selected::after {
  opacity: 0.4;
}
</style>
