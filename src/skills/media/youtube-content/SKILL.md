# youtube-content

> Fetch YouTube video transcripts and transform them into structured content — chapters, summaries, threads, blog posts.

<!-- created: 2026-01-01T00:00:00Z -->
<!-- updated: 2026-01-01T00:00:00Z -->
<!-- usage_count: 0 -->
<!-- tags: media, youtube, content, transcription -->

## Setup

```bash
pip install youtube-transcript-api yt-dlp
```

## Get Transcript

```python
from youtube_transcript_api import YouTubeTranscriptApi

video_id = "dQw4w9WgXcQ"  # from youtube.com/watch?v=<id>
transcript = YouTubeTranscriptApi.get_transcript(video_id, languages=['en', 'id'])
full_text = " ".join([t['text'] for t in transcript])
print(full_text[:2000])
```

## Get Video Info

```bash
yt-dlp --dump-json --no-download "https://youtube.com/watch?v=<id>" | python3 -c "
import json, sys
d = json.load(sys.stdin)
print(f'Title: {d[\"title\"]}')
print(f'Channel: {d[\"uploader\"]}')
print(f'Duration: {d[\"duration\"]}s')
print(f'Description: {d[\"description\"][:500]}')
"
```

## Transform to Blog Post

Once you have the transcript, ask the agent:
> "Here's the transcript of a YouTube video. Write a blog post with an introduction, 3-5 main sections with headers, key takeaways, and a conclusion."

## Transform to Twitter/X Thread

> "Convert this transcript into a 10-tweet thread. Each tweet max 280 chars. Start with a hook tweet."

## Extract Chapters

```python
# If the video has chapter markers in description
import re
desc = "..."  # video description
chapters = re.findall(r'(\d+:\d+(?::\d+)?)\s+(.+)', desc)
for timestamp, title in chapters:
    print(f"{timestamp}: {title}")
```
