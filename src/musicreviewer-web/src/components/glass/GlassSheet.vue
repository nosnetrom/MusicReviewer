<script setup>
import { onMounted, useId, useTemplateRef, watch } from 'vue'
import AppIcon from '@/components/AppIcon.vue'
import GlassButton from './GlassButton.vue'

/**
 * Modal sheet built on the native <dialog>, which provides focus trapping, Escape-to-close
 * and inert background for free. Bottom sheet on phones, centred card on wider screens.
 */
const props = defineProps({
  title: { type: String, required: true },
})

const open = defineModel('open', { type: Boolean, default: false })

const titleId = useId()
const dialog = useTemplateRef('dialog')

function sync() {
  const el = dialog.value
  if (!el) return
  if (open.value && !el.open) el.showModal?.()
  else if (!open.value && el.open) el.close()
}

watch(open, sync)
onMounted(sync)

/** Clicks on the ::backdrop target the <dialog> itself, never its content. */
function onClick(event) {
  if (event.target === dialog.value) open.value = false
}
</script>

<template>
  <dialog
    ref="dialog"
    class="sheet"
    :aria-labelledby="titleId"
    @close="open = false"
    @click="onClick"
  >
    <div class="glass glass--3 sheet__panel">
      <header class="sheet__header">
        <h2 :id="titleId" class="sheet__title">{{ props.title }}</h2>
        <GlassButton variant="plain" icon-only size="sm" aria-label="Close" @click="open = false">
          <AppIcon name="close" :size="16" />
        </GlassButton>
      </header>
      <div class="sheet__body">
        <slot />
      </div>
      <footer v-if="$slots.actions" class="sheet__actions">
        <slot name="actions" />
      </footer>
    </div>
  </dialog>
</template>

<style scoped>
.sheet {
  width: min(32rem, calc(100% - 2 * var(--space-4)));
  max-width: none;
  padding: 0;
  border: 0;
  color: inherit;
  background: transparent;
  overflow: visible;
}

.sheet::backdrop {
  background: rgb(0 0 0 / 0.28);
  -webkit-backdrop-filter: blur(4px);
  backdrop-filter: blur(4px);
}

.sheet__panel {
  --glass-radius: var(--radius-xl);

  padding: var(--space-5);
}

.sheet[open] .sheet__panel {
  animation: sheet-in var(--duration-slow) var(--ease-spring);
}

.sheet__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-4);
  margin-bottom: var(--space-3);
}

.sheet__title {
  margin: 0;
  font-size: var(--font-size-lg);
}

.sheet__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
  margin-top: var(--space-5);
}

@keyframes sheet-in {
  from {
    opacity: 0;
    transform: translateY(24px) scale(0.97);
  }
}

@media (max-width: 640px) {
  .sheet {
    width: 100%;
    margin: auto 0 0;
  }

  .sheet__panel {
    border-bottom-left-radius: 0;
    border-bottom-right-radius: 0;
    padding-bottom: max(var(--space-5), env(safe-area-inset-bottom));
  }
}
</style>
