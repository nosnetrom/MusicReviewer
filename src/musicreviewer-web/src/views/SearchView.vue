<script setup>
import { onMounted, ref, useTemplateRef, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import EmptyState from '@/components/EmptyState.vue'
import GlassSearchField from '@/components/glass/GlassSearchField.vue'

const route = useRoute()
const router = useRouter()

const queryFromRoute = () => (typeof route.query.q === 'string' ? route.query.q : '')

const query = ref(queryFromRoute())
const field = useTemplateRef('field')

watch(
  () => route.query.q,
  () => (query.value = queryFromRoute()),
)

onMounted(() => {
  if (!query.value) field.value?.focus()
})

const search = (q) => router.replace({ query: { q } })
</script>

<template>
  <div class="search">
    <h1 class="search__title">Search</h1>
    <GlassSearchField
      ref="field"
      v-model="query"
      size="lg"
      label="Search artists and albums"
      @submit="search"
    />

    <EmptyState v-if="!route.query.q" title="Find an artist" icon="search">
      <p>Search by artist or band name to see their major recordings.</p>
    </EmptyState>
    <EmptyState v-else :title="`Results for “${route.query.q}”`" icon="disc">
      <p>Catalog search is coming soon.</p>
    </EmptyState>
  </div>
</template>

<style scoped>
.search {
  display: grid;
  gap: var(--space-6);
  max-width: 48rem;
}

.search__title {
  margin: 0;
  font-size: var(--font-size-xl);
}
</style>
