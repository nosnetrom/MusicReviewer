<script setup>
import EmptyState from '@/components/EmptyState.vue'
import GlassButton from '@/components/glass/GlassButton.vue'
</script>

<template>
  <EmptyState title="Page not found" icon="disc">
    <p>That page skipped a groove. Let's get you back on track.</p>
    <GlassButton variant="primary" to="/">Back to home</GlassButton>
  </EmptyState>
</template>
