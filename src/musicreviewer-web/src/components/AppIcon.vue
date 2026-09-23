<script>
/** Minimal stroke icon set on a 24×24 grid. Each entry is a list of path `d` strings. */
const paths = {
  search: ['M10.5 18a7.5 7.5 0 1 0 0-15 7.5 7.5 0 0 0 0 15Z', 'm16 16 5 5'],
  close: ['M6 6l12 12', 'M18 6 6 18'],
  home: ['M4 10.5 12 4l8 6.5V20a1 1 0 0 1-1 1h-4.5v-6h-5v6H5a1 1 0 0 1-1-1Z'],
  browse: ['M4 4h7v7H4Z', 'M13 4h7v7h-7Z', 'M4 13h7v7H4Z', 'M13 13h7v7h-7Z'],
  sun: [
    'M12 16a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z',
    'M12 2v2',
    'M12 20v2',
    'm4.9 4.9 1.4 1.4',
    'm17.7 17.7 1.4 1.4',
    'M2 12h2',
    'M20 12h2',
    'm4.9 19.1 1.4-1.4',
    'm17.7 6.3 1.4-1.4',
  ],
  moon: ['M20 14.5A8 8 0 1 1 9.5 4a6.5 6.5 0 0 0 10.5 10.5Z'],
  system: ['M3 5h18v11H3Z', 'M8 20h8', 'M12 16v4'],
  chevronRight: ['m9 5 7 7-7 7'],
  disc: ['M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18Z', 'M12 14.5a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5Z'],
  sparkle: [
    'M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8Z',
    'M19 17l.7 2 2 .7-2 .7-.7 2-.7-2-2-.7 2-.7Z',
  ],
}

export const iconNames = Object.keys(paths)
</script>

<script setup>
/** Decorative by default; pass `label` when the icon is the only content of a control. */
const props = defineProps({
  name: {
    type: String,
    required: true,
    validator: (v) => Object.hasOwn(paths, v),
  },
  size: { type: [Number, String], default: 20 },
  label: { type: String, default: '' },
})
</script>

<template>
  <svg
    :width="props.size"
    :height="props.size"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="1.8"
    stroke-linecap="round"
    stroke-linejoin="round"
    :role="props.label ? 'img' : undefined"
    :aria-label="props.label || undefined"
    :aria-hidden="props.label ? undefined : 'true'"
    focusable="false"
  >
    <path v-for="d in paths[props.name]" :key="d" :d="d" />
  </svg>
</template>
