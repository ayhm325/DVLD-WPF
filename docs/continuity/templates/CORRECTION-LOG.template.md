# DVLD Correction Record — Template

> **Correction ID:** `CORR-YYYY-MM-DD-NNN`  
> **Recorded At:** `YYYY-MM-DDTHH:MM:SS+OFFSET`  
> **Prepared By:** `<Human Engineer or AI Agent Role>`  
> **Correction Status:** `PROPOSED` | `EVIDENCE VERIFIED` | `RECORDED` | `CLOSED`  
> **Decision Approval:** `NOT REQUIRED` | `REQUIRED` | `PENDING`  
> **Parent Continuity Suite:** [`CORRECTION-LOG.md`](../CORRECTION-LOG.md)  

---

## 1. Discrepancy Identification

### 1.1 Original Record
- **Affected Document / File:** `<Path to affected file, e.g. docs/continuity/SESSION-LOGS/2026-11-08-SESSION-001.md>`
- **Specific Section / Heading:** `<Heading or line number range>`
- **Original Statement:**
  > "<Insert exact quotation of the inaccurate, unverified, or superseded statement>"

### 1.2 Discrepancy Classification
<!--
Select the category of error:
- FACTUAL INACCURACY (Code or test state was misstated)
- UNVERIFIED ASSUMPTION (Claim made without empirical evidence)
- DECISION STATUS DRIFT (Status overstated, e.g. Proposal marked as Approved)
- GOVERNANCE INCONSISTENCY (Contradicts approved standard or ADR)
-->
- **Classification:** `[FACTUAL INACCURACY | UNVERIFIED ASSUMPTION | DECISION STATUS DRIFT | GOVERNANCE INCONSISTENCY]`

---

## 2. Evidence-Backed Correction

### 2.1 Corrected Information
<!--
State the exact, verified factual information that replaces the inaccurate claim.
-->
> "<Accurate factual statement backed by direct code, test, or document evidence>"

### 2.2 Supporting Evidence
<!--
Provide specific evidence demonstrating why the original statement was incorrect and why the corrected information is true.
Include file paths, line ranges, commit hashes, or test failure traces.
-->
- **Concrete Evidence:** `<e.g., Static code inspection of UserService.cs lines 45-60 confirms DbUpdateException escapes unhandled>`
- **Reason for Prior Error:** `<Explanation of how the discrepancy originated, e.g. assumption from old audit report>`

---

## 3. Impact Assessment & Decision Approval

### 3.1 Impact Analysis
- **Architecture / Security Impact:** `<Does this affect security boundaries or layering? e.g. None / Critical>`
- **Roadmap / Scope Impact:** `<Does this change backlog priorities or work item scope?>`
- **Safety / Data Impact:** `<Does this affect database protection or deployment rules?>`

### 3.2 Decision Approval Boundary
<!--
Separation rule:
- Correction Status represents the evidence/recording lifecycle.
- Decision Approval represents whether an engineering policy or ADR requires Project Owner sign-off.
-->
- **Policy Decision Required:** `[YES | NO]`
- **Decision Approval:** `NOT REQUIRED` | `REQUIRED` | `PENDING`
- **Approval Reference:** `<Explicit Project Owner approval / ADR reference / N/A>`
- **ADR Reference:** `<Link to ADR in ENGINEERING-DECISIONS.md if approval is required, or N/A>`

---

## 4. Required Follow-up & Resolution

### 4.1 Required Actions
- [ ] Record entry in [`CORRECTION-LOG.md`](../CORRECTION-LOG.md) history table.
- [ ] Add cross-reference in affected document (without silently rewriting historical text).
- [ ] Submit architectural policy question to Project Owner (if `Decision Approval: REQUIRED`).
- [ ] Update roadmap or standards if approved by Project Owner.

### 4.2 Resolution Summary
- **Final Resolution:** `<Summary of the outcome: how the error was reconciled and whether follow-up items are closed or pending>`
- **Closed Date:** `YYYY-MM-DDTHH:MM:SS+OFFSET` (or `PENDING`)
