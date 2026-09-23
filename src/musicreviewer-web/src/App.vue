<script setup>
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <header class="app-header">
    <RouterLink to="/" class="brand">MusicReviewer</RouterLink>
  </header>

  <main class="app-main">
    <RouterView />
  </main>
</template>

<style scoped>
.app-header {
  padding: 1rem 1.5rem;
}

.brand {
  font-weight: 700;
  font-size: 1.25rem;
  color: inherit;
  text-decoration: none;
}

.app-main {
  padding: 0 1.5rem 2rem;
  max-width: 72rem;
  margin: 0 auto;
}
</style>
