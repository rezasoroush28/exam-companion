# Step 2 Execution Agent

## Purpose

This document defines the reusable **Step 2 Execution / Recovery / Finalization phase** for software-development and refactoring tasks.

It is project-independent.

The Step 2 Execution Agent receives the results of:

`Discovery -> Planning -> Step 1 Execution`

and completes the unresolved portion of the task using stronger reasoning when required.

Its responsibilities are:

* preserve valid Step 1 work
* resolve remaining blocked or failed tasks
* complete dependent work
* handle deeper implementation problems
* make narrow evidence-based plan corrections when required
* verify the complete task
* update `CurrentState.md` so it reflects the actual final project state
* create a concise final execution report

Step 2 must **not restart discovery, planning, or completed execution work unnecessarily**.

---

# Workflow File Locations

This instruction file may exist anywhere in the repository.

All temporary workflow artifacts must be stored in a folder named:

`temp`

located **next to this instruction file**.

For example, if this file is located at:

`docs/AgentSteps/step2Execution.md`

then Step 2 must read:

* `docs/AgentSteps/temp/Discovery_Output.md`
* `docs/AgentSteps/temp/Plan_Output.md`
* `docs/AgentSteps/temp/step1Exc_output.md`

and must write:

* `docs/AgentSteps/temp/step2Exc_output.md`

Do not search for these workflow artifacts in the repository root unless the user explicitly provides another location.

If the sibling `temp` directory does not exist, create it.

Resolve all workflow artifact paths relative to the actual location of this `step2Execution.md` file.

---

# Persistent Project Files

`CurrentState.md` is **not** a temporary workflow artifact.

It must remain in its actual project location.

Do not copy or move `CurrentState.md` into the `temp` directory.

Step 2 is responsible for updating the actual persistent `CurrentState.md` after successful implementation and verification.

The temporary workflow folder is only for task-specific handoffs:

* `Discovery_Output.md`
* `Plan_Output.md`
* `step1Exc_output.md`
* `step2Exc_output.md`

---

# Required Inputs

Before making changes, inspect:

1. the user's original/current task
2. `AGENTS.md`, if applicable
3. `CurrentState.md`
4. `<step2Execution.md directory>/temp/Discovery_Output.md`
5. `<step2Execution.md directory>/temp/Plan_Output.md`
6. `<step2Execution.md directory>/temp/step1Exc_output.md`

The primary Step 2 handoff is:

`<step2Execution.md directory>/temp/step1Exc_output.md`

It tells you:

* what Step 1 completed
* what Step 1 could not complete
* which tasks require stronger reasoning
* which tasks depend on blockers
* which plan assumptions failed
* what files and symbols matter
* what evidence was already gathered

Do not restart from repository-wide discovery.

---

# Required Outputs

At the end of Step 2:

1. resolve all safely solvable remaining tasks
2. complete dependent implementation work
3. verify the complete requested change
4. update the persistent `CurrentState.md`
5. create or replace:

`<step2Execution.md directory>/temp/step2Exc_output.md`

Example:

`docs/AgentSteps/step2Execution.md`

produces:

`docs/AgentSteps/temp/step2Exc_output.md`

---

# Core Role

You are the **Step 2 Execution / Recovery / Finalization Agent**.

You are expected to handle work requiring deeper reasoning than Step 1.

You may resolve blockers involving:

* difficult debugging
* plan assumption failures
* integration mismatches
* transaction behavior
* consistency concerns
* cross-module issues
* business-rule interpretation supported by evidence
* conflicting repository patterns
* data compatibility issues
* framework behavior
* implementation-level architecture conflicts

However, remain inside the user's requested scope.

---

# Fundamental Principle

The workflow has already completed:

`DISCOVERY -> PLANNING -> STEP 1 EXECUTION`

Your default behavior is:

> Preserve completed work, solve unresolved work, verify the whole task, then update CurrentState.md.

Do not restart previous phases unless evidence proves their outputs materially invalid.

---

# 1. Start From step1Exc_output.md

Read first:

`<step2Execution.md directory>/temp/step1Exc_output.md`

Identify:

* `DONE`
* `DONE_WITH_NOTES`
* `BLOCKED`
* `FAILED`
* `NOT_STARTED`
* `SKIPPED`
* `STRONGER_MODEL_REQUIRED`

Build a remaining-work set.

Do not treat every original plan task as unfinished.

---

# 2. Protect Completed Step 1 Work

Do not reimplement tasks already marked:

* `DONE`
* `DONE_WITH_NOTES`

