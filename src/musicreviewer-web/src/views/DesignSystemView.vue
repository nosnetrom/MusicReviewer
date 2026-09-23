<script setup>
import { ref } from 'vue'
import AppIcon, { iconNames } from '@/components/AppIcon.vue'
import GlassButton from '@/components/glass/GlassButton.vue'
import GlassCard from '@/components/glass/GlassCard.vue'
import GlassChip from '@/components/glass/GlassChip.vue'
import GlassPanel from '@/components/glass/GlassPanel.vue'
import GlassSearchField from '@/components/glass/GlassSearchField.vue'
import GlassSheet from '@/components/glass/GlassSheet.vue'
import CoverArt from '@/components/music/CoverArt.vue'
import { useBackdrop } from '@/composables/useBackdrop'
import { useToast } from '@/composables/useToast'
import { usePreferencesStore } from '@/stores/preferences'

const preferences = usePreferencesStore()
const { setBackdrop } = useBackdrop()
const { showToast } = useToast()

const themes = [
  { value: 'system', label: 'System', icon: 'system' },
  { value: 'light', label: 'Light', icon: 'sun' },
  { value: 'dark', label: 'Dark', icon: 'moon' },
]

const tints = [
  { label: 'Default', value: null },
  { label: 'Indigo', value: 'oklch(0.6 0.2 275)' },
  { label: 'Coral', value: 'oklch(0.7 0.17 30)' },
  { label: 'Teal', value: 'oklch(0.68 0.12 190)' },
  { label: 'Amber', value: 'oklch(0.8 0.15 80)' },
  { label: 'Rose', value: 'oklch(0.65 0.2 350)' },
]
const activeTint = ref(null)

function selectTint(value) {
  activeTint.value = value
  setBackdrop({ tint: value })
}

const query = ref('')
const chips = ref({ Jazz: true, Soul: false, Rock: false, Live: false })
const sheetOpen = ref(false)
</script>

<template>
  <div class="ds">
    <header>
      <p class="eyebrow">Design system</p>
      <h1>Liquid glass</h1>
      <p class="text-secondary ds__lede">
        Translucent, layered surfaces that take their colour from the content behind them. Toggle
        theme and transparency to check every state.
      </p>
    </header>

    <!-- Display controls -->
    <GlassPanel :level="2" class="ds__panel ds__controls">
      <div>
        <h2 class="ds__h">Theme</h2>
        <div class="ds__row" role="group" aria-label="Theme">
          <GlassButton
            v-for="t in themes"
            :key="t.value"
            :variant="preferences.theme === t.value ? 'primary' : 'secondary'"
            :aria-pressed="preferences.theme === t.value"
            @click="preferences.theme = t.value"
          >
            <AppIcon :name="t.icon" :size="16" /> {{ t.label }}
          </GlassButton>
        </div>
      </div>
      <div>
        <h2 class="ds__h">Accessibility</h2>
        <label class="ds__toggle">
          <input v-model="preferences.reduceTransparency" type="checkbox" />
          Reduce transparency
        </label>
        <p class="ds__note">
          Also follows the OS setting (<code>prefers-reduced-transparency</code>). Motion follows
          <code>prefers-reduced-motion</code>.
        </p>
      </div>
      <div>
        <h2 class="ds__h">Backdrop tint</h2>
        <div class="ds__row" role="group" aria-label="Backdrop tint">
          <button
            v-for="t in tints"
            :key="t.label"
            type="button"
            class="ds__swatch"
            :class="{ 'is-active': activeTint === t.value }"
            :style="{ '--swatch': t.value ?? 'var(--backdrop-tint)' }"
            :aria-label="t.label"
            :aria-pressed="activeTint === t.value"
            @click="selectTint(t.value)"
          />
        </div>
      </div>
    </GlassPanel>

    <!-- Materials -->
    <section>
      <h2>Material levels</h2>
      <div class="ds__stage">
        <div class="ds__stage-art" aria-hidden="true" />
        <GlassPanel v-for="level in [1, 2, 3]" :key="level" :level="level" class="ds__level">
          <strong>Level {{ level }}</strong>
          <span class="text-secondary">{{ ['Inline', 'Floating', 'Modal'][level - 1] }}</span>
        </GlassPanel>
      </div>
    </section>

    <section>
      <h2>Tinted glass</h2>
      <div class="ds__grid">
        <GlassPanel
          v-for="t in tints.slice(1)"
          :key="t.label"
          :tint="t.value"
          :level="2"
          class="ds__tile"
        >
          {{ t.label }}
        </GlassPanel>
      </div>
    </section>

    <!-- Controls -->
    <section>
      <h2>Buttons</h2>
      <div class="ds__stack">
        <div v-for="size in ['sm', 'md', 'lg']" :key="size" class="ds__row">
          <GlassButton variant="primary" :size="size">Primary</GlassButton>
          <GlassButton variant="secondary" :size="size">Secondary</GlassButton>
          <GlassButton variant="plain" :size="size">Plain</GlassButton>
          <GlassButton icon-only :size="size" aria-label="Search">
            <AppIcon name="search" :size="size === 'sm' ? 14 : 18" />
          </GlassButton>
        </div>
        <div class="ds__row">
          <GlassButton variant="primary" disabled>Disabled</GlassButton>
          <GlassButton disabled>Disabled</GlassButton>
          <GlassButton to="/browse"
            >Router link <AppIcon name="chevronRight" :size="14"
          /></GlassButton>
        </div>
      </div>
    </section>

    <section>
      <h2>Search field</h2>
      <div class="ds__stack ds__narrow">
        <GlassSearchField v-model="query" @submit="showToast(`Searched for “${$event}”`)" />
        <GlassSearchField v-model="query" size="lg" />
      </div>
    </section>

    <section>
      <h2>Chips</h2>
      <div class="ds__row">
        <GlassChip v-for="(on, name) in chips" :key="name" v-model:selected="chips[name]">
          {{ name }}
        </GlassChip>
      </div>
    </section>

    <section>
      <h2>Cards</h2>
      <div class="ds__cards">
        <GlassCard title="Kind of Blue" subtitle="Miles Davis · 1959" to="/recordings/kind-of-blue">
          <template #media><CoverArt seed="Kind of Blue" alt="Kind of Blue cover art" /></template>
        </GlassCard>
        <GlassCard title="Blue" subtitle="Joni Mitchell · 1971">
          <template #media><CoverArt seed="Blue" alt="Blue cover art" /></template>
        </GlassCard>
        <GlassCard title="Text-only card" subtitle="No media slot">
          <p class="ds__note">Cards without artwork work for artists and genres.</p>
        </GlassCard>
      </div>
    </section>

    <section>
      <h2>Sheet &amp; toast</h2>
      <div class="ds__row">
        <GlassButton variant="primary" @click="sheetOpen = true">Open sheet</GlassButton>
        <GlassButton @click="showToast('Saved to your library')">Show toast</GlassButton>
        <GlassButton @click="showToast('Something went wrong', { tone: 'error' })">
          Show error toast
        </GlassButton>
      </div>
      <GlassSheet v-model:open="sheetOpen" title="Filter recordings">
        <p class="text-secondary">Sheets use the native dialog: focus is trapped and Esc closes.</p>
        <div class="ds__row">
          <GlassChip v-for="(on, name) in chips" :key="name" v-model:selected="chips[name]">
            {{ name }}
          </GlassChip>
        </div>
        <template #actions>
          <GlassButton variant="plain" @click="sheetOpen = false">Cancel</GlassButton>
          <GlassButton variant="primary" @click="sheetOpen = false">Apply</GlassButton>
        </template>
      </GlassSheet>
    </section>

    <section>
      <h2>Icons</h2>
      <div class="ds__row">
        <GlassPanel v-for="name in iconNames" :key="name" radius="md" class="ds__icon">
          <AppIcon :name="name" :size="22" />
          <span>{{ name }}</span>
        </GlassPanel>
      </div>
    </section>

    <section>
      <h2>Typography</h2>
      <GlassPanel class="ds__panel">
        <p class="eyebrow">Eyebrow</p>
        <h1>Display heading</h1>
        <h2>Section heading</h2>
        <h3>Card heading</h3>
        <p>Body text. The quick brown fox jumps over the lazy dog.</p>
        <p class="text-secondary">Secondary text for supporting details.</p>
      </GlassPanel>
    </section>
  </div>
