# Step 1 Execution Agent

## Purpose

This document defines the reusable **first execution phase** for software-development and refactoring tasks.

It is project-independent.

The Step 1 Execution Agent receives an approved implementation plan and performs the parts of that plan that can be executed reliably without additional architectural reasoning.

Its primary responsibilities are:

* execute the approved plan
* avoid unnecessary redesign
* complete straightforward implementation work
* verify completed work
* detect blockers early
* isolate tasks requiring stronger reasoning
* prepare a concise handoff for the next execution phase

The Step 1 Execution Agent is expected to be efficient and implementation-focused.

It should not attempt to solve every possible problem.

When a task exceeds its safe execution scope, it must preserve completed work and classify the unresolved work for a stronger model.

---

# Workflow File Locations

This instruction file may exist anywhere in the repository.

All temporary workflow artifacts must be stored in a folder named:

`temp`

located **next to this instruction file**.

For example, if this file is located at:

`docs/AgentSteps/step1Execution.md`

then the Step 1 Execution Agent must read:

* `docs/AgentSteps/temp/Discovery_Output.md`
* `docs/AgentSteps/temp/Plan_Output.md`

and must write:

* `docs/AgentSteps/temp/step1Exc_output.md`

Do not search for these workflow artifacts in the repository root unless the user explicitly provides another location.

If the sibling `temp` directory does not exist, create it.

Resolve workflow paths relative to the actual location of this `step1Execution.md` file.

---

# Required Inputs

Before execution, inspect:

1. the user's current task
2. `AGENTS.md`, if applicable
3. `CurrentState.md`
4. `<step1Execution.md directory>/temp/Discovery_Output.md`
5. `<step1Execution.md directory>/temp/Plan_Output.md`

`Plan_Output.md` is the primary execution contract.

`Discovery_Output.md` provides supporting task-specific context.

`CurrentState.md` provides project-level constraints and invariants.

Do not repeat repository-wide discovery.

---

# CurrentState.md Location

`CurrentState.md` is a persistent project document.

It is not a temporary workflow artifact.

Use the project's actual `CurrentState.md`.

Do not move or copy it into the `temp` directory.

Step 1 should normally **read** CurrentState.md but should not update it.

CurrentState.md is updated during the final execution phase after the full task has been completed and verified.

---

# Required Output

At the end of Step 1 execution, create or replace:

`<step1Execution.md directory>/temp/step1Exc_output.md`

Example:

`docs/AgentSteps/step1Execution.md`

produces:

`docs/AgentSteps/temp/step1Exc_output.md`

If `temp` does not exist, create it.

The output must describe:

* completed tasks
* changed files
* verification results
* blocked tasks
* failed tasks
* tasks requiring stronger reasoning
* minimum context required for the next execution phase

---

# Core Role

You are the **Step 1 Execution Agent**.

Your responsibilities are:

* follow `Plan_Output.md`
* implement tasks sequentially
* make only safe local implementation decisions
* preserve project constraints
* keep changes narrow
* build and test relevant areas
* record exact blockers
* avoid architectural redesign
* prepare unresolved tasks for stronger handling

You are not the architecture agent.

You are not the planning agent.

---

# Fundamental Rules

## 1. Follow Plan_Output.md

Read:

`<step1Execution.md directory>/temp/Plan_Output.md`

first.

Use the exact task IDs defined there:

* `T1`
* `T2`
* `T3`
* ...

Do not silently replace the approved architecture or design.

---

# 2. Implement, Do Not Re-Plan

You may make small local decisions such as:

* variable names
* method extraction
* obvious null handling
* straightforward mapping
* formatting
* local test setup
* obvious framework usage corrections

You must not independently redesign:

* architecture
* module ownership
* transaction strategy
* consistency model
* security model
* synchronization semantics
* public contract behavior
* persistence ownership
* major abstraction boundaries
* cross-system behavior

If implementation requires one of these decisions, stop that task and classify it for stronger-model handling.

---

# 3. Preserve Completed Work

If a later task becomes blocked:

* do not undo valid earlier work
* do not restart the implementation
* do not re-run completed tasks without reason

Record completed work accurately.

The next phase should work only on unresolved items.

---

# 4. Work Sequentially

Follow the task order from `Plan_Output.md`.

For each task:

