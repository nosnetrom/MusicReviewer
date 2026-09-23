<script setup>
import AppIcon from '@/components/AppIcon.vue'
import GlassPanel from './GlassPanel.vue'
import { dismissToast, toastList } from '@/composables/useToast'
</script>

<template>
  <div class="toasts" role="status" aria-live="polite">
    <TransitionGroup name="toast">
      <GlassPanel
        v-for="toast in toastList"
        :key="toast.id"
        :level="2"
        radius="pill"
        class="toast"
        :class="`toast--${toast.tone}`"
      >
        <span>{{ toast.message }}</span>
        <button
          type="button"
          class="toast__dismiss"
          aria-label="Dismiss"
          @click="dismissToast(toast.id)"
        >
          <AppIcon name="close" :size="14" />
        </button>
      </GlassPanel>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toasts {
  position: fixed;
  z-index: 60;
  top: calc(var(--space-4) + 4rem);
  left: 50%;
  display: grid;
  justify-items: center;
  gap: var(--space-2);
  width: min(28rem, calc(100% - 2 * var(--space-4)));
  translate: -50% 0;
  pointer-events: none;
}

.toast {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-3) var(--space-3) var(--space-5);
  font-size: var(--font-size-sm);
  font-weight: 550;
  pointer-events: auto;
}

.toast--error {
  --glass-tint: var(--color-danger);
}

.toast__dismiss {
  display: grid;
  place-items: center;
  width: 1.75rem;
  height: 1.75rem;
  border: 0;
  border-radius: var(--radius-pill);
  background: var(--color-separator);
  cursor: pointer;
}

.toast-enter-active,
.toast-leave-active {
  transition:
    opacity var(--duration-base) var(--ease-out),
    transform var(--duration-slow) var(--ease-spring);
}

.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateY(-12px) scale(0.95);
}

@media (max-width: 640px) {
  .toasts {
    top: var(--space-4);
  }
}
</style>