</template>

<style scoped>
.ds {
  display: grid;
  gap: var(--space-7);
}

.ds__lede {
  max-width: 40rem;
  font-size: var(--font-size-lg);
}

.ds__panel {
  padding: var(--space-5);
}

.ds__controls {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(15rem, 1fr));
  gap: var(--space-5);
}

.ds__h {
  margin-bottom: var(--space-3);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.ds__row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-2);
}

.ds__stack {
  display: grid;
  gap: var(--space-3);
}

.ds__narrow {
  max-width: 32rem;
}

.ds__toggle {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  font-weight: 550;
}

.ds__toggle input {
  width: 1.125rem;
  height: 1.125rem;
  accent-color: var(--color-accent);
}

.ds__note {
  margin: var(--space-2) 0 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.ds__swatch {
  width: 2rem;
  height: 2rem;
  border: 2px solid transparent;
  border-radius: 50%;
  background: var(--swatch);
  box-shadow: inset 0 0 0 1px rgb(0 0 0 / 0.12);
  cursor: pointer;
}

.ds__swatch.is-active {
  border-color: var(--color-text);
}

/* A busy, colourful stage so the blur and refraction are obvious. */
.ds__stage {
  position: relative;
  isolation: isolate;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
  gap: var(--space-4);
  padding: var(--space-6);
  border-radius: var(--radius-xl);
  overflow: hidden;
}

.ds__stage-art {
  position: absolute;
  inset: 0;
  z-index: -1;
  background:
    repeating-linear-gradient(60deg, transparent 0 18px, rgb(255 255 255 / 0.35) 18px 22px),
    conic-gradient(
      from 30deg,
      oklch(0.72 0.18 20),
      oklch(0.8 0.16 90),
      oklch(0.72 0.14 170),
      oklch(0.62 0.2 270),
      oklch(0.72 0.18 20)
    );
}

.ds__level {
  display: grid;
  gap: var(--space-1);
  padding: var(--space-6) var(--space-5);
}

.ds__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(9rem, 1fr));
  gap: var(--space-3);
}

.ds__tile {
  display: grid;
  place-items: center;
  height: 6rem;
  font-weight: 650;
}

.ds__cards {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr));
  gap: var(--space-4);
}

.ds__icon {
  display: grid;
  justify-items: center;
  gap: var(--space-1);
  width: 6.5rem;
  padding: var(--space-3);
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}
</style>
