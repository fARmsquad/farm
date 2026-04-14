# Feature Spec: Town Optional Voice Input And Exit Gate - TOWN-005

## Summary
`Town.unity` currently forces the player to continue through the four generated
reply buttons after each NPC line. That keeps the flow deterministic, but it
also makes the conversation feel constrained and leaves no single place where
"goodbye" rules are enforced if a new freeform input surface is added.

This feature adds an optional push-to-talk voice input path that transcribes the
player's microphone into a normal Town player prompt. The transcript must flow
through the same request, memory, and option pipeline as button picks, while a
shared exit gate decides when spoken or future freeform "goodbye" prompts are
allowed to end the conversation.

## User Story
As a player talking to Town NPCs, I want to optionally speak my own reply
instead of only picking one of the four buttons, while still keeping the
conversation from ending too early.

## Product Goals
- Keep the existing four-option Town flow as the safe fallback.
- Add one optional freeform input surface instead of branching the conversation
  logic into a second code path.
- Make spoken prompts reuse the same conversation memory, UI summary, and
  streamed reply lifecycle as button prompts.
- Prevent early spoken "goodbye" attempts from bypassing the staged Town
  conversation cadence.

## Current Slice Anchors
- `Assets/_Project/Scenes/Town.unity`
- `Assets/_Project/Scripts/MonoBehaviours/LLMConversationController.cs`
- `Assets/_Project/Scripts/MonoBehaviours/DialogueChoiceUI.cs`
- `Assets/_Project/Scripts/MonoBehaviours/OpenAIClient.cs`
- `Assets/_Project/Scripts/Core/TownDialogueOptionComposer.cs`
- `Assets/Tests/EditMode/TownConversationFlowTests.cs`
- `Assets/Tests/EditMode/DialogueChoiceUITests.cs`

## Acceptance Criteria
- [ ] While a Town conversation is active, the player can hold the voice-input
      key to record a short microphone clip and release it to submit.
- [ ] Releasing the voice-input key uploads the recorded clip to OpenAI's
      Transcriptions API as `wav` and receives plain-text transcript output.
- [ ] A successful transcript is submitted through the same Town prompt path as
      button choices, so memory, streamed responses, and reply-aware options all
      stay in sync.
- [ ] The dialogue UI shows the last player prompt for both button picks and
      transcribed voice prompts.
- [ ] The existing four reply buttons remain available as fallback when the
      microphone is unavailable, transcription fails, or the player simply
      prefers not to speak.
- [ ] A shared exit gate blocks early spoken or freeform goodbye-style prompts
      until the same unlock turn used by the Town option composer.
- [ ] After the unlock turn, a spoken goodbye prompt ends the conversation
      cleanly through the existing end-conversation flow.
- [ ] Voice-input failure never breaks the text conversation; the player can
      continue using the preset choices immediately.
- [ ] OpenAI credentials for transcription remain environment-first and are not
      serialized into `Town.unity`.
- [ ] EditMode coverage locks the exit gate, WAV encoding, UI prompt display,
      and runtime voice-input auto-wiring.

## Edge Cases
- No microphone device is available.
- The recorded clip is empty because the player taps too quickly.
- The transcript comes back empty or whitespace only.
- The transcript is an early "goodbye" attempt before the exit gate unlocks.
- A voice submission is in flight while the player tries to record again.
- The player ends the conversation while recording or transcribing.

## Out Of Scope
- Continuous always-listening voice input
- Realtime speech-to-text sessions with server-side turn detection
- Typed freeform text-entry UI
- Quest-specific microphone permission packaging and store-submission hardening
- Voice activity visualization, waveform rendering, or spoken echo playback

---

## Technical Plan

### Research Reference
- **OpenAI speech-to-text approach**: use the Audio Transcriptions API with a
  completed `wav` recording and `gpt-4o-mini-transcribe`, because the official
  docs explicitly support `wav`, plain-text responses, and completed-recording
  streaming or non-streaming flows.
- **Unity microphone capture approach**: use `Microphone.Start`,
  `Microphone.GetPosition`, and `AudioClip.GetData` to capture the recorded
  frames and convert them into a PCM WAV payload for upload.
- **Sources consulted**:
  - https://developers.openai.com/api/docs/guides/speech-to-text
  - https://docs.unity3d.com/ScriptReference/Microphone.GetPosition.html
  - https://docs.unity3d.com/ScriptReference/AudioClip.GetData.html

### Memory Reference
- Follow `Pure C# Core + thin MonoBehaviour wrappers`.
- Reuse the existing Town dialogue rules instead of creating a second response
  path for voice.
- Apply the completion-learning guardrails that Town dialogue behavior must be
  verified separately for streamed text, option cadence, and request-shape
  correctness.

### Architecture
- **Core/**
  - `TownConversationExitGate`
  - `TownPcm16WavEncoder`
- **MonoBehaviours/**
  - `OpenAIConfigurationResolver`
  - `OpenAITranscriptionClient`
  - `TownVoiceInputController`
- **Updated existing runtime**
  - `LLMConversationController` gets one shared player-prompt submission path.
  - `DialogueChoiceUI` listens for submitted prompts and voice-input status.
  - `TownDialogueOptionComposer` reuses the shared exit gate constants instead
    of maintaining its own goodbye rule copy.

### Runtime Flow
```text
Town conversation is active
  -> player either clicks a reply button or holds the voice-input key
  -> TownVoiceInputController records microphone frames
  -> release stops recording and encodes frames into PCM WAV
  -> OpenAITranscriptionClient uploads the WAV to /v1/audio/transcriptions
  -> transcript text returns
  -> LLMConversationController.SubmitPlayerPrompt(transcript)
  -> TownConversationExitGate decides:
       - blocked early exit -> stay in conversation and surface feedback
       - allowed exit -> EndConversation()
       - normal prompt -> add user message and stream NPC reply
  -> DialogueChoiceUI shows "You: <prompt>" above the next NPC reply
```

### Testing Strategy
- **EditMode**
  - exit gate blocks goodbye before the unlock turn and allows it later
  - WAV encoder writes the expected RIFF/WAVE header and PCM payload length
  - dialogue UI reflects submitted player prompts independent of button clicks
  - `LLMConversationController` adds the optional voice-input component when the
    feature is enabled
- **Manual**
  - open `Town.unity`
  - start a conversation with Garrett, Mira, and Pip
  - confirm the four option buttons still work
  - hold the voice-input key, speak a short reply, release, and confirm the
    transcript is submitted as the player prompt
  - try saying goodbye on the first turn and confirm the conversation stays open
  - try again after the unlock turn and confirm the conversation closes cleanly

### Risks
- Microphone availability and permissions differ across Editor, desktop, and
  Quest builds, so this slice must be handed off as editor-verified first.
- OpenAI transcription latency may feel slower than button clicks; the feature
  must remain optional and non-blocking.
- If the transcript mishears a goodbye phrase, the shared exit gate becomes the
  final guardrail, so its behavior needs focused regression coverage.
