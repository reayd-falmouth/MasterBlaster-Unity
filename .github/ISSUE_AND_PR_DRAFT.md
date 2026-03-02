# GitHub Issue and Pull Request Drafts

Use the content below to create the issue and pull request on GitHub (e.g. at https://github.com/reayd-falmouth/MasterBlaster-Unity).

---

## Issue (create first)

**Title:** AI opponents for unconnected players and optional reinforcement learning

**Labels (suggested):** `enhancement`, `ai`

**Body:**

### Summary
- Players without an assigned controller should be controlled by AI so 2–5 player games work with a single keyboard (or fewer gamepads than players).
- Optionally support training AI via reinforcement learning (Unity ML-Agents) for smarter opponents.

### Acceptance criteria
- [ ] At game start, controller count is detected (keyboard + connected joysticks); first N players get devices, remaining slots are AI.
- [ ] Input is abstracted via `IPlayerInput`; human (keyboard/gamepad) and AI both use the same movement/bomb flow.
- [ ] Scripted AI: safety → offense → collect → chase/wander so unconnected players play without any setup.
- [ ] Optional: ML-Agents agent (observations, discrete actions, rewards) with a GameManager toggle “Use Reinforcement Learning” and training docs/config for PPO.

### Out of scope
- Menu UI to manually assign “Player 2: AI” (auto-detect only for this issue).

---

## Pull Request (create after pushing branch)

**Title:** feat: AI opponents for unconnected players and RL (ML-Agents) support

**Base branch:** `main`  
**Compare branch:** `feature/ai-opponents-rl`

**Description:**

### What
- **Controller detection:** Keyboard = device 0, joysticks = 1,2,…; assign to player slots in order; slots without a device get AI.
- **Input abstraction:** `IPlayerInput` (move, bomb, detonate). `HumanPlayerInput` (keyboard/gamepad) and `AIPlayerInput` (driven by a “brain”).
- **Scripted AI:** `ScriptedAIBrain` – safety first, then offense, collect, chase/wander. Uses `BombInfo` on bombs for danger checks; `players` array built from GameManager refs so AI sees opponents.
- **Reinforcement learning:** `BombermanAgent` (ML-Agents) with 15 observations, discrete actions (move 5, bomb 2, detonate 2), step/kill/death/item rewards; `MLAgentsBrain` adapts agent to `IAIBrain`. GameManager “Use Reinforcement Learning” toggle; training README and `bomberman_config.yaml`.
- **SessionManager:** `AssignInputDevices()`, `GetAssignedDevice()`.
- **PlayerController:** notifies `BombermanAgent` on death for RL reward/episode end.

### Lint
- Linting run on changed scripts; no issues reported.

### Notes
- Push from this environment failed (permission denied for `reayd-awsbot`). Please push the branch from your side:  
  `git push -u origin feature/ai-opponents-rl`  
  Then open the PR using the compare branch `feature/ai-opponents-rl` against `main`.
