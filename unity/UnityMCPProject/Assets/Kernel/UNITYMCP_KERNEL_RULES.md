# unitymcp — Kernel Rules (DO NOT MODIFY)

These rules protect the Unity-side “Kernel” from unsafe or accidental edits by generated code.

## Hard Rules
1. Files in `Assets/Kernel/` MUST NOT be modified by LLM-generated code.
2. All generated scripts MUST be written to `Assets/Generated/`.
3. unitymcp executes ONLY structured JSON commands. Never execute arbitrary C# code strings.
4. Verification gates:
   - Compile gate (no compile errors)
   - Runtime gate (Play Mode run window with no exceptions)
   - Visual gate (screenshots)
5. If a command fails, return structured errors (no crashes, no silent failures).

## Naming / Identity
- Project: **unitymcp**
- Tagline: **One-shot game builder for Unity game engine**
- Local server: `http://127.0.0.1:7777` (can be changed later)
