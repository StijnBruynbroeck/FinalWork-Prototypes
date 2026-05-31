# Neuro-Filter AI

A first-person stealth game developed in Unity 6 where the player uses voice commands to morph into furniture objects, avoid an enemy AI, hack terminals, and solve puzzles. All AI processing runs locally using Ollama (LLM) and Whisper.cpp (speech-to-text), ensuring privacy and low latency.

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation and Setup](#installation-and-setup)
  - [1. Install Ollama (LLM Server)](#1-install-ollama-llm-server)
  - [2. Install Whisper.cpp (Speech-to-Text Server)](#2-install-whispercpp-speech-to-text-server)
  - [3. Start the Servers](#3-start-the-servers)
  - [4. Verify Servers Are Running](#4-verify-servers-are-running)
  - [5. Open in Unity](#5-open-in-unity)
- [Controls](#controls)
- [Gameplay](#gameplay)
  - [Voice Morphing](#voice-morphing)
  - [Enemy AI](#enemy-ai)
  - [Door System](#door-system)
  - [Terminal Hacking](#terminal-hacking)
  - [Riddle Puzzles](#riddle-puzzles)
- [Architecture](#architecture)
  - [Voice Command Pipeline](#voice-command-pipeline)
  - [Routing System](#routing-system)
- [Project Structure](#project-structure)
  - [Scripts Overview](#scripts-overview)
  - [Scenes](#scenes)
  - [Furniture Prefabs](#furniture-prefabs)
- [Technical Stack](#technical-stack)
  - [Unity Packages](#unity-packages)
  - [External Services](#external-services)
  - [AI Models](#ai-models)
- [Design Patterns](#design-patterns)
- [Performance and Analytics](#performance-and-analytics)
  - [LLM Performance Dashboard](#llm-performance-dashboard)
  - [Analytics Logging](#analytics-logging)
  - [Automated Testing](#automated-testing)
- [References and Credits](#references-and-credits)
- [Known Issues and Missing Features](#known-issues-and-missing-features)

## Features

- **Voice-Controlled Furniture Morphing** -- Press and hold T to speak, release to morph into one of 10 furniture types. An LLM evaluates how well the chosen object fits the current zone context and scores the disguise on a 0-100 scale.
- **Local AI Processing (Privacy-First)** -- All speech recognition runs through Whisper.cpp and all language model inference runs through Ollama on your own machine. No cloud services required. No data leaves your computer.
- **Enemy AI with Vision and Suspicion System** -- A NavMesh-based enemy patrols the level with a vision cone, responds to audio events (hears you speaking), and inspects morphed players from a distance. Suspicion levels are modulated by the LLM disguise score -- a well-chosen object keeps suspicion low.
- **Terminal Hacking Minigame** -- A Fallout-inspired terminal hacking system with a multi-step voice authentication process (3 stages), a word-matching minigame using Levenshtein distance, and dud removers (bracket-based voice commands).
- **Voice-Controlled Door System** -- Proximity-triggered door with voice code entry (12123). Supports 3 animation methods: legacy Animation, Mecanim Animator, and direct Transform rotation.
- **LLM-Driven Riddle Puzzles** -- Multi-step puzzles where the LLM judges your spoken answers. Four stages per puzzle with dynamic feedback and hints.
- **Context-Aware Zone System** -- The game world is divided into zones. The LLM considers which zone you are in when scoring your morph request, giving higher scores to furniture that belongs in that room.
- **Analytics and Performance Dashboard** -- Every voice interaction is logged to a CSV file (LLM_Performance_Log.csv). A standalone HTML dashboard visualizes test results with scatter plots and histograms.
- **Automated Batch Testing** -- Press F12 in the editor to run 50 predefined test phrases (20 logical, 20 illogical, 10 edge cases) through the LLM pipeline and log results automatically.
- **Cyberpunk Terminal UI** -- Monospace typewriter effects, boot sequences, animated cursor, color themes (green system text, red errors, green access granted), and screen shake effects.

## Requirements

### Software

| Component | Version | Purpose |
|-----------|---------|---------|
| Unity Editor | 6000.3.3f1 (Unity 6) | Game engine and development environment |
| Ollama | Latest | Local LLM server for semantic evaluation |
| Whisper.cpp | Latest (with HTTP server) | Local speech-to-text server |
| Windows | 10 or 11 | Target platform |

### Hardware (Minimum)

| Component | Requirement |
|-----------|-------------|
| CPU | 4+ cores (x86_64) |
| RAM | 8 GB (16 GB recommended for running both AI servers) |
| GPU | DirectX 12 compatible (for URP rendering) |
| Microphone | Any Windows-compatible microphone |
| Disk | ~2 GB for Ollama models + ~1 GB for Unity project |

### Required Models

| Model | Size | Provider | Used For |
|-------|------|---------|----------|
| llama3:latest | ~4.7 GB | Ollama/Meta | Primary LLM for morph evaluation and puzzle judging |
| phi3:mini | ~2.2 GB | Ollama/Microsoft | Alternative LLM, used in fallback configuration |
| ggml-base.bin | ~142 MB | Whisper.cpp/OpenAI | Speech-to-text transcription |

## Installation and Setup

The project requires two local servers running simultaneously: one for the LLM (Ollama) and one for speech-to-text (Whisper.cpp). Both must be started before launching the game in Unity.

### 1. Install Ollama (LLM Server)

1. Download Ollama from [https://ollama.ai](https://ollama.ai)
2. Install the application (adds Ollama to PATH automatically)
3. Open a terminal and pull the required model:

   ```powershell
   ollama pull llama3
   ```

4. (Optional) Pull the alternative model:

   ```powershell
   ollama pull phi3
   ```

### 2. Install Whisper.cpp (Speech-to-Text Server)

1. Download or build the Whisper.cpp HTTP server from [https://github.com/ggml-org/whisper.cpp](https://github.com/ggml-org/whisper.cpp)
2. The pre-built release should be placed at `C:\whisper\Release\` (or any location of your choice)
3. Download the `ggml-base.bin` model file and place it in the same directory as `whisper-server.exe`
4. Verify the following files exist:
   - `C:\whisper\Release\whisper-server.exe`
   - `C:\whisper\Release\ggml-base.bin`

### 3. Start the Servers

Open two separate terminal windows:

**Terminal 1 -- Ollama:**

```powershell
ollama serve
```

Ollama will start on `http://localhost:11434`. The first request may take longer as the model loads into memory.

**Terminal 2 -- Whisper.cpp:**

```powershell
C:\whisper\Release\whisper-server.exe --port 9090 --model C:\whisper\Release\ggml-base.bin
```

Whisper.cpp will start on `http://localhost:9090/inference`. The model file path must point to the location of `ggml-base.bin`.

### 4. Verify Servers Are Running

Check that both ports are listening:

```powershell
netstat -an | Select-String "LISTEN" | Select-String "11434|9090"
```

Both ports should show `LISTENING`.

**Test Ollama:**

```powershell
curl -X POST http://localhost:11434/v1/chat/completions `
  -H "Content-Type: application/json" `
  -d "{\"model\":\"llama3\",\"messages\":[{\"role\":\"user\",\"content\":\"hello\"}]}"
```

**Test Whisper.cpp:**

```powershell
curl -X POST "http://localhost:9090/inference" `
  -F "file=@test.wav" `
  -F "response_format=json" `
  -F "temperature=0.0" `
  -F "language=en"
```

### 5. Open in Unity

1. Open Unity Hub
2. Click "Open" and browse to the `ApiTest` directory
3. Wait for the project to load and compile all scripts
4. Open `Assets/Scenes/TestScene.unity`
5. Press Play in the Unity Editor
6. Hold T to speak into your microphone, release to process

> **Note:** The `TestScene` is the main game scene. `SampleScene` is a legacy debug scene. Currently only `SampleScene` is added to the build settings -- ensure `TestScene` is added before building a standalone executable.

## Controls

| Key | Action |
|-----|--------|
| **T** (hold) | Record voice command |
| **T** (release) | Process voice command (transcription + LLM) |
| **R** | Unmorph (revert to human form) |
| **W/A/S/D** | Move (first and third person) |
| **Mouse** | Look around |
| **P** | Toggle between first-person and third-person camera |
| **H** | Toggle help panel overlay (commands reference) |
| **F12** | Run automated batch test (50 phrases through LLM only, skips audio) |

## Gameplay

### Voice Morphing

The core mechanic. Approach any area, hold T, and speak a command like "Turn me into a chair" or "I want to be a table." Release T. The system will:

1. Record your voice (16 kHz, max 30 seconds)
2. Send the audio to Whisper.cpp for transcription
3. Route the transcribed text through the command system
4. If no direct command matches, send the request to Ollama with zone context
5. The LLM evaluates plausibility (0-100) based on zone fit
6. If the score is high enough and the prefab exists, you morph into the furniture
7. The enemy AI receives the LLM score as a disguise multiplier

Available furniture for morphing (10 types):

| Prefab | Voice Keywords |
|--------|---------------|
| Chair01 | chair, seat, stool |
| OfficeChair | office chair |
| Sofa01 | sofa, couch |
| Table01 | table, desk |
| Bed01 | bed, cot |
| Closet01 | closet, cabinet, locker, wardrobe |
| BathTub01 | bath, tub, bathtub |
| Cushion01 | cushion, pillow |
| Drawer01 | drawer, chest |
| Bench | bench |

### Enemy AI

The enemy patrols the level using Unity NavMesh and has three states:

- **Roam** -- Default state. The enemy walks between waypoints within a configurable radius. Has a bias toward investigating the player's last known position.
- **Investigate** -- Triggered by audio events (the system emits sound when you start/stop speaking). The enemy moves to the sound source and waits briefly.
- **Chase** -- Triggered when suspicion reaches the detection threshold (100). The enemy pursues the player.

**Suspicion System:**

Suspicion is determined by several factors combined:

| Factor | Effect |
|--------|--------|
| Player in vision cone | Adds suspicion over time (suspicionRate = 10/s) |
| LLM disguise score (>= 80) | Reduces suspicion via multiplier (well-disguised) |
| LLM disguise score (< 80) | Increases suspicion (poor disguise) |
| Player outside current zone | Out-of-zone suspicion bonus added per tick |
| Player not in sight | Suspicion decays naturally (suspicionDecay = 15/s) |

**Suspicion Thresholds:**

| Level | Value | Behavior |
|-------|-------|----------|
| Stop | 30 | Enemy slows movement and becomes alert |
| Alert | 60 | Enemy actively searches and moves faster |
| Detection | 100 | Enemy transitions to Chase state |

The suspicion level is displayed as a color-coded UI bar (green/yellow/red).

### Door System

The door near the starting area is locked and requires:

1. **Proximity** -- Walk close to the door (trigger zone)
2. **Voice Code** -- Say "one two one two three" (the code is 12123)
3. The door unlocks and swings open using a smooth RotateAround animation

The door also responds to "open the door" and "close the door" voice commands through both direct keyword routing and LLM evaluation.

### Terminal Hacking

The terminal hacking system requires three voice-authenticated steps:

| Step | Voice Command |
|------|---------------|
| 1 | "Initiate connection" |
| 2 | "Authentication code alpha seven" |
| 3 | "Override security" |

Each step uses Levenshtein distance (fuzzy matching with 35% tolerance) so variations are accepted.

After authentication, a word-matching minigame begins:

- 12 words are displayed from a pool of 15 (ACCESS, OVERRIDE, SECURE, LOCKED, GUARD, BREACH, TARGET, ENSIGN, BYPASS, ROBOT, THREAD, PATHOS, SECTOR, ASSIGN, LEGACY)
- 4 attempts allowed
- After a furniture counting puzzle is completed, the terminal grants access

**Dud Remover:** Words enclosed in brackets `[]`, `()`, `{}`, `<>` can be spoken to remove fake entries from the word list.

### Riddle Puzzles

The voice riddle puzzle has 4 stages evaluated by the LLM:

| Stage | Description | Expected Concepts |
|-------|-------------|-------------------|
| 1 | SCAN THE ROOM - Describe what you see | server room, computers, cabinets |
| 2 | IDENTIFY - Which object has a blue light? | server cabinet, blue LED, indicator |
| 3 | IMITATE - Make the sound of a server | humming, buzzing, electronic |
| 4 | CODE - Give the authorization code | 4821, authorization, code |

The LLM judges each answer and provides Dutch-language feedback and hints.

## Architecture

### Voice Command Pipeline

```
Player holds [T]
    -> MicrophoneRecorder.StartRecording()
Player releases [T]
    -> MicrophoneRecorder.StopRecording()
        -> Convert to 16-bit PCM WAV (hand-written RIFF WAV generator)
    -> GroqAudioService.TranscribeAudio()
        -> POST http://localhost:9090/inference (Whisper.cpp)
    -> VoiceAppController.OnTranscriptionSuccess()
        -> Step 0: Noise filtering / rejection of [BLANK_AUDIO], [INAUDIBLE], etc.
        -> Step 0a: Whisper mishearing corrections (bye+bass -> bypass, on morph -> unmorph)
        -> Step 0b: Pre-LLM unmorph check (always runs)
        -> Step 1: Door command routing (RouteerNaarDeur)
        -> Step 1a: Door code routing (RouteerNaarCode)
        -> Step 2: Terminal routing (RouteerNaarTerminal)
        -> Step 3: Puzzle routing (VoiceRiddlePuzzle)
        -> Step 4: Zone context determination
        -> Step 5: LLM evaluation (GroqLLMService.EvaluatePlausibility)
            -> POST http://localhost:11434/v1/chat/completions (Ollama)
            -> Returns JSON: {score, reason, prefab_name, door_action, unmorph}
        -> ObjectSpawner.ProcessTextAndSpawn() (morph player into prefab)
        -> EnemyVision.SetLLMScoreMultiplier() (adjust enemy suspicion)
        -> AnalyticsManager.LogData() (write to CSV)
```

### Routing System

The command routing uses a Chain of Responsibility pattern: each handler checks whether the spoken text matches its domain and either processes it or passes it to the next handler.

**Order of routing:**

1. Pre-LLM unmorph check (keyword-based, runs even if not morphed)
2. Direct door commands (keyword-based: "open door", "close door")
3. Door code entry (digit/number-word detection)
4. Terminal hacking (fuzzy matching against active terminal state)
5. Riddle puzzle (if player is in range of an active puzzle)
6. LLM evaluation (Ollama processes the request with zone context)
7. Keyword fallback (if LLM returns empty, basic keyword matching runs)

## Project Structure

### Scripts Overview

All scripts are located in `Assets/Scripts/`. Total: approximately 5,400 lines across 32 C# files.

| File | Lines | Role |
|------|-------|------|
| `VoiceAppController.cs` | 493 | Main controller: routes voice input to subsystems, manages the full pipeline |
| `Terminal Hacker.cs` | 602 | Multi-step terminal state machine with voice authentication and hack integration |
| `EnemyAI.cs` | 587 | Enemy behavior: NavMesh patrol, chase, investigate, morph inspection, suspicion |
| `HackMinigame.cs` | 325 | Word-matching minigame: Levenshtein distance, Fisher-Yates shuffle, dud removers |
| `GroqLLMService.cs` | 251 | Ollama LLM client: sends requests to local endpoint, parses JSON responses, offline fallback |
| `DoorCode.cs` | 208 | Voice-controlled code entry: extracts digits from speech, validates against code 12123 |
| `TerminalDisplay.cs` | 200 | Boot sequence animation, cursor animation, status bar |
| `FurnitureCounter.cs` | 188 | Furniture counting sub-puzzle: counts objects in scene, validates spoken answer |
| `VoiceRiddlePuzzle.cs` | 184 | Multi-step LLM-driven riddle puzzle: sends player answers to Ollama for evaluation |
| `TerminalUI.cs` | 166 | Typewriter text effect, color themes, pulse/shake animations |
| `ObjectSpawner.cs` | 155 | Player morphing: instantiates prefabs, snaps to ground, toggles player visuals |
| `EnemyVision.cs` | 162 | Vision cone detection, line-of-sight checks, suspicion rate modulation by LLM score |
| `HelpPanel.cs` | 148 | Help overlay panel: commands reference, toggle with H key |
| `MicrophoneRecorder.cs` | 142 | Audio recording: Unity Microphone API, WAV conversion (16-bit PCM), sample trimming |
| `DoorAnimationGenerator.cs` | 154 | Editor tool: procedural door animation generation (Editor folder) |
| `VoicePuzzle.cs` | 140 | Legacy code-based voice puzzle (simpler predecessor to riddle system) |
| `PlayerMovement.cs` | 125 | First/third person movement: WASD, mouse look, camera toggle, morph mode |
| `ApiCall.cs` | 130 | Legacy AssemblyAI cloud STT service (replaced by local Whisper) |
| `AutomatedTester.cs` | 111 | F12 batch testing: 50 phrases through LLM pipeline, logs to CSV |
| `FurnitureMorphing.cs` | 112 | Legacy UI-based furniture morphing (replaced by voice system) |
| `MorphCamera.cs` | 86 | Camera controller for morphed state (orbit around furniture) |
| `DoorController.cs` | 74 | Door swing animation via RotateAround, lock/unlock state |
| `MicController.cs` | 74 | Legacy microphone volume monitor (debugging tool) |
| `GroqAudioService.cs` | 74 | Whisper.cpp STT client: sends WAV to local endpoint |
| `ZoneDetectionManager.cs` | 64 | Zone trigger detection: tracks which zone the player occupies |
| `NeuroFilterAi.cs` | 54 | Legacy patrol waypoint AI (simpler predecessor to EnemyAI) |
| `AnalyticsManager.cs` | 38 | CSV logging: writes interaction data to LLM_Performance_Log.csv |
| `SuspicionUI.cs` | 32 | UI bar displaying enemy suspicion level with color coding |
| `GameOverManager.cs` | 32 | Game over screen: displays message and reloads scene |
| `ZoneData.cs` | 12 | Zone metadata component: stores zone type and ID |
| `AudioEventSystem.cs` | 13 | Static event bus: broadcasts sound emission events to enemy AI |

### Scenes

| Scene | Purpose |
|-------|---------|
| `Assets/Scenes/TestScene.unity` | Main game scene with all systems active |
| `Assets/Scenes/SampleScene.unity` | Legacy debug/test scene (also in root Assets/Scenes/) |
| `Assets/Scenes/FinalWork_WorkSpace.unity` | Work-in-progress scene |

### Furniture Prefabs

10 furniture prefabs stored in `Assets/Resources/Props/`:

`BathTub01`, `Bed01`, `Bench`, `Chair01`, `Closet01`, `Cushion01`, `Drawer01`, `OfficeChair`, `Sofa01`, `Table01`

These are loaded dynamically via `Resources.Load<GameObject>("Props/" + prefabName)`.

## Technical Stack

### Unity Packages

| Package | Version | Purpose |
|---------|---------|---------|
| Universal Render Pipeline (URP) | 17.3.0 | Forward+ rendering, SSAO, 4x MSAA, shadow cascades |
| Input System | 1.17.0 | Keyboard/mouse input handling |
| AI Navigation | 2.0.9 | NavMesh for enemy pathfinding |
| Shader Graph | 17.3.0 | HoloDissolve shader (morphing dissolve effect) |
| TextMesh Pro | Included | Terminal UI rendering, monospace fonts |
| Unity UI (uGUI) | 2.0.0 | Canvas, buttons, images, UI bars |
| Post Processing | 3.5.1 | VHS filter effects (vignette, lens distortion, chromatic aberration) |
| Timeline | 1.8.10 | Animation timeline support |
| Visual Scripting | 1.9.9 | Visual scripting (not actively used) |

### External Services

| Service | Endpoint | Protocol |
|---------|----------|----------|
| Ollama | `http://localhost:11434/v1/chat/completions` | HTTP POST, OpenAI-compatible JSON |
| Whisper.cpp | `http://localhost:9090/inference` | HTTP POST, multipart form data |
| AssemblyAI (Legacy) | `https://api.assemblyai.com/v2/...` | HTTP (deprecated, replaced by Whisper) |

### AI Models

**Llama 3 (Meta)** -- 8B parameter model running via Ollama. Used as the primary model for evaluating morph plausibility and judging puzzle responses. Selected for its strong semantic understanding and ability to work with zone context.

**Phi-3 Mini (Microsoft)** -- 3.8B parameter model running via Ollama. Configured as the model in `GroqLLMService.cs`. Lighter weight, faster response times, suitable for real-time game interactions.

**Whisper Base (OpenAI)** -- ~142 MB quantized model (ggml-base.bin) running via Whisper.cpp. Used for speech-to-text transcription. The HTTP server prompt includes game-specific vocabulary: "morph chair table bed bench bathtub closet cushion drawer sofa door open close bypass security unmorph turn into furniture" to improve recognition accuracy.

## Design Patterns

| Pattern | Usage | Files |
|---------|-------|-------|
| **State** | Game object states, transitions between behaviors | `TerminalHacker.cs` (TerminalFase enum: Inactive, Boot, HackMinigame, Complete, etc.), `EnemyAI.cs` (EnemyState: Roam, Chase, Investigate) |
| **Observer** | Event-driven communication between systems | `AudioEventSystem.cs` (static OnSoundEmitted), `HackMinigame.cs` (OnHackSuccess, OnHackFailed, OnAttemptUsed), `ZoneData.cs` (OnEnter, OnExit) |
| **Chain of Responsibility** | Sequential command routing | `VoiceAppController.cs` routes voice through: door -> terminal -> puzzle -> LLM |
| **Adapter** | Unified interface for different door animation systems | `DoorController.cs` wraps legacy Animation, Mecanim Animator, and direct Transform rotation under one API |
| **Strategy** | Algorithm selection at runtime | `GroqLLMService.cs` switches between Ollama API call and offline keyword fallback |
| **Levenshtein Distance** | Fuzzy string matching for voice commands | `HackMinigame.cs` (FindBesteMatch), `TerminalHacker.cs` (ZelfdeAls) with configurable tolerance |
| **Fisher-Yates Shuffle** | Randomizing word order in hack minigame | `HackMinigame.cs` for unbiased shuffling of displayed words |
| **Singleton (Event Bus)** | Global event manager | `AudioEventSystem.cs` provides a static event that any script can invoke/subscribe to |

## Performance and Analytics

### LLM Performance Dashboard

A standalone HTML dashboard is located at `magazine_charts/index.html` in the root directory. It visualizes data from the LLM_Performance_Log.csv:

- **Scatter plot** -- Test number vs. score (color-coded: green = success >=80, orange = marginal 20-79, red = fail <20)
- **Histogram** -- Score distribution across 5 buckets (0-20, 21-40, 41-60, 61-80, 81-100)
- **Statistics bar** -- Total tests, successful matches, average score, average latency, success percentage

The dashboard uses a terminal/hacker aesthetic matching the game's visual theme. Open it in any browser. The data is currently hardcoded as a JavaScript array in the HTML file -- to use live CSV data, load the CSV file externally.

### Analytics Logging

`AnalyticsManager.cs` writes to `Assets/LLM_Performance_Log.csv` with the following columns:

| Column | Description |
|--------|-------------|
| DateTime | Timestamp of interaction |
| SpokenText | The transcribed voice input |
| AI_Selection | The prefab name selected by the LLM |
| Score | Plausibility score (0-100, -1 for errors) |
| Latency_ms | Round-trip time in milliseconds |

The CSV contains 280 entries spanning April 28 to May 31, 2026. Observed latency ranges from 0 ms (direct keyword matches that skip the LLM) to approximately 27,000 ms (first requests where the model loads into memory).

### Automated Testing

Press **F12** in the Unity Editor to run the automated batch test. The `AutomatedTester.cs` script sends 50 predefined phrases directly to the LLM (skipping audio recording and Whisper transcription):

- 20 logical morph requests (expected high score)
- 20 illogical morph requests (expected low score)
- 10 edge cases and typos (tests LLM robustness)

Results are logged to the CSV. A 4-second delay between requests prevents rate limiting.

## References and Credits

A comprehensive references file is maintained at `Assets/Scripts/Bronnen.txt` containing all academic sources, API documentation, tutorials, and asset licenses.

### External APIs and Services

| Service | Purpose | Link |
|---------|---------|------|
| Ollama | Local LLM server | [ollama.ai](https://ollama.ai) |
| Whisper.cpp | Local speech-to-text | [github.com/ggml-org/whisper.cpp](https://github.com/ggml-org/whisper.cpp) |
| AssemblyAI (Legacy) | Cloud STT (deprecated) | [assemblyai.com](https://www.assemblyai.com) |

### 3D Assets

| Asset | Source | License |
|-------|--------|---------|
| Furniture Mega Pack (500+ prefabs) | Unity Asset Store (OneSquareFoot) | Standard Unity EULA |
| Custom FBX models | Self-made (Area1_MeubelShowroom) | Proprietary |

### Fonts

| Font | License |
|------|---------|
| JetBrains Mono | SIL Open Font License |
| LiberationSans | SIL Open Font License |

### Academic References

- Levenshtein, V. I. (1966). "Binary codes capable of correcting deletions, insertions, and reversals." *Soviet Physics Doklady*.
- Fisher, R. A., & Yates, F. (1938). "Statistical tables for biological, agricultural and medical research."
- Knuth, D. E. (1997). "The Art of Computer Programming, Volume 2" (3rd ed.). Addison-Wesley.
- IBM & Microsoft (1991). "Multimedia Programming Interface and Data Specifications 1.0."
- Park, J. S., et al. (2023). "Generative Agents: Interactive Simulacra of Human Behavior." Stanford University. [arXiv:2304.03442](https://arxiv.org/abs/2304.03442)
- Wang, G., et al. (2023). "Voyager: An Open-Ended Embodied Agent with Large Language Models." [arXiv:2305.16291](https://arxiv.org/abs/2305.16291)
- Radford, A., et al. (2022). "Robust Speech Recognition via Large-Scale Weak Supervision." OpenAI. [GitHub](https://github.com/openai/whisper)
- Zhao, W. X., et al. (2023). "A Survey of Large Language Models." [arXiv:2303.18223](https://arxiv.org/abs/2303.18223)
- Yankelovich, N. (1996). "How do users know what to say?" *Interactions*, 3(6), 32-43.
- Microsoft (2024). "Xbox Accessibility Guidelines (XAGs)."
- Chen, J. (2007). "Flow in games (and everything else)." *Communications of the ACM*.
- Nystrom, R. (2014). "Game Programming Patterns." Genever Benning.
- Totten, C. W. (2014). "An Architectural Approach to Level Design." CRC Press.

### Tutorials

- Dissolve shader effect: [danielilett.com](https://danielilett.com/2020-04-15-tut5-4-urp-dissolve/) / [GitHub](https://github.com/daniel-ilett/dissolve-urp)
- Hologram shader: [Unity Learn](https://learn.unity.com/tutorial/create-a-hologram-shader)
- Brackeys dissolve shader: [YouTube](https://www.youtube.com/watch?v=taMp1g1pBeE)

## Known Issues and Missing Features

| Item | Status |
|------|--------|
| **LICENSE file** | Not present in the repository. Licenses are documented in `Bronnen.txt` but no formal LICENSE file exists. |
| **Build settings** | Only `SampleScene.unity` is added to the build settings. `TestScene.unity` (the main game scene) must be added before building. |
| **Mixed language code** | Variable names and comments in several scripts mix Dutch and English (e.g., `RouteerNaarDeur`, `huidigeFase`, `IsInBereik`, `ZoekTekstVeld`). User-facing text was translated to English in the latest commits. |
| **Misleading class names** | `GroqLLMService` and `GroqAudioService` communicate with Ollama and Whisper.cpp respectively, not with Groq. The names are legacy and no longer reflect the actual endpoints. |
| **Hardcoded API key** | `ApiCall.cs` (line 10) contains a hardcoded AssemblyAI API key: `fe0d18f4e26d4d44be37680f7e88e7d4`. This script is legacy and unused, but the key should still be removed or externalized. |
| **No unit tests** | Only `AutomatedTester.cs` exists for integration-style testing. No NUnit/Unity Test Framework tests are present. |
| **No CI/CD configuration** | No GitHub Actions, GitLab CI, or other pipeline configuration is included. |
| **No CHANGELOG** | Git history is available but no formal changelog file exists. |
| **No demo media** | The README would benefit from screenshots or a GIF demonstrating gameplay. |
| **LLM offline fallback** | When Ollama is unreachable, a simple keyword fallback runs. This can produce unreliable results. The game has no grace period or retry logic for server restarts. |
| **Empty/unused folders** | `Assets/fonts/` contains webfonts and variable TTF files that may not be used in the game. |
| **No error recovery** | If Whisper.cpp or Ollama crashes mid-game, there is no recovery mechanism beyond restarting the Unity scene. |
