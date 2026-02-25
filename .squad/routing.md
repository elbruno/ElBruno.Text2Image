# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture & scope | Morpheus | System design, project structure, tech decisions |
| Core implementation | Neo | Feature development, main application logic, refactoring |
| APIs & services | Trinity | Backend endpoints, data services, infrastructure |
| AI/ML & image generation | Oracle | Model integration, prompt engineering, image pipelines |
| Testing & quality | Tank | Unit tests, integration tests, edge cases, coverage |
| Code review | Morpheus | Review PRs, check quality, approve/reject work |
| Scope & priorities | Morpheus | What to build next, trade-offs, decisions |
| Async issue work (bugs, tests, small features) | @copilot 🤖 | Well-defined tasks matching capability profile |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, evaluate @copilot fit, assign `squad:{member}` label | Morpheus |
| `squad:morpheus` | Architecture, scope, or review work | Morpheus |
| `squad:neo` | Core implementation tasks | Neo |
| `squad:trinity` | Backend/API/infrastructure tasks | Trinity |
| `squad:oracle` | AI/ML and image generation tasks | Oracle |
| `squad:tank` | Testing and quality tasks | Tank |
| `squad:copilot` | Assign to @copilot for autonomous work (if enabled) | @copilot 🤖 |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, **Morpheus** triages it — analyzing content, evaluating @copilot's capability profile, assigning the right `squad:{member}` label, and commenting with triage notes.
2. **@copilot evaluation:** Morpheus checks if the issue matches @copilot's capability profile (🟢 good fit / 🟡 needs review / 🔴 not suitable). If it's a good fit, Morpheus may route to `squad:copilot` instead of a squad member.
3. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
4. When `squad:copilot` is applied and auto-assign is enabled, `@copilot` is assigned on the issue and picks it up autonomously.
5. Members can reassign by removing their label and adding another member's label.
6. The `squad` label is the "inbox" — untriaged issues waiting for Morpheus's review.

### Lead Triage Guidance for @copilot

When triaging, Morpheus should ask:

1. **Is this well-defined?** Clear title, reproduction steps or acceptance criteria, bounded scope → likely 🟢
2. **Does it follow existing patterns?** Adding a test, fixing a known bug, updating a dependency → likely 🟢
3. **Does it need design judgment?** Architecture, API design, UX decisions → likely 🔴
4. **Is it security-sensitive?** Auth, encryption, access control → always 🔴
5. **Is it medium complexity with specs?** Feature with clear requirements, refactoring with tests → likely 🟡

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. Morpheus handles all `squad` (base label) triage.
8. **@copilot routing** — when evaluating issues, check @copilot's capability profile in `team.md`. Route 🟢 good-fit tasks to `squad:copilot`. Flag 🟡 needs-review tasks for PR review. Keep 🔴 not-suitable tasks with squad members.
