<script setup>
import { computed, ref, useId } from 'vue'

/** A short first paragraph is often just a one-line definition, so show the next one too. */
const SHORT_PARAGRAPH = 400

const props = defineProps({
  /** A WikipediaSummaryDto: { title, url, revisionId, fetchedUtc, paragraphs }. */
  summary: { type: Object, required: true },
})

const expanded = ref(false)
const moreId = useId()

const leadCount = computed(() => {
  const [first = ''] = props.summary.paragraphs
  return first.length < SHORT_PARAGRAPH ? 2 : 1
})
const lead = computed(() => props.summary.paragraphs.slice(0, leadCount.value))
const more = computed(() => props.summary.paragraphs.slice(leadCount.value))
</script>

<template>
  <div class="wiki-summary">
    <div class="wiki-summary__text">
      <p v-for="(paragraph, i) in lead" :key="`lead-${i}`">{{ paragraph }}</p>
      <div v-if="more.length" v-show="expanded" :id="moreId">
        <p v-for="(paragraph, i) in more" :key="`more-${i}`">{{ paragraph }}</p>
      </div>
    </div>

    <button
      v-if="more.length"
      type="button"
      class="wiki-summary__toggle"
      :aria-expanded="expanded"
      :aria-controls="moreId"
      @click="expanded = !expanded"
    >
      {{ expanded ? 'Show less' : 'Read more' }}
    </button>

    <p class="wiki-summary__attribution">
      Excerpt from Wikipedia’s article
      <a :href="summary.url" target="_blank" rel="noopener">“{{ summary.title }}”</a>, available
      under
      <a
        href="https://creativecommons.org/licenses/by-sa/4.0/"
        target="_blank"
        rel="noopener license"
        >CC BY-SA 4.0</a
      >.
    </p>
  </div>
</template>

<style scoped>
.wiki-summary {
  display: grid;
  gap: var(--space-3);
}

.wiki-summary__text {
  display: grid;
  gap: var(--space-3);
  max-width: 70ch;
  line-height: 1.6;
}

.wiki-summary__text p,
.wiki-summary__text div {
  margin: 0;
}

.wiki-summary__text div {
  display: grid;
  gap: var(--space-3);
}

.wiki-summary__toggle {
  justify-self: start;
  padding: 0;
  border: 0;
  background: none;
  font: inherit;
  font-size: var(--font-size-sm);
  font-weight: 600;
  color: var(--color-accent);
  cursor: pointer;
}

.wiki-summary__toggle:hover {
  text-decoration: underline;
}

.wiki-summary__attribution {
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-tertiary);
}

.wiki-summary__attribution a {
  color: inherit;
  text-decoration: underline;
}
</style>
