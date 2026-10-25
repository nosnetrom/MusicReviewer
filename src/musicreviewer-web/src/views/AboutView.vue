<script setup>
import { RouterLink } from 'vue-router'
import GlassPanel from '@/components/glass/GlassPanel.vue'
</script>

<template>
  <article class="about">
    <header>
      <h1 class="about__title">About MusicReviewer</h1>
      <p class="about__lede">
        MusicReviewer helps you explore well-known musicians and their major albums. This page
        explains where our information comes from, how we decide what to show and in what order, and
        how we handle your data.
      </p>
    </header>

    <nav class="about__toc" aria-label="On this page">
      <a href="#methodology">How we select and display music</a>
      <a href="#cookies">Cookie Policy</a>
      <a href="#disclaimers">Disclaimers</a>
    </nav>

    <GlassPanel as="section" class="about__section" aria-labelledby="methodology">
      <h2 id="methodology">How we select and display music</h2>

      <h3>Where the information comes from</h3>
      <p>
        Everything on MusicReviewer comes from open, community-maintained sources. We don’t write
        reviews, assign ratings, or edit the underlying data.
      </p>
      <ul>
        <li>
          <a href="https://musicbrainz.org/">MusicBrainz</a> provides artists, albums, track lists,
          labels, credits, and the genre tags its contributors vote on.
        </li>
        <li>
          <a href="https://www.wikidata.org/">Wikidata</a> connects each artist and album to its
          Wikipedia article and tells us how many language editions of Wikipedia cover it.
        </li>
        <li>
          <a href="https://www.wikipedia.org/">Wikipedia</a> provides the summaries, taken from the
          opening section of each English-language article.
        </li>
        <li>
          The <a href="https://coverartarchive.org/">Cover Art Archive</a> provides album covers.
        </li>
      </ul>

      <h3>Which artists appear</h3>
      <p>Artists join the catalog in two ways:</p>
      <ul>
        <li>
          <strong>Featured artists.</strong> We keep a hand-picked list of influential artists
          across many genres, and they appear on the home page.
        </li>
        <li>
          <strong>Your searches.</strong> When anyone opens an artist from
          <RouterLink to="/search">Search</RouterLink>, we import that artist from MusicBrainz, so
          the catalog grows with what visitors look for.
        </li>
      </ul>
      <p>
        Search results combine MusicBrainz’s own search, in its relevance order, with artists
        already in our catalog whose names begin with what you typed.
        <RouterLink to="/browse">Browse</RouterLink> includes every artist in the catalog, whichever
        way they joined.
      </p>

      <h3>What counts as an album</h3>
      <p>
        We list each album once, as a work, rather than every pressing or reissue of it. We include
        studio albums, EPs, live albums, and compilations, each in its own tab on the artist’s page.
        Singles, soundtracks, remix albums, demos, and DJ mixes aren’t included.
      </p>
      <p>
        For an album’s track list, label, and credits, we use one official edition. We prefer an
        edition from the year the album first came out, then the one with the earliest exact release
        date, then (where there’s still a tie) editions from the United States, the United Kingdom,
        worldwide, or Europe, in that order.
      </p>

      <h3>How albums are ranked</h3>
      <p>
        Wherever albums are ordered by importance, we use a <strong>notability</strong> score rather
        than popularity charts or user ratings. The score combines:
      </p>
      <ul>
        <li>
          <strong>The kind of release.</strong> Studio albums count most, then live albums, then
          compilations; EPs count less than full-length albums.
        </li>
        <li>
          <strong>Whether the album has its own English Wikipedia article.</strong>
        </li>
        <li>
          <strong>How many language editions of Wikipedia cover it.</strong> This counts on a
          sliding scale, so going from 1 to 10 editions matters more than going from 40 to 50.
        </li>
      </ul>
      <p>
        Notability is a rough, transparent measure of how widely an album is documented. It isn’t a
        judgement of quality, and the score itself isn’t shown.
      </p>

      <h3>Genres</h3>
      <p>
        MusicBrainz contributors tag artists with specific genres and vote on them. We group those
        tags into twelve broad genres (Jazz, Blues, Rock, Pop, Soul &amp; R&amp;B, Funk &amp; Disco,
        Hip-Hop, Country, Folk, Electronic, Reggae &amp; Ska, and Gospel) and give each artist up to
        three, based on the votes. Albums are filed under their artist’s genres.
      </p>

      <h3>What you see on each page</h3>
      <ul>
        <li>
          <strong>Home</strong> takes the most notable studio album by each featured artist and
          shows up to 24 of the most notable of those, in a different random order on each visit.
        </li>
        <li>
          <strong>Browse</strong> shows studio albums, filterable by genre and decade. So that no
          one artist dominates, it takes turns: each artist’s three most notable albums first, then
          each artist’s next three, and so on.
        </li>
        <li>
          <strong>Artist pages</strong> list albums by notability by default, or by release date if
          you choose.
        </li>
        <li>
          <strong>Album pages</strong> show the cover, track list, credits, a Wikipedia summary
          where one exists, and links to look the album up on music services.
        </li>
      </ul>

      <h3>Keeping information current</h3>
      <p>
        When you view an artist whose information is more than 30 days old, we refresh it from the
        sources above in the background. To respect MusicBrainz’s usage limits, we request data from
        it no more than about once per second, so new or refreshed artists can take a little while
        to fill in.
      </p>

      <h3>What we don’t do</h3>
      <ul>
        <li>
          No one pays to be featured or ranked, and we don’t accept payment to change rankings.
        </li>
        <li>
          Links to music services are plain searches for the artist and album, with no affiliate or
          tracking codes.
        </li>
      </ul>
    </GlassPanel>

    <GlassPanel as="section" class="about__section" aria-labelledby="cookies">
      <h2 id="cookies">Cookie Policy</h2>

      <h3>Cookies</h3>
      <p>
        MusicReviewer does not use cookies, including advertising, analytics, or tracking cookies.
        <strong>
          We do not collect, sell, rent, or share information through cookies, and we do not sell or
          share any personal information about our visitors.
        </strong>
      </p>

      <h3>Storage in your browser</h3>
      <p>
        To remember a few choices, the site stores a small amount of information in your browser’s
        own storage. It stays on your device and is never sent to us:
      </p>
      <ul>
        <li>
          your theme (light, dark, or system) and display preferences, kept until you clear them;
        </li>
        <li>
          which example artist the search box last suggested, so the next one is different. This is
          deleted when you close the browser tab.
        </li>
      </ul>
      <p>
        You can clear this at any time through your browser’s settings for site data. The site works
        normally without it; it just won’t remember your theme.
      </p>

      <h3>Information our servers record</h3>
      <p>
        Like most websites, our hosting provider (Microsoft Azure) keeps technical logs of requests
        to the site, such as IP address, browser type, the page requested, and the time. We use them
        only to keep the site running, secure, and performing well, and we keep them for a limited
        time. We also use IP addresses briefly to limit how fast any one visitor can make requests,
        so the site stays available to everyone. We don’t use this information to identify you,
        build profiles, or advertise to you.
      </p>
      <p>We don’t offer accounts and don’t ask for your name, email address, or other details.</p>

      <h3>Other websites</h3>
      <p>
        Album covers load directly from the Cover Art Archive, run by the Internet Archive, which
        can see your IP address when your browser requests an image. Links to MusicBrainz,
        Wikipedia, and music services take you to sites with their own privacy and cookie policies,
        which we don’t control.
      </p>
    </GlassPanel>

    <GlassPanel as="section" class="about__section" aria-labelledby="disclaimers">
      <h2 id="disclaimers">Disclaimers</h2>

      <h3>Accuracy</h3>
      <p>
        Our information comes from community-maintained sources and is provided as is. It may be
        incomplete or contain errors, and we can’t guarantee its accuracy. Corrections are best made
        at the source, on MusicBrainz or Wikipedia, and will reach this site when it next refreshes.
      </p>

      <h3>Copyright and trademarks</h3>
      <p>
        Album cover art is the property of its respective copyright holders and is shown to identify
        the albums discussed. Wikipedia summaries are available under the
        <a href="https://creativecommons.org/licenses/by-sa/4.0/">CC BY-SA 4.0</a> license, and each
        is credited on the page where it appears. Names and logos of music services are trademarks
        of their respective owners.
      </p>

      <h3>No affiliation</h3>
      <p>
        MusicReviewer is an independent site. It isn’t affiliated with or endorsed by MusicBrainz,
        Wikipedia, the Wikimedia Foundation, the Internet Archive, the artists listed, or any music
        service we link to.
      </p>

      <h3>Changes to this page</h3>
      <p>
        We may update this page as the site changes. The date below shows when it was last revised.
      </p>
      <p class="about__updated">Last updated: October 25, 2026</p>
    </GlassPanel>
  </article>
</template>

<style scoped>
.about {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: var(--space-5);
  max-width: 48rem;
}

.about__title {
  margin: 0 0 var(--space-3);
  font-size: var(--font-size-xl);
}

.about__lede {
  margin: 0;
  color: var(--color-text-secondary);
}

.about__toc {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2) var(--space-5);
  font-size: var(--font-size-sm);
  font-weight: 600;
}

.about__section {
  padding: var(--space-5) var(--space-6);
}

/* Leave room above section headings jumped to from the contents, below the floating nav. */
.about__section h2 {
  scroll-margin-top: calc(3.5rem + var(--space-6));
}

.about__section h3 {
  margin-top: var(--space-5);
  font-size: var(--font-size-md);
}

.about__section ul {
  margin: 0 0 var(--space-4);
  padding-left: var(--space-5);
}

.about__section li + li {
  margin-top: var(--space-2);
}

.about__section > :last-child {
  margin-bottom: 0;
}

.about__updated {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

@media (max-width: 640px) {
  .about__section {
    padding: var(--space-4);
  }

  /* The nav is a bottom bar on phones, so headings only need a little room. */
  .about__section h2 {
    scroll-margin-top: var(--space-5);
  }
}
</style>