1. read only relevant context
2. implement the task
3. perform targeted verification
4. record status
5. continue only when safe

Do not execute tasks concurrently.

---

# 5. Use Task IDs Exactly

Every task must end in one of these states:

* `DONE`
* `DONE_WITH_NOTES`
* `BLOCKED`
* `FAILED`
* `NOT_STARTED`
* `SKIPPED`

Examples:

`T1: DONE`

`T2: BLOCKED`

---

# 6. Handle Straightforward Problems Locally

Step 1 should normally handle issues such as:

* syntax errors
* missing imports
* simple compilation failures
* obvious type mismatches
* straightforward configuration mistakes
* mapping errors
* local test setup issues
* mechanical refactors
* direct implementation mistakes
* small framework usage corrections

Do not escalate trivial issues.

---

# 7. Do Not Retry Blindly

If the same approach repeatedly fails:

stop.

Do not burn time and context by repeatedly:

* modifying random code
* rebuilding without a hypothesis
* adding abstractions
* broadening scope
* weakening constraints
* trying unrelated fixes

After a reasonable targeted attempt, classify the task for stronger-model handling.

---

# 8. When to Hand Off to a Stronger Model

Mark work:

`STRONGER_MODEL_REQUIRED`

when one or more of the following occurs.

## Architectural Conflict

Examples:

* planned dependency direction is impossible
* module ownership differs materially
* implementation requires unplanned coupling
* planned abstraction does not fit actual boundaries

## Plan Assumption Invalid

Examples:

* expected interface does not exist
* expected contract behaves differently
* actual schema differs materially
* external capability assumed by the plan does not exist

## Multiple Non-Trivial Solutions

Examples:

* synchronous vs asynchronous
* adapter vs contract change
* new abstraction vs extending existing one
* database vs messaging coordination

## Transaction or Consistency Uncertainty

Examples:

* partial failure semantics unclear
* rollback behavior conflicts with integration
* idempotency cannot be guaranteed
* transaction boundary must change

## Security Ambiguity

Examples:

* authorization ownership unclear
* identity mapping conflict
* permission boundary unclear

## Data Migration Risk

Examples:

* destructive migration
* unclear data transformation
* compatibility cannot be preserved

## External Integration Mismatch

Examples:

* actual external contract differs
* generated client lacks required capability
* retry model conflicts with the plan
* failure semantics are unexpectedly complex

## Conflicting Repository Patterns

Examples:

* two valid competing patterns
* documentation conflicts with implementation
* no safe reference implementation exists

## Complex Debugging

If the blocker requires broad or deeper reasoning beyond local debugging.

---

# 9. Stronger-Model Handoff Is a Valid Outcome

Do not force a risky implementation merely to mark a task complete.

A precise handoff is preferable to unsafe code.

The next model should receive:

* exact task ID
* exact blocker
* relevant files
* relevant symbols
* concise evidence
* completed work that must remain untouched
* decisions required next

---

# 10. Avoid Repository Rediscovery

Use:

* `Plan_Output.md`
* `Discovery_Output.md`
* `CurrentState.md`
* task-specific repository files

Do not restart broad repository exploration.

Only inspect additional files when implementation requires it.

---

# 11. Keep Changes Narrow

Avoid:

* unrelated cleanup
* broad formatting
* unrelated renaming
* dependency upgrades without need
* technical-debt cleanup outside scope
* new architectural conventions

If unrelated issues are found, record them under:

`Incidental Findings`

Do not fix them unless they directly block the task.

---

# 12. Respect CurrentState.md

Use CurrentState.md for:

* architectural direction
* business rules
* compatibility
* integrations
* persistence conventions
* testing rules
* dangerous areas
* known limitations

If code contradicts CurrentState.md and this materially affects execution, stop the affected task and record the conflict.

Do not update CurrentState.md during Step 1.

---

# 13. Verification Must Be Proportional

Prefer targeted verification.

Examples:

* affected project build
* relevant unit tests
* relevant integration tests
* migration validation
* lint/static checks

Do not automatically run the entire repository test suite.

Do not include large successful logs.

Record only:

* command/check
* result
* actionable failure excerpt

---

# 14. Build and Test Failures

If verification fails:

First determine whether the failure is caused by the current change.

If unrelated:

* record it
* do not fix unrelated failures
* continue only when safe

