import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/search',
      name: 'search',
      component: () => import('../views/SearchView.vue'),
      meta: { title: 'Search' },
    },
    {
      path: '/browse',
      name: 'browse',
      component: () => import('../views/BrowseView.vue'),
      meta: { title: 'Browse' },
    },
    {
      path: '/artists/:id',
      name: 'artist',
      component: () => import('../views/ArtistView.vue'),
      props: true,
      meta: { title: 'Artist' },
    },
    {
      path: '/recordings/:id',
      name: 'recording',
      component: () => import('../views/RecordingView.vue'),
      props: true,
      meta: { title: 'Recording' },
    },
    {
      path: '/design',
      name: 'design',
      component: () => import('../views/DesignSystemView.vue'),
      meta: { title: 'Design system' },
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('../views/NotFoundView.vue'),
      meta: { title: 'Not found' },
    },
  ],
  scrollBehavior: (to, from, saved) => saved ?? { top: 0 },
})

router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} · MusicReviewer` : 'MusicReviewer'
})

export default router