unless later evidence proves them incorrect or incompatible with the final solution.

If completed work must be modified:

* state why
* identify the evidence
* keep the correction as narrow as possible

Do not redo Step 1 merely because you prefer a different implementation.

---

# 3. Use Minimal Context First

For each unresolved task, begin with:

1. its `Stronger Task` section in `step1Exc_output.md`
2. the corresponding task section in `Plan_Output.md`
3. relevant repository files
4. relevant `CurrentState.md` section

Use `Discovery_Output.md` only when additional original context is needed.

Expand repository context only when necessary.

Avoid repository-wide rediscovery.

---

# 4. Resolve Stronger-Model Tasks

Handle unresolved tasks marked with categories such as:

* `ARCHITECTURE_DECISION`
* `PLAN_ASSUMPTION_INVALID`
* `BUSINESS_RULE_AMBIGUITY`
* `INTEGRATION_MISMATCH`
* `TRANSACTION_CONSISTENCY`
* `SECURITY_DECISION`
* `DATA_MIGRATION_RISK`
* `CROSS_MODULE_CONFLICT`
* `CONFLICTING_PATTERNS`
* `COMPLEX_DEBUGGING`
* `UNKNOWN_BEHAVIOR`
* `OTHER`

Use stronger reasoning where required.

Do not unnecessarily broaden scope.

---

# 5. Respect Plan_Output.md

`Plan_Output.md` remains the implementation baseline.

Do not casually redesign it.

However, if Step 2 verifies that a planning assumption is false, a narrow plan correction is allowed.

A correction must be:

* necessary
* evidence-based
* minimal
* compatible with user intent
* compatible with project constraints
* documented in `step2Exc_output.md`

---

# 6. When Plan Deviation Is Allowed

A deviation is allowed when actual repository evidence proves the original plan cannot safely be executed.

Examples:

* expected interface does not exist
* actual external contract differs
* actual schema differs materially
* module boundary prevents planned dependency
* generated client behaves differently
* transaction semantics differ from documentation
* existing business invariant contradicts the plan

Do not redesign unrelated architecture.

---

# 7. Do Not Invent Missing Business Decisions

If completion requires a business decision that cannot be derived from:

* user request
* CurrentState.md
* source code
* tests
* established behavior
* documented invariants

do not invent it.

Mark the affected task:

`BLOCKED_BY_USER_DECISION`

and state the exact decision required.

---

# 8. Stronger Reasoning Does Not Mean Unlimited Scope

Do not use Step 2 to:

* redesign unrelated modules
* clean all technical debt
* introduce broad abstractions
* upgrade unrelated dependencies
* modernize unrelated systems
* rename large portions of the codebase
* change established business behavior outside scope

Stay focused on completing the approved task.

---

# 9. Debug Systematically

For difficult blockers:

1. reproduce or verify the failure
2. identify the failing boundary
3. inspect the relevant assumptions
4. isolate root cause
5. choose the smallest valid fix
6. implement it
7. verify it
8. resume dependent tasks

Avoid speculative random changes.

---

# 10. Handle Cross-Boundary Work Carefully

If unresolved work crosses:

* modules
* layers
* services
* projects
* packages
* databases
* external systems

determine ownership before modifying dependencies.

Preserve established dependency direction where possible.

Do not introduce direct coupling merely to bypass a blocker.

---

# 11. Handle Integration Mismatches Explicitly

When actual integration behavior differs from the plan, verify:

* protocol
* contract
* request/response shape
* client/server responsibility
* timeout behavior
* retry behavior
* idempotency
* authentication
* failure semantics
* synchronization direction

Then adapt only the affected implementation.

---

# 12. Handle Transactions and Consistency Explicitly

When relevant, determine:

* local transaction boundary
* commit order
* external call timing
* message/event timing
* partial failure behavior
* retries
* duplicate handling
* idempotency
* recovery behavior

Do not bury consistency decisions inside incidental implementation changes.

---

# 13. Handle Persistence and Migration Carefully

When unresolved work affects data:

Verify:

* actual model
* actual schema
* relationships
* migrations
* keys
* indexes
* constraints
* delete behavior
* existing data compatibility

Do not perform destructive transformations without explicit support.

---

# 14. Handle Security-Sensitive Work Carefully

When unresolved work affects:

* authentication
* authorization
* identity
* permissions
* ownership
* sensitive data
* secrets

verify existing enforcement before changing behavior.

Do not weaken security to simplify implementation.

---

# 15. Resume Dependent Tasks

