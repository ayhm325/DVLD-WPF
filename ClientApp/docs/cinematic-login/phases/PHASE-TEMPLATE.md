# DVLD Cinematic Login — Phase [XX]: [Phase Title]

> **Instructions for the User / AI Agent:** Use this standardized template to author and execute future phases of the DVLD Cinematic Login project. Every field is mandatory.

---

## 1. Mission
[Provide a clear, 2-3 paragraph executive summary of the phase's core objective and its role within the master 11-phase roadmap.]

---

## 2. Reference and Documentation to Read
Before modifying any files, inspect the following documents in order:
1. `ClientApp/AGENTS.md`
2. `ClientApp/docs/cinematic-login/README.md`
3. `ClientApp/docs/cinematic-login/00-PROJECT-CHARTER.md`
4. `ClientApp/docs/cinematic-login/08-CURRENT-STATE.md`
5. `ClientApp/docs/cinematic-login/07-ROADMAP.md`
6. [List phase-specific documentation, e.g., `01-VISUAL-REFERENCE.md`, `03-COMPOSITION-SPEC.md`, `04-TECH-STACK.md`, `06-ASSET-REGISTER.md`]
7. Visual Reference Target: [Specify path or image identifier]

---

## 3. Current Verified State
- **Last Approved Phase:** [e.g., NONE / Phase 00.1 — Documentation Consistency & Governance Correction]
- **Current Phase:** Phase [XX] — [Phase Title]
- **Current Phase Lifecycle Status:** [e.g., IN PROGRESS]
- **Working Tree Baseline:** [Summary of verified baseline files and pre-existing modifications]
- **Baseline Build & Lint:** Must be verified prior to editing.

---

## 4. Objective
[Bulleted list of the concrete, verifiable outcomes this phase must achieve.]

---

## 5. Scope
### Authorized Work
- [Explicit itemized list of what the agent IS permitted to create, modify, or analyze.]

### Explicitly Prohibited Changes
- **DO NOT** modify authentication logic, API clients, or backend services.
- **DO NOT** modify unrelated application pages or WPF code.
- **DO NOT** install or uninstall packages without an approved ADR.
- **DO NOT** execute git commit, git push, or destructive git reset operations.
- **DO NOT** proceed beyond the authorized deliverables of this phase.

---

## 6. Required Tools and Technologies
- **Rendering Stack:** [e.g., Three.js r187, React Three Fiber v9, Drei v10]
- **Styling / UI:** [e.g., Tailwind CSS v4, HTML/CSS, Lucide React]
- **Asset Tools:** [e.g., Node.js Three.js GLTFExporter, Canvas PBR generators]
- **New Dependencies:** [NONE / Specify ADR reference if approved]

---

## 7. Step-by-Step Execution Plan
1. **Step 1 — Pre-flight Audit:** Inspect working tree and confirm baseline build.
2. **Step 2 — Implementation:** [Detailed technical steps].
3. **Step 3 — Technical Validation:** Run linter, compiler, and build checks.
4. **Step 4 — Visual / Quality Gate Assessment:** Evaluate against `10-QUALITY-GATES.md`.
5. **Step 5 — Documentation Update:** Synchronize `08-CURRENT-STATE.md`, `07-ROADMAP.md`, etc.
6. **Step 6 — Final Report & Stop:** Cease tool calls and request approval.

---

## 8. Expected Deliverables
- [Deliverable 1: e.g., Updated GLB model / UI component / specification file]
- [Deliverable 2: e.g., Rendered multi-resolution screenshots]
- [Deliverable 3: e.g., Synchronized documentation files]

---

## 9. Files Expected to Change
### Files to Create
- `[Path/to/new/file]`

### Files to Modify
- `[Path/to/existing/file]`

### Files Strictly Preserved
- `[Path/to/protected/file]`

---

## 10. Acceptance Criteria
- [ ] Matches visual requirements defined in `01-VISUAL-REFERENCE.md`.
- [ ] Conforms to spatial layout in `03-COMPOSITION-SPEC.md`.
- [ ] `npm.cmd --prefix ClientApp run build` passes with 0 errors.
- [ ] `npm.cmd --prefix ClientApp run lint` passes with 0 errors.
- [ ] WebGL error boundary and SVG fallback remain fully functional.
- [ ] Zero unapproved Git operations.

---

## 11. Validation Commands
```bash
# Baseline TypeScript & Production Build Check
npm.cmd --prefix ClientApp run build

# ESLint Adherence Check
npm.cmd --prefix ClientApp run lint

# Working Tree Status Check
git status --short
```

---

## 12. Visual Evidence Requirements
- [Specify required screenshot resolutions, e.g., 1920x1080, 1366x768, 1280x720]
- [Specify key visual angles or comparison overlays]

---

## 13. Risks and Known Limitations
- [List any performance, browser compatibility, or geometric constraints]

---

## 14. Documentation Updates Required
At the end of this phase, the following documents **must** be updated:
- `ClientApp/docs/cinematic-login/08-CURRENT-STATE.md`
- `ClientApp/docs/cinematic-login/07-ROADMAP.md`
- [Include `06-ASSET-REGISTER.md` or `09-DECISIONS.md` if applicable]

---

## 15. Stop Conditions
- **STOP** immediately once all phase deliverables are complete and validated.
- **DO NOT** begin the subsequent phase.
- **AWAIT** explicit written user review and approval.

---

## 16. Final Report Format
Deliver the final report using the mandatory 8-section structure:
- **A. Phase Result**
- **B. Documentation / Assets Created & Modified**
- **C. Repository Audit & Baseline Check**
- **D. Project Risks and Unknowns**
- **E. Safety Verification**
- **F. Git Status**
- **G. Next Phase Recommendation**
- **H. Approval Request**
