<script setup>
import { computed } from 'vue'
import { RouterLink } from 'vue-router'

/**
 * Capsule button. Renders a RouterLink when `to` is set, an <a> for `href`,
 * and a <button type="button"> otherwise.
 */
const props = defineProps({
  variant: {
    type: String,
    default: 'secondary',
    validator: (v) => ['primary', 'secondary', 'plain'].includes(v),
  },
  size: { type: String, default: 'md', validator: (v) => ['sm', 'md', 'lg'].includes(v) },
  /** Square/circular icon-only button; requires an accessible `aria-label`. */
  iconOnly: { type: Boolean, default: false },
  to: { type: [String, Object], default: null },
  href: { type: String, default: null },
  type: { type: String, default: 'button' },
  disabled: { type: Boolean, default: false },
})

const tag = computed(() => (props.to ? RouterLink : props.href ? 'a' : 'button'))

const attrs = computed(() => {
  if (props.to) return { to: props.to }
  if (props.href) return { href: props.href }
  return { type: props.type, disabled: props.disabled }
})
</script>

<template>
  <component
    :is="tag"
    v-bind="attrs"
    class="glass-button"
    :class="[
      `glass-button--${props.variant}`,
      `glass-button--${props.size}`,
      { 'glass-button--icon': props.iconOnly, glass: props.variant === 'secondary' },
    ]"
  >
    <slot />
  </component>
</template>

<style scoped>
.glass-button {
  --glass-radius: var(--radius-pill);
  --_height: 2.5rem;

  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  min-height: var(--_height);
  padding: 0 calc(var(--_height) * 0.45);
  border: 0;
  border-radius: var(--radius-pill);
  font-weight: 600;
  font-size: var(--font-size-sm);
  line-height: 1;
  white-space: nowrap;
  text-decoration: none;
  color: var(--color-text);
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  transition:
    transform var(--duration-fast) var(--ease-spring),
    background-color var(--duration-fast) var(--ease-out),
    opacity var(--duration-fast) var(--ease-out);
}

.glass-button:active:not(:disabled) {
  transform: scale(0.95);
}

.glass-button:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}

.glass-button--sm {
  --_height: 2rem;
  font-size: var(--font-size-xs);
}

.glass-button--lg {
  --_height: 3.25rem;
  font-size: var(--font-size-md);
}

.glass-button--icon {
  width: var(--_height);
  padding: 0;
}

.glass-button--primary {
  color: var(--color-accent-contrast);
  background: var(--color-accent);
  box-shadow:
    inset 0 1px 0 rgb(255 255 255 / 0.35),
    0 6px 20px color-mix(in oklab, var(--color-accent) 40%, transparent);
}

.glass-button--plain {
  background: transparent;
}

@media (hover: hover) {
  .glass-button--primary:hover:not(:disabled) {
    background: color-mix(in oklab, var(--color-accent) 88%, white);
  }

  .glass-button--plain:hover:not(:disabled) {
    background: var(--color-separator);
  }
}
</style>