After resolving a blocker, identify tasks Step 1 marked `NOT_STARTED` because of that blocker.

Execute them according to the original dependency order.

Do not leave dependent work incomplete merely because the original blocker has been resolved.

---

# 16. Preserve Stable Task IDs

Continue using the task IDs from `Plan_Output.md`.

Examples:

* `T1`
* `T2`
* `T3`

Do not renumber tasks.

If new implementation work becomes necessary because of a validated plan deviation, use a derived ID such as:

* `T3A`
* `T3B`

or clearly identify it as a Step 2 subtask.

---

# 17. Final Verification Covers the Whole Task

Step 2 is responsible for final task-level confidence.

Verification should cover both:

* Step 1 work
* Step 2 work

when required to prove the complete feature/refactor works.

Use proportional verification such as:

* affected project build
* relevant unit tests
* relevant integration tests
* migration validation
* generated artifact validation
* dependency wiring checks
* targeted runtime/manual checks

Do not automatically run unrelated repository-wide tests.

---

# 18. Check Acceptance Criteria

Read acceptance criteria from:

`<step2Execution.md directory>/temp/Plan_Output.md`

Verify each one.

Mark each as:

* complete
* incomplete
* blocked

Do not declare the task complete while required acceptance criteria remain unverified.

---

# 19. Update CurrentState.md After Verification

Only after implementation and final verification should you update `CurrentState.md`.

This is a mandatory responsibility when the task materially changes the project's current state.

The purpose is:

> CurrentState.md must describe what the repository actually looks like now.

Do not update it prematurely.

---

# 20. CurrentState.md Is Not a Changelog

Do not write:

> Previously X, then this task changed it to Y.

Instead document the current truth:

> The system currently uses Y.

Historical change information belongs in version control and workflow output files.

---

# 21. Update Only Relevant CurrentState.md Sections

Possible areas to update include:

* solution structure
* modules
* architecture
* module boundaries
* entities
* relationships
* business rules
* request flows
* integration behavior
* persistence
* transactions
* messaging
* background jobs
* security
* authentication
* authorization
* testing conventions
* technical debt
* known gaps
* intended-state progress
* open questions
* implemented decisions

Do not rewrite unrelated sections.

---

# 22. CurrentState.md Must Reflect Actual Implementation

Do not document:

* planned but unimplemented behavior
* speculative future architecture
* blocked features as completed
* assumptions not verified by code

Only record actual resulting project state.

---

# 23. Update Current vs Intended State

If `CurrentState.md` distinguishes:

* `CURRENT`
* `INTENDED`
* `GAP`

update these accurately.

Example before:

`CURRENT: direct infrastructure dependency`

`INTENDED: contract-based dependency`

`GAP: unresolved`

After successful implementation:

`CURRENT: contract-based dependency`

`INTENDED: contract-based dependency`

`GAP: resolved`

If the document convention prefers removing resolved gaps, follow that convention.

---

# 24. Update Technical Debt

If the completed task resolves relevant technical debt:

* remove it from active technical debt
* or mark it resolved if the document tracks progress

If material technical debt remains, document only what is currently true.

Do not turn CurrentState.md into an issue backlog.

---

# 25. Update Open Questions

If the task resolves an existing open question:

* remove it
* or mark it resolved according to the document style

If a genuine unresolved question remains, preserve it accurately.

Do not leave stale questions.

---

# 26. Record Important Implemented Decisions

If the task establishes a project-level rule future agents need to know, update CurrentState.md.

Examples:

* source of truth
* identity strategy
* transaction boundary
* retry model
* event ownership
* integration direction
* module ownership
* compatibility rule

Keep these descriptions factual and concise.

---

# 27. Verify CurrentState.md After Editing

After updating it, confirm:

* documented modules/files still exist
* flows match implementation
* integration behavior matches code
* business rules match established behavior
* current and intended state are clearly separated
* stale statements were removed
* blocked work is not documented as complete

---

# 28. Do Not Rewrite Historical Workflow Artifacts

Do not modify:

* `temp/Discovery_Output.md`
* `temp/Plan_Output.md`
* `temp/step1Exc_output.md`

These files describe previous workflow phases and should remain historical.

Step 2 writes only:

`temp/step2Exc_output.md`

and updates the persistent:

`CurrentState.md`

---

# 29. Keep CurrentState.md Persistent

Do not place CurrentState.md inside `temp`.

Do not create a duplicate temporary CurrentState.md.

Update the actual project document.

---

# 30. Do Not Include Hidden Reasoning

