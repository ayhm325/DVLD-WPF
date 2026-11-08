# DVLD Engineering Session Logs Repository

> **Directory Role:** Historical Register of Engineering Sessions & Autonomous Agent Invocations  
> **Parent Continuity Suite:** [`PROJECT-CHARTER-AND-CONTINUITY.md`](../PROJECT-CHARTER-AND-CONTINUITY.md)  
> **Template Reference:** [`SESSION-LOG.template.md`](../templates/SESSION-LOG.template.md)  
> **Governance Invariant:** Session logs are point-in-time historical records; past logs must never be silently rewritten.  

---

## 1. Purpose of This Directory

This directory stores standardized, permanent records of engineering sessions conducted across human engineers and AI coding assistants within the DVLD repository. 

Session logs provide:
1. **Context Continuity:** Seamless handover between development sessions and distinct AI context windows.
2. **Evidence Auditing:** Clear traceability of tool executions, code inspections, and verification outputs.
3. **Decision Lineage:** Explicit recording of architectural recommendations, human approvals, and rejected options.
4. **Safety & Permission Accountability:** Documented compliance with Git, database, and file-access authorization boundaries.

---

## 2. Session Classification: When is a Log Required?

Not every interaction warrants a permanent file in this directory. Sessions are classified into two categories:

### 2.1 Significant Engineering Sessions (LOG REQUIRED)
An independent session log **must** be created for any session that:
- Modifies or creates repository files (source code, tests, documentation, or configurations).
- Conducts an architectural, security, performance, or database investigation.
- Diagnoses a bug, defect, or test failure using static or runtime evidence.
- Evaluates or formulates architectural decisions (ADRs) or roadmap work items.
- Executes automated test suites, database migrations, or build workflows.
- **Zero-Change Significant Sessions:** If a session performs extensive analysis, code auditing, or planning, it **requires a session log even if no files were modified, no tests were run, or no final decision was reached.**

### 2.2 Minor Learning & Discussion Sessions (NO FILE REQUIRED)
Sessions that do not materially impact project continuity do **not** require an independent file in this directory:
- Quick conversational clarifications or general programming questions.
- High-level explanations of existing documentation or known architecture concepts.
- Reviewing past session logs without proposing new changes or discovering new defects.

---

## 3. Session Identification & Naming Conventions

### 3.1 Session ID Format
All session logs in this directory must strictly follow the canonical naming convention:

```text
YYYY-MM-DD-SESSION-NNN.md
```

- `YYYY-MM-DD`: The calendar date of the session in the explicitly recorded session timezone. Use the local calendar date corresponding to the session start timestamp; record the exact timezone offset in the timestamp.
- `SESSION-NNN`: A 3-digit zero-padded sequential index for that date (e.g., `2026-11-08-SESSION-001.md`, `2026-11-08-SESSION-002.md`).

> [!IMPORTANT]
> **Collision Prevention:** Before creating a new session log, the engineer or AI assistant must inspect existing logs in this directory to identify the next available sequence number for that date.

### 3.2 Standard Timestamps
All timestamps within session logs must use **ISO 8601 formatting with explicit timezone offsets**:
```text
YYYY-MM-DDTHH:MM:SS+OFFSET (e.g., 2026-11-08T15:15:00+03:00)
```
Ambiguous relative time references ("today", "earlier", "recently") are strictly prohibited.

---

## 4. Operational Governance & Integrity Rules

### 4.1 Historical Immutability (No Silent Rewrites)
- Session logs represent factual snapshots of what occurred during that specific session.
- Once a session log is persisted in the repository, it must not be silently rewritten or deleted. This is a governance convention, not a claim of technical immutability.
- If a subsequent turn reveals that a session log contained factual errors, an inaccurate test claim, or an unverified assumption, the correction **must be formally logged in [`CORRECTION-LOG.md`](../CORRECTION-LOG.md)** and cross-referenced.

### 4.2 Documentation Obligation vs. Repository Write Authorization
- A fundamental governance rule applies: **The obligation to record a session does NOT grant authorization to write to the repository filesystem.**
- If write authorization has not been explicitly granted for `docs/continuity/SESSION-LOGS/`, the AI assistant **must prepare the full session log directly inside the conversation output**.
- In-conversation logs must carry `Record Status: PREPARED IN CONVERSATION` and state that repository persistence remains pending human authorization.
- Failure to obtain repository write authorization must **never** result in omitting the session record.

### 4.3 Redaction and Secret Exclusion
Session logs must **NEVER** contain sensitive credentials, secrets, or PII:
- No passwords, API keys, or raw JWT/refresh tokens.
- No database connection strings containing credentials.
- No personal citizen identification data or photos.

### 4.4 Git History and Record Integrity
While Git history tracks additions and commits to this directory, Git history alone does not guarantee technical immutability against force-push or branch rebasing. Discipline in never modifying past session files is the primary mechanism of continuity integrity.

---

## 5. Authoring a New Session Log

When authoring a new session record:
1. Copy the standardized template: [`SESSION-LOG.template.md`](../templates/SESSION-LOG.template.md).
2. Save it as `docs/continuity/SESSION-LOGS/YYYY-MM-DD-SESSION-NNN.md` (if write authorization is granted).
3. Complete all required fields accurately, using explicit confidence labels (`VERIFIED`, `REPORTED`, `INFERRED`, `PENDING`).
4. Ensure all modified files and executed commands are exhaustively documented.
