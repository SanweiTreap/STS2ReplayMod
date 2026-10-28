# SPIKE-INV006A — scope-safe outer-cloud timing gate

Status: `COMPLETE — PASS_SCOPE_SAFE_GATE`

## Outcome

The single sponsor-operated public-beta v0.111.0 main-menu run reached `Gate Ready`; the sponsor clicked `Verify Stable Gate` exactly once and reported the final UI classification `PASS_SCOPE_SAFE_GATE`. Independent inspection of the closed runtime artifact confirms the same classification and every required timing, identity, stability, schema, and safety condition.

This result validates only the scope-safe pre-override timing gate. The probe did not replace a manager, inspect a save-store call, read a logical save name or data argument, construct a candidate, call progress/save, start a run, transition a scene, or exercise replay/direct-write behavior.

Runtime artifact: `evidence/spike-inv006a/runtime-artifact.json`, 3,554 bytes, SHA-256 `7F45652CC5AB7524CB3444B99FA8AF8AF5EFF0E47EB433A3306C2FAE44B52823`.

## Scope and fixed identity

This was a wholly new probe under ID `SPIKEINV006A`, source directory `experiments/SPIKE-INV006A/`, evidence directory `evidence/spike-inv006a/`, and schema `spike-inv006a.timing.v1`. The retired SPIKE-INV005A runtime artifact was not opened, processed, copied, or used as a fixture. Implementation Codex did not start or control the game and used no GUI automation.

The artifact records the fixed public-beta identity `v0.111.0` / `41cef1ea`, with `sts2.dll` length 9,757,184 and SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`. It also records the installed probe DLL identity: 40,960 bytes and SHA-256 `5CC7505594F7E669327E333A980548FE02C0DEE6D04FB5F2FC38F97D43230BC1`. Post-run inspection confirmed that the installed DLL still matched that identity.

## Confirmed

### Runtime event order and terminal state

The closed event set contains exactly ten consecutive events with strictly increasing monotonic ticks:

1. `PatchActive`
2. `OuterCloudReturned` — `Pending`
3. `GraphicsEntry`
4. `GraphicsNormalExit`
5. `OuterCloudTerminal` — `RanToCompletion`
6. `MenuReady`
7. `StartupMarkerTerminal` — `RanToCompletion`
8. `GateReady`
9. `Verify`
10. `Terminal`

The terminal record is sequence 10 with classification `PASS_SCOPE_SAFE_GATE` and `firstFailureSite: None`. Patch activation preceded the sole graphics entry. Exactly one outer task was captured; pending return did not open the gate, and the gate opened only after its terminal continuation.

### Gate and Verify stability

The locked Gate and Verify snapshots both record:

- original manager runtime identity `55084617`;
- outer task runtime identity `29941743`;
- outer and startup task status `RanToCompletion`;
- original current-run task status `Null`;
- run state null;
- graphics entry/normal-exit/active counters `1 / 1 / 0`;
- all eleven condition booleans true, including manager identity stability, no terminal failure, and no post-gate lifecycle.

Gate and Verify manager/task identities, statuses, and counters are identical. There was no second cloud or graphics lifecycle and no admitted post-seal event.

### Observer and artifact safety

- The production assembly has only the required zero-argument graphics, outer-cloud, and main-menu patches. It contains no store/leaf/component/path-bearing patch.
- Every patch, continuation, timeout callback, and probe UI callback performs the running/enabled check before observer work and rechecks before commit under the shared lock.
- Task classification never reads `Task.Exception`; the graphics finalizer returns the original exception reference unchanged and records only a fixed site.
- One CAS winner sealed the event set. Only its opaque ticket could access the closed snapshot and fixed output location.
- The artifact passed the exact top-level property whitelist and nested schema checks. A case-insensitive scan found none of the forbidden path, payload, exception, Steam/user, profile, wall-clock, inventory, or out-of-scope terms.
- The evidence directory contains exactly one runtime JSON and no remaining temporary artifact.
- Release build completed with zero warnings/errors. All 23 synthetic tests passed, including all 15 INV-006 negative/race cases, synchronous continuation/seal competition, pending outer rejection, Verify non-reopen, wrong-DLL blocking, and runtime schema whitelist.
- Production source and binary forbidden-surface audits passed. A synthetic wrong DLL identity produced `BLOCKED_IDENTITY`.

## Inferred

- For the fixed v0.111.0/profile1/Cloud-off/one-probe environment, this event conjunction is a usable scope-safe pre-override timing gate: production naturally emitted the required outer task and lifecycle events, and the same conditions survived a later human Verify without broadened observation.
- The result supports using this gate as the precondition for a separately authorized live-seeded experiment. It does not itself validate the candidate that such an experiment would install.

## Unknown

- Save-manager override correctness, live-seeded candidate equivalence, direct progress-write routing, teardown, fault/restart behavior, reconstruction, action replay, and replay fidelity remain untested here.
- This one bounded run does not establish behavior on another game build, profile, Cloud configuration, mod set, or machine.

## Single recommended next action

Project Lead should issue a wholly new live-seeded direct-write sentinel work package that uses the confirmed scope-safe timing gate as its precondition. No direct-write action is authorized under SPIKE-INV006A itself.
