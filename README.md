# Continue Watching

## A Jellyfin Plugin

Continue Watching replaces Jellyfin's built-in `Continue Watching` home screen section with one simple place to pick up where you left off.

If you stop a movie halfway through, the movie stays there. If you are watching a show, the show stays there as one item, pointing at the episode you should continue with.

Why this helps:

- Your home screen shows one entry for a show, not a clutter of episodes.
- Finish an episode and the show stays ready with the next episode.
- You do not need to mark episodes watched or unwatched just to make your home screen look right.
- Movies leave the section when you finish them. Shows leave when there is nothing left to continue.

## Installation

### Prerequisites

- Jellyfin `10.11.11`

### Installation

1. Add this plugin repository to Jellyfin:

   ```text
   https://jellyfin.vuegen.dev/plugins/manifest.json
   ```

2. Install `Continue Watching` from the plugin catalogue.
3. Restart Jellyfin.
4. If you use Home Screen Sections, use the new `Continue Watching` section at the bottom instead of the built-in `Continue Watching` up top.
5. (Optional) Remove the Next Up section from your clients. You no longer need it.

> [!IMPORTANT]
> The Continue Watching section will only reappear once you start watching a movie or series

---

This project is licensed under the GPL-3.0 License. See the [LICENSE](LICENSE) file for details.

AI was used to aid the development of the project.
