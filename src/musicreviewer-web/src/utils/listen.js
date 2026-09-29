import amazonMusicIcon from '@/assets/brands/amazon-music.png'
import pandoraIcon from '@/assets/brands/pandora.svg'
import youtubeMusicIcon from '@/assets/brands/youtube-music.svg'

/**
 * Search links for listening to an album on streaming services. They open each service's
 * search for "artist album", which usually lists the album first. Formats checked September 2026.
 *
 * @param {{ artist: string, title: string }} album
 * @returns {{ service: string, label: string, note: string | null, icon: string, url: string }[]}
 */
export function listenLinks({ artist, title }) {
  // "/" would split a path segment (Pandora, Amazon), so treat it as a word break.
  const q = encodeURIComponent(
    `${artist} ${title}`.replaceAll('/', ' ').replace(/\s+/g, ' ').trim(),
  )

  return [
    {
      service: 'youtube-music',
      label: 'YouTube Music',
      note: null,
      icon: youtubeMusicIcon,
      // YouTube Music rather than YouTube search: it favours official uploads.
      url: `https://music.youtube.com/search?q=${q}`,
    },
    {
      service: 'pandora',
      label: 'Pandora',
      note: 'USA only',
      icon: pandoraIcon,
      url: `https://www.pandora.com/search/${q}/all`,
    },
    {
      service: 'amazon-music',
      label: 'Amazon Music',
      note: 'requires account',
      icon: amazonMusicIcon,
      url: `https://music.amazon.com/search/${q}`,
    },
  ]
}