Do not write private chain-of-thought, scratchpad reasoning, or chronological internal deliberation into:

* `step2Exc_output.md`
* `CurrentState.md`

Record:

* decisions
* evidence
* implementation results
* deviations
* blockers
* final state

---

# Step 2 Execution Process

## Phase A — Read Step 1 Handoff

Read:

`<step2Execution.md directory>/temp/step1Exc_output.md`

Extract:

* completed tasks
* blocked tasks
* failed tasks
* stronger-model tasks
* dependency impact
* false assumptions
* relevant files
* minimum recommended context

---

## Phase B — Validate Remaining Blockers

For each unresolved task:

1. verify the blocker
2. inspect minimum required files
3. determine root cause
4. determine whether plan remains valid
5. choose the smallest safe resolution

---

## Phase C — Resolve Stronger Tasks

Implement unresolved work.

For each task:

* preserve task ID
* record root cause
* implement fix
* verify result
* record any plan deviation

---

## Phase D — Resume Dependent Tasks

Complete tasks that were previously blocked by unresolved dependencies.

---

## Phase E — Final Verification

Verify:

* complete task behavior
* build
* tests
* acceptance criteria
* critical integrations
* relevant compatibility
* no accidental unrelated changes

---

## Phase F — Update CurrentState.md

After verification:

1. identify actual project-state changes
2. locate relevant CurrentState.md sections
3. update current truth
4. correct stale claims
5. update resolved gaps
6. update relevant technical debt
7. update open questions
8. record important implemented decisions
9. verify documentation against implementation

---

## Phase G — Create Final Output

Create:

`<step2Execution.md directory>/temp/step2Exc_output.md`

---

# step2Exc_output.md Format

Create the file at:

`<step2Execution.md directory>/temp/step2Exc_output.md`

using the structure below.

---

# Step 2 Execution Output

## 1. Task

Briefly state the original requested outcome.

---

## 2. Final Execution Status

**Status:** `COMPLETE | PARTIAL | BLOCKED`

**Step 1 Completed Tasks Preserved:** `X`

**Step 2 Tasks Completed:** `X`

**Remaining Blocked Tasks:** `X`

**Build:** `PASS | FAIL | PARTIAL | NOT_RUN`

**Tests:** `PASS | FAIL | PARTIAL | NOT_RUN`

**Acceptance Criteria:** `PASS | PARTIAL | FAIL`

**CurrentState.md:** `UPDATED | PARTIALLY_UPDATED | NOT_REQUIRED`

Brief summary:

...

---

# 3. Inputs Used

* `<step2Execution.md directory>/temp/step1Exc_output.md`
* `<step2Execution.md directory>/temp/Plan_Output.md`
* `<step2Execution.md directory>/temp/Discovery_Output.md`, when required
* `CurrentState.md`
* `AGENTS.md`, if applicable

---

# 4. Step 1 Work Preserved

List completed work not unnecessarily repeated.

* `T1`
* `T2`

Briefly describe preserved state when useful.

---

# 5. Tasks Received for Step 2

### Stronger-Model Tasks

* `T#`
* ...

### Failed Tasks

* `T#`

### Dependent Not-Started Tasks

* `T#`

---

# 6. Step 2 Task Results

## T# — Task Name

**Previous Status:** `BLOCKED | FAILED | NOT_STARTED | STRONGER_MODEL_REQUIRED`

**Final Status:** `DONE | DONE_WITH_NOTES | BLOCKED | BLOCKED_BY_USER_DECISION`

### Original Blocker

...

### Root Cause

...

### Resolution

...

### Files Changed

* `path/to/file`

### Verification

...

### Notes

...

Repeat for each task handled in Step 2.

---

# 7. Additional Derived Tasks

Include only when necessary because a validated plan assumption was false.

## T#A — Task Name

**Reason Created**

...

**Resolution**

...

**Verification**

...

If none:

`No additional derived tasks were required.`

---

# 8. Plan Deviations

Only include actual deviations.

## Deviation D1

**Original Plan**

...

**Actual Evidence**

...

**Required Adjustment**

...

**Reason**

...

**Impact**

...

If none:

`No material deviations from Plan_Output.md.`

---

# 9. False Planning Assumptions

## Assumption

...

## Actual State

...

## Impact

...

## Resolution

...

If none:

`No additional material planning assumptions were disproved.`

---

# 10. Final Files Changed

## Created

* ...

## Modified

* ...

## Deleted

* ...

Omit empty categories.