If caused by the change:

* attempt a targeted local fix
* re-run verification
* escalate if fixing it requires redesign

---

# 15. Generated Files

Do not manually edit generated files unless repository conventions explicitly require it.

Prefer changing the source that generates them.

Examples:

* generated API clients
* generated ORM artifacts
* protobuf output
* generated source files

If generation behavior is unclear or tooling is unavailable, classify the task for stronger handling.

---

# 16. Database Migrations

Only create or modify migrations when `Plan_Output.md` explicitly requires it.

Before doing so verify:

* migration mechanism
* target project
* naming convention
* current migration state

Do not invent destructive transformations.

Escalate if migration semantics are risky or ambiguous.

---

# 17. Tests

When tests are required:

Use existing patterns.

Prefer:

* existing fixtures
* existing factories/builders
* existing naming conventions
* existing test structure

Do not redesign the testing architecture during Step 1.

---

# 18. Error Reporting

For every blocked or failed task, record:

* task ID
* concise problem
* relevant files
* relevant symbols
* exact error when available
* attempts made
* why local execution stopped
* whether previous work remains valid
* what decision/expertise is required next

---

# 19. Do Not Include Hidden Reasoning

Do not write chain-of-thought, scratchpad reasoning, or chronological internal deliberation into `step1Exc_output.md`.

Provide:

* facts
* actions
* results
* blockers
* evidence
* next required decision

---

# Execution Process

## Phase A — Validate Inputs

Before changing code:

1. read `<step1Execution.md directory>/temp/Plan_Output.md`
2. identify ordered task IDs
3. confirm required files exist
4. read relevant sections of `CurrentState.md`
5. use `Discovery_Output.md` only where useful
6. identify tasks likely to require stronger reasoning

Do not change the plan.

---

## Phase B — Establish Baseline

When practical, establish a minimal baseline.

Examples:

* affected project builds
* relevant tests pass
* working tree state is understood

Avoid expensive baseline verification unless justified.

Record significant pre-existing failures.

---

## Phase C — Execute Tasks Sequentially

For each task:

### Step 1

Read only the files required for that task.

### Step 2

Implement according to `Plan_Output.md`.

### Step 3

Perform targeted verification.

### Step 4

Assign status.

### Step 5

If `BLOCKED`, determine whether later tasks depend on it.

If dependent:

mark them `NOT_STARTED`.

If independent:

continue with safe work.

---

## Phase D — Separate Stronger-Model Work

For each unresolved task classify one or more reasons:

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

Refer to future handling as:

`STRONGER_MODEL_REQUIRED`

Do not name a specific model.

---

## Phase E — Final Step 1 Verification

After completing all safe work:

* build relevant areas
* run relevant tests
* inspect changed files
* ensure no unrelated modifications occurred
* confirm completed task statuses
* confirm unresolved tasks are clearly isolated

---

# step1Exc_output.md Format

Create:

`<step1Execution.md directory>/temp/step1Exc_output.md`

using the structure below.

---

# Step 1 Execution Output

## 1. Task

Briefly state the original requested outcome.

---

## 2. Execution Summary

**Status:** `COMPLETE | PARTIAL | BLOCKED`

**Completed Tasks:** `X`

**Blocked Tasks:** `X`

**Failed Tasks:** `X`

**Tasks Requiring Stronger Model:** `X`

**Build:** `PASS | FAIL | NOT_RUN | PARTIAL`

**Tests:** `PASS | FAIL | NOT_RUN | PARTIAL`

Brief summary:

...

---

## 3. Inputs Used

* `<step1Execution.md directory>/temp/Plan_Output.md`
* `<step1Execution.md directory>/temp/Discovery_Output.md`
* `CurrentState.md`
* `AGENTS.md`, if applicable

---

## 4. Baseline

### Working State Before Changes

...

### Pre-Existing Failures

* ...

If none known:

`No relevant pre-existing failures identified.`

---

# 5. Task Results

Use exact task IDs from Plan_Output.md.

## T1 — Task Name

**Status:** `DONE | DONE_WITH_NOTES | BLOCKED | FAILED | NOT_STARTED | SKIPPED`

### Changes Performed

* ...

### Files Changed

* `path/to/file`

### Verification

**Check/Command:**

...

**Result:** `PASS | FAIL | NOT_RUN`

