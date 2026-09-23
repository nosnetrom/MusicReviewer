<script setup>
import { useId, useTemplateRef } from 'vue'
import AppIcon from '@/components/AppIcon.vue'

/** Capsule search input. Emits `submit` with the trimmed query on Enter. */
const props = defineProps({
  label: { type: String, default: 'Search' },
  placeholder: { type: String, default: 'Artists, albums…' },
  size: { type: String, default: 'md', validator: (v) => ['md', 'lg'].includes(v) },
  autofocus: { type: Boolean, default: false },
})

const model = defineModel({ type: String, default: '' })
const emit = defineEmits(['submit'])

const id = useId()
const input = useTemplateRef('input')

function submit() {
  const query = model.value.trim()
  if (query) emit('submit', query)
}

function clear() {
  model.value = ''
  input.value?.focus()
}

defineExpose({ focus: () => input.value?.focus() })
</script>

<template>
  <form
    role="search"
    class="glass glass--2 search-field"
    :class="`search-field--${props.size}`"
    @submit.prevent="submit"
  >
    <label :for="id" class="visually-hidden">{{ props.label }}</label>
    <AppIcon name="search" class="search-field__icon" />
    <input
      :id="id"
      ref="input"
      v-model="model"
      type="search"
      enterkeyhint="search"
      autocomplete="off"
      spellcheck="false"
      :placeholder="props.placeholder"
      :autofocus="props.autofocus"
      class="search-field__input"
    />
    <button
      v-if="model"
      type="button"
      class="search-field__clear"
      aria-label="Clear search"
      @click="clear"
    >
      <AppIcon name="close" :size="14" />
    </button>
  </form>
</template>

<style scoped>
.search-field {
  --glass-radius: var(--radius-pill);
  --_height: 3rem;

  display: flex;
  align-items: center;
  gap: var(--space-2);
  height: var(--_height);
  padding: 0 var(--space-2) 0 var(--space-4);
  transition: box-shadow var(--duration-base) var(--ease-out);
}

.search-field--lg {
  --_height: 3.75rem;
  padding-left: var(--space-5);
  font-size: var(--font-size-lg);
}

.search-field:focus-within {
  box-shadow:
    var(--glass-shadow-2),
    0 0 0 3px color-mix(in oklab, var(--color-focus) 55%, transparent);
}

.search-field__icon {
  flex: none;
  color: var(--color-text-secondary);
}

.search-field__input {
  flex: 1;
  min-width: 0;
  height: 100%;
  border: 0;
  outline: 0;
  background: transparent;
}

.search-field__input::placeholder {
  color: var(--color-text-tertiary);
}

.search-field__input::-webkit-search-cancel-button {
  display: none;
}

.search-field__clear {
  display: grid;
  place-items: center;
  flex: none;
  width: 1.75rem;
  height: 1.75rem;
  border: 0;
  border-radius: var(--radius-pill);
  color: var(--color-bg);
  background: var(--color-text-tertiary);
  cursor: pointer;
}
</style>
