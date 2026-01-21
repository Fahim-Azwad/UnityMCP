# 🎥 UnityMCP Demo Video Script

**Goal**: Showcase the "Agentic Workflow" where Claude builds a game feature-by-feature in Unity.
**Duration**: ~2-3 Minutes.

---

## 🎬 Act 1: The Setup (0:00 - 0:30)

**Visuals**:
- Split screen: **Claude Desktop** on the Left, **Unity Editor** on the Right.
- Unity Console works (show "Server started").
- Empty Scene (or just Main Camera/Light).

**Action**:
1.  Type in Claude: *"Ping Unity and describe the current scene."*
2.  **Claude**: Calls `unity_ping` -> `unity_snapshot`.
3.  **Claude**: "Connected. The scene contains Main Camera and Directional Light."

**Narration/Text Overlay**:
"UnityMCP connects LLMs directly to the Engine. It sees what you see."

---

## 🎬 Act 2: Feature 1 - Constructing Values (0:30 - 1:15)

**Visuals**:
- Focus on Claude prompting.

**Action**:
1.  Type: *"Build Feature 1: A Player object (Capsule) with physics movement (WASD) and a 3rd person camera. Plan, then apply."*
2.  **Claude**:
    - Proposes Plan.
    - Calls `unity_apply` (Creates Capsule, Walls, Ground).
    - Calls `unity_write_script` (Writes `PlayerMovement.cs`, `CameraFollow.cs`).
3.  **Unity Side**:
    - Watch objects appear instantly.
    - Watch "Compiling" spinner in Unity bottom corner.

**Narration/Text Overlay**:
"It plans, writes C# scripts, and waits for compilation automatically."

---

## 🎬 Act 3: Verification & Play Mode (1:15 - 2:00)

**Visuals**:
- Focus on Unity Game View.

**Action**:
1.  **Claude**: *"Compilation complete. Attaching components. Now running Play Mode for 5 seconds."*
2.  **Unity**: Auto-enters Play Mode.
3.  **You**: Use Keyboard WASD to move the capsule around during the 5 seconds.
4.  **Unity**: Auto-exits Play Mode.
5.  **Claude**: "Play Mode finished. No errors detected."

**Narration/Text Overlay**:
"It verifies its own work by running the game and checking logs."

---

## 🎬 Act 4: Resilience (The "Wow" Factor) (2:00 - 2:30)

**Visuals**:
- Show the Console Log "Async Play runner aborted (domain reload)" (The Warning we fixed).

**Action**:
1.  Type: *"Add Feature 2: A Chasing Enemy."*
2.  **Claude**: Creates Enemy, writes script.
3.  **Unity**: Recompiles (Domain changes).
4.  **Claude**: "Detected recompilation. Waiting... Ready. Enemy Attached."

**Narration/Text Overlay**:
"It handles domain reloads and compilation locks without crashing."

---

## 🎬 Outro (2:30 - 3:00)

**Visuals**:
- Final Gameplay (Player running from Enemy).
- GitHub Repo URL.

**Text Overlay**:
"UnityMCP: Agentic Game Development."
"Available now at github.com/Fahim-Azwad/UnityMCP"

---
**Recording Tips**:
- Use OBS Studio.
- Record at 1080p/60fps.
- Ensure text in Claude is large enough to read.