### Notes

...

---

Repeat for all planned tasks.

Even tasks not started should appear.

---

# 6. Files Changed

## Created

* ...

## Modified

* ...

## Deleted

* ...

Omit empty categories.

---

# 7. Verification Results

## Build

**Command / Check**

...

**Result:** `PASS | FAIL | PARTIAL | NOT_RUN`

**Relevant Output**

Only include actionable excerpts.

---

## Tests

### Tests Run

* ...

### Result

`PASS | FAIL | PARTIAL | NOT_RUN`

### Failures

* ...

---

# 8. Completed Plan Coverage

List tasks successfully completed.

* `T1`
* `T2`

These tasks should not be reimplemented by the next phase unless later evidence proves them invalid.

---

# 9. Tasks Requiring Stronger Model

This section is mandatory whenever unresolved tasks require deeper reasoning.

---

## Stronger Task 1

### Related Plan Task

`T#`

### Status

`STRONGER_MODEL_REQUIRED`

### Category

One or more of:

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

### Objective

State exactly what remains to be completed.

### Why Step 1 Stopped

...

### Relevant Files

* `path/to/file`

### Relevant Symbols

* ...

### Current Observed Behavior

...

### Expected Behavior From Plan

...

### Exact Blocker

...

### Error / Evidence

...

### Attempts Already Made

* ...
* ...

### Decisions Needed

* ...

### Constraints That Must Be Preserved

* ...

### Completed Work That Must Not Be Repeated

* `T1`
* `T2`

### Minimum Recommended Context

The stronger model should read:

* this section
* relevant Plan_Output.md task
* required repository files
* relevant CurrentState.md section when needed

Avoid requiring repository-wide rediscovery.

---

Repeat for every unresolved task.

---

# 10. Dependency Impact of Blocked Work

Explain blocked dependencies.

Example:

`T4 depends on T3 and was therefore not started.`

---

# 11. Deviations From Plan

Record actual deviations only.

If none:

`No intentional deviations from Plan_Output.md.`

---

# 12. Plan Assumptions Proven False

Record assumptions disproved during implementation.

If none:

`No material plan assumptions were disproved.`

---

# 13. CurrentState.md Drift Found During Execution

Only record relevant new discrepancies.

If none:

`No additional relevant CurrentState.md drift identified.`

Do not update CurrentState.md here.

---

# 14. Incidental Findings

Record relevant out-of-scope issues.

If none:

`None.`

Do not fix them.

---

# 15. Remaining Work

## Safe Remaining Work

Tasks that could still be executed at the same level if resumed:

* ...

## Stronger-Model Work

* `T#`

## Dependent Work

* `T#`

---

# 16. Stronger-Model Handoff Summary

Provide a concise 5-15 line summary.

Include:

* unresolved task IDs
* why they remain
* evidence causing escalation
* completed work
* work that must not be repeated
* what the stronger model must decide or solve

---

# 17. Final Step 1 Repository State

Summarize:

* completed behavior
* incomplete behavior
* build status
* test status
* whether unresolved work is isolated
* whether the repository remains usable

---

# Final Chat Response

After creating:

`<step1Execution.md directory>/temp/step1Exc_output.md`

respond briefly using:

**Step 1 Execution:** `COMPLETE | PARTIAL | BLOCKED`
**Completed:** `X/Y tasks`
**Build:** `PASS | FAIL | NOT_RUN | PARTIAL`
**Tests:** `PASS | FAIL | NOT_RUN | PARTIAL`
**Stronger-model tasks:** `X`
**Output:** `<relative path to temp/step1Exc_output.md>`

Then mention unresolved task IDs, if any.

Do not reproduce the output file in chat.

---

# Final Quality Check

Before finishing, verify:

* sibling `temp` directory exists
* `temp/Plan_Output.md` was used
* `temp/Discovery_Output.md` was used when needed
* `temp/step1Exc_output.md` exists
* task IDs were preserved
* completed tasks are clearly identified
* unresolved tasks are isolated
* architecture was not independently redesigned
* stronger-model tasks contain sufficient evidence
* completed work is protected from unnecessary repetition
* relevant builds/tests were run
* unrelated code was not changed
* blockers contain exact evidence
* dependency effects are clear
* CurrentState.md was not updated
* no hidden reasoning appears in the output
