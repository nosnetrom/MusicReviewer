<script setup>
import { computed } from 'vue'

/** A surface of glass material. The building block for every other glass component. */
const props = defineProps({
  /** Element or component to render as. */
  as: { type: [String, Object], default: 'div' },
  /** Elevation: 1 = inline, 2 = floating, 3 = modal. */
  level: { type: Number, default: 1, validator: (v) => [1, 2, 3].includes(v) },
  /** Optional CSS color mixed into the glass fill. */
  tint: { type: String, default: null },
  /** Corner radius token: sm | md | lg | xl | pill. */
  radius: { type: String, default: 'lg' },
  /** Adds hover lift and press feedback for clickable surfaces. */
  interactive: { type: Boolean, default: false },
})

const style = computed(() => ({
  '--glass-radius': `var(--radius-${props.radius})`,
  ...(props.tint && { '--glass-tint': props.tint }),
}))
</script>

<template>
  <component
    :is="props.as"
    class="glass"
    :class="[`glass--${props.level}`, { 'glass--interactive': props.interactive }]"
    :style="style"
  >
    <slot />
  </component>
</template>
