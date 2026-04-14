# Feature Spec: Town NPC Voice Streaming - TOWN-002

## Summary
`Town.unity` now streams NPC reply text directly from OpenAI, but the slice is
still silent. This feature adds real-time voice playback driven by the same
streamed text path so each Town NPC can speak with a distinct ElevenLabs voice
profile while the line is still arriving.

The implementation must keep secrets out of Unity scene data. Unity should ask
the local story-orchestrator backend for a short-lived ElevenLabs
`tts_websocket` token, then connect directly to ElevenLabs' Text-to-Speech
WebSocket using the NPC's configured voice profile.

## User Story
As a player talking to Town NPCs, I want their voice to begin while their line
is being generated so the conversation feels alive instead of like silent
subtitles.

## Product Goals
- Add distinct voice identity to Old Garrett, Mira, and Young Pip.
- Reuse the existing Town text streaming lifecycle instead of adding a separate
  dialogue path.
- Keep provider secrets in the backend env configuration, not in Unity assets.
- Fail soft: text dialogue still works if voice streaming is unavailable.

## Current Slice Anchors
- `Assets/_Project/Scenes/Town.unity`
- `Assets/_Project/Scripts/MonoBehaviours/LLMConversationController.cs`
- `Assets/_Project/Scripts/MonoBehaviours/DialogueChoiceUI.cs`
- `Assets/_Project/Scripts/MonoBehaviours/OpenAIClient.cs`
- `backend/story-orchestrator/app/main.py`
- `backend/story-orchestrator/app/config.py`

## Acceptance Criteria
- [ ] Town NPC voice playback begins from streamed text before the full reply is
      complete.
- [ ] Each Town NPC resolves to a stable ElevenLabs voice profile with a voice
      ID, model ID, and voice settings tuned for that character.
- [ ] Unity never stores the ElevenLabs API key in scene data or serialized
      MonoBehaviour fields for this feature.
- [ ] Unity requests a time-limited `tts_websocket` token from the local backend
      and uses that token to authenticate the ElevenLabs TTS WebSocket session.
- [ ] Voice playback chunks are derived from the streamed text itself, using a
      deterministic chunking policy that prefers sentence or clause boundaries.
- [ ] Voice streaming failure does not interrupt the text conversation. The line
      still streams and the reply options still appear.
- [ ] When a conversation ends, errors, or switches NPCs, the current voice
      stream is closed and any buffered playback is cleared cleanly.
- [ ] The Town scene contains the runtime voice-streaming component and remains
      playable with `TownInteractionAutoplay`.
- [ ] Backend tests cover token minting success and missing-configuration
      failure.
- [ ] EditMode tests cover voice profile mapping, streamed text chunking, and
      Town scene wiring.

## Edge Cases
- ElevenLabs token endpoint unavailable while OpenAI text streaming still works.
- The backend is not running locally.
- The WebSocket opens but no audio arrives before the text turn completes.
- The streamed text never hits punctuation before completion.
- A new NPC interaction starts while previous audio is still buffered.
- The user exits on `Goodbye` mid-voice line.

## Out of Scope
- Lip sync, facial animation, or gesture synthesis
- Persisted voice settings editors or runtime voice selection UI
- Quest-production hardening beyond this local Town slice
- Multi-speaker dialogue generation in one ElevenLabs request
- Replacing the existing text UI with diegetic audio-only interaction

---

## Technical Plan

### Research Reference
- **Key pattern adopted**: use ElevenLabs' Text-to-Speech WebSocket for partial
  text input, because the official docs position it for text that is being
  streamed or generated in chunks.
- **Security decision**: Unity should not use `xi-api-key` directly. The
  backend will mint a single-use `tts_websocket` token via ElevenLabs'
  token endpoint and Unity will pass that token in the WebSocket query string.
- **Voice selection source**: use the official voices listing endpoint to map
  Town NPCs to available premade voices in the workspace.
- **Sources consulted**:
  - https://elevenlabs.io/docs/api-reference/text-to-speech/v-1-text-to-speech-voice-id-stream-input
  - https://elevenlabs.io/docs/api-reference/tokens/create
  - https://elevenlabs.io/docs/api-reference/voices/search
  - https://elevenlabs.io/docs/changelog/2025/11/21

### Memory Reference
- Follow `Pure C# Core + thin MonoBehaviour wrappers`.
- Keep all chunking and profile resolution in `Core/` with EditMode coverage.
- Do not serialize provider secrets into scenes or components.
- Treat audio failure as additive UX failure, not a reason to break the text
  conversation path.

### Architecture
- **Core/**
  - `TownNpcVoiceProfile`
  - `TownNpcVoiceProfileCatalog`
  - `TownVoiceTextChunker`
- **MonoBehaviours/**
  - `TownNpcVoiceStreamController`
  - `StreamingPcmAudioPlayer`
- **Backend**
  - FastAPI route for ElevenLabs `tts_websocket` single-use token minting

### Runtime Flow
```text
LLMConversationController.OnStreamStarted(npc)
  -> TownNpcVoiceStreamController resolves NPC voice profile
  -> Unity requests single-use token from local backend
  -> Unity opens ElevenLabs TTS WebSocket with single_use_token
  -> OpenAI text deltas feed TownVoiceTextChunker
  -> completed text chunks are sent to ElevenLabs
  -> audio chunks arrive as base64 PCM
  -> StreamingPcmAudioPlayer queues and plays them on an AudioSource
  -> OnNPCResponse flushes trailing text
  -> OnConversationEnded / OnError closes socket and clears playback
```

### Voice Defaults
- `Old Garrett` -> `Bill - Wise, Mature, Balanced`
- `Mira the Baker` -> `Bella - Professional, Bright, Warm`
- `Young Pip` -> `Liam - Energetic, Social Media Creator`
- Model default: `eleven_flash_v2_5`
- Output format default: `pcm_16000`

### Testing Strategy
- **Backend unit tests**
  - token endpoint returns a token when configured
  - token endpoint returns `503` when ElevenLabs is not configured
- **EditMode**
  - voice profile lookup for each Town NPC
  - text chunker emits a chunk on punctuation and flushes trailing text
  - Town scene contains the voice stream controller
- **Manual**
  - run the backend locally
  - open `Town.unity`
  - trigger Garrett, Mira, and Pip
  - verify each speaks with a distinct voice while subtitles stream
  - stop the backend and confirm text still works without voice

### Risks
- Unity-side WebSocket + PCM playback is more complex than plain HTTP TTS.
- This slice is verified in-editor first; Quest-device runtime behavior remains
  an explicit follow-up risk until playtested on hardware.