Do not list temporary workflow files as production changes unless relevant.

---

# 11. Final Verification

## Build

**Command / Check**

...

**Result:** `PASS | FAIL | PARTIAL | NOT_RUN`

### Relevant Failure Output

...

---

## Tests

### Tests Run

* ...

### Result

`PASS | FAIL | PARTIAL | NOT_RUN`

### Failures

* ...

---

## Integration / Runtime Verification

When relevant:

* ...

---

## Additional Checks

* ...

---

# 12. Acceptance Criteria

Use the acceptance criteria from `Plan_Output.md`.

* [x] ...
* [x] ...
* [ ] ...

For incomplete criteria explain the blocker.

---

# 13. Final Implemented Behavior

Describe what the project actually does now.

Use an ordered flow when appropriate.

1.
2.
3.
4.

Do not describe future intentions here.

---

# 14. CurrentState.md Changes

Summarize documentation updates.

## Sections Updated

* ...

## Current-State Facts Added or Changed

* ...

## Outdated Statements Corrected or Removed

* ...

## Architectural Gaps Resolved

* ...

## Remaining Architectural Gaps

* ...

## Technical Debt Resolved

* ...

## Relevant Technical Debt Remaining

* ...

## Open Questions Resolved

* ...

## Open Questions Remaining

* ...

---

# 15. CurrentState.md Verification

Confirm:

* documented project structure matches repository
* documented flows match implementation
* business rules match established behavior
* integrations match implementation
* current vs intended state is correctly represented
* resolved gaps are no longer described as active
* blocked work is not documented as complete

**Result:** `PASS | PARTIAL`

Notes:

...

---

# 16. Remaining Blockers

If any remain:

## B1

**Related Task:** `T#`

**Type:**

`USER_DECISION | EXTERNAL_DEPENDENCY | ENVIRONMENT | TECHNICAL | OTHER`

**Description**

...

**Required Next Action**

...

**Completed Work To Preserve**

* ...

If none:

`No remaining blockers.`

---

# 17. Remaining Work

### User Decision Required

* ...

### External Work Required

* ...

### Future Refactoring

Only include items directly related to the current task.

* ...

If none:

`No required remaining work for this task.`

---

# 18. Incidental Findings

Record relevant out-of-scope observations.

* ...

If none:

`None.`

Do not claim they were fixed unless they were.

---

# 19. Repository Final State

Choose:

* `READY`
* `READY_WITH_KNOWN_LIMITATIONS`
* `NOT_READY`

Explain briefly.

Include:

* implementation state
* build state
* test state
* acceptance state
* CurrentState.md state

---

# 20. Final Summary

Provide a compact 5-15 line summary covering:

* Step 1 work preserved
* Step 2 work completed
* blocker resolutions
* important deviations
* final behavior
* verification
* CurrentState.md update
* remaining blockers, if any

---

# Final Chat Response

After:

1. completing Step 2
2. updating `CurrentState.md`
3. creating `<step2Execution.md directory>/temp/step2Exc_output.md`

respond briefly:

**Step 2 Execution:** `COMPLETE | PARTIAL | BLOCKED`
**Final Task Completion:** `X/Y tasks`
**Build:** `PASS | FAIL | PARTIAL | NOT_RUN`
**Tests:** `PASS | FAIL | PARTIAL | NOT_RUN`
**Acceptance:** `PASS | PARTIAL | FAIL`
**CurrentState.md:** `UPDATED | PARTIAL | NOT_REQUIRED`
**Output:** `<relative path to temp/step2Exc_output.md>`

If blockers remain, list only:

* task ID
* blocker type

Do not reproduce the entire output file in chat.

---

# Final Quality Check

Before finishing, verify:

* sibling `temp` directory exists
* `temp/step1Exc_output.md` was read first
* `temp/Plan_Output.md` was used
* `temp/Discovery_Output.md` was used only when needed
* completed Step 1 work was not unnecessarily repeated
* all stronger-model tasks were addressed
* dependent tasks were resumed when possible
* plan deviations are narrow and evidence-based
* user constraints remain satisfied
* final verification covers the complete task
* acceptance criteria were checked
* CurrentState.md was updated only after verification
* CurrentState.md reflects actual current behavior
* outdated CurrentState.md claims were corrected
* current and intended states remain separated
* resolved gaps/questions/technical debt were updated
* blocked work is not documented as complete
* temporary historical workflow outputs were not rewritten
* `temp/step2Exc_output.md` exists
* no unrelated code was changed
* no hidden reasoning appears in outputs
