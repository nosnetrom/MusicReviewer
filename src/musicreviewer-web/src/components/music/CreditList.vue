<script setup>
/** Personnel grouped by role, e.g. Performer → John Coltrane (tenor saxophone). */
const props = defineProps({
  /** @type {import('vue').PropType<{ role: string, people: { name: string, instruments: string | null }[] }[]>} */
  groups: { type: Array, required: true },
})
</script>

<template>
  <dl class="credits">
    <div v-for="group in props.groups" :key="group.role" class="credits__group">
      <dt class="credits__role">{{ group.role }}</dt>
      <dd v-for="person in group.people" :key="person.name" class="credits__person">
        <span class="credits__name">{{ person.name }}</span>
        <span v-if="person.instruments" class="credits__instruments">{{ person.instruments }}</span>
      </dd>
    </div>
  </dl>
</template>

<style scoped>
.credits {
  display: grid;
  gap: var(--space-5);
  margin: 0;
}

.credits__role {
  margin-bottom: var(--space-2);
  font-size: var(--font-size-xs);
  font-weight: 650;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-text-secondary);
}

.credits__person {
  display: flex;
  flex-wrap: wrap;
  gap: 0 var(--space-2);
  margin: 0 0 var(--space-1);
}

.credits__name {
  font-weight: 550;
}

.credits__instruments {
  color: var(--color-text-secondary);
}
</style>
