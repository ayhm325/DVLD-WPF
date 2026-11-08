# DVLD — Licensing Domain Business Invariants Specification

> **System Name:** Driver & Vehicle Licensing Department (DVLD) Management System  
> **Repository Root:** `D:\DVLD-WPF`  
> **Document Role:** Authoritative Domain Business Invariants & Civic Workflow Constraints  
> **Source Origin:** Extracted from `DEVELOPMENT-PLAN.md` §2 (preserved in [`docs/archive/historical-planning/DEVELOPMENT-PLAN.md`](../archive/historical-planning/DEVELOPMENT-PLAN.md))  
> **Governance Authority:** [`AGENTS.md`](../../AGENTS.md) | [`ENGINEERING-STANDARDS.md`](../../ENGINEERING-STANDARDS.md)  
> **Status:** ACTIVE & ENFORCED  
> **Effective Date:** 2026-11-08  

---

## 1. Domain Governance & Scope

This specification contains the authoritative, non-negotiable civic licensing business rules and domain invariants for the DVLD platform. These domain constraints govern all application services, domain entities, validators, and database transactions in `Application/` and `Domain/`.

*These domain invariants were extracted and validated from historical business discovery in [`docs/audits/phase-a-supplementary-review.md`](../audits/phase-a-supplementary-review.md) §3.*

---

## 2. Core Licensing Domain Business Constraints

### 2.1 Local Driving License (LDL) Application Lifecycle
1. **Prerequisite Citizen Demographic Profile:** An applicant must exist in the `People` directory before an application can be created.
2. **Class-Specific Minimum Age:** An applicant must satisfy the minimum legal age for the target license class (e.g., 18 years for Class 3 Ordinary Driving License, 21 years for Heavy Transport / Buses).
3. **Application State Machine:**
   - **`New` (Status = 1):** Application created and unpaid/in-progress.
   - **`Cancelled` (Status = 2):** Explicitly cancelled by staff. Cancelled applications cannot be reopened, scheduled for tests, or completed.
   - **`Completed` (Status = 3):** Final license issued. Completed applications are locked and immutable.
4. **Active Application Constraint (Domain Rule):** An applicant cannot have more than one active (`New`) local driving license application for the **same license class** simultaneously. (Observed in application validation logic; not the subject of ADR-O02).

### 2.2 3-Tier Sequential Test Appointment System
Examinations must strictly follow this unalterable linear progression:
$$\text{Vision Test (Type 1)} \longrightarrow \text{Written Test (Type 2)} \longrightarrow \text{Street/Practical Test (Type 3)}$$

1. **Strict Prerequisite Enforcement:**
   - To schedule a Written Test appointment, the applicant **must have passed** the Vision Test.
   - To schedule a Practical Test appointment, the applicant **must have passed** the Written Test.
2. **Appointment Lockout & Concurrency:**
   - An applicant can only have one active (`Unlocked`) appointment per test type at any given time.
   - Once a test is recorded (`Pass` or `Fail`), the appointment becomes `Locked` and immutable.
3. **Retake Test Rules:**
   - If an applicant fails a test, they must schedule a **Retake Test Appointment**.
   - Retake test appointments incur both the standard test fee and a retake application fee.
   - Retake scheduling is blocked until the previous test result is officially locked and recorded as a failure.

### 2.3 First-Time Driver License Issuance
1. **Completion Prerequisite:** All three examinations (Vision, Written, Practical) must be passed.
2. **Driver Registration:** If the applicant is not already registered as a driver, a new `Driver` record is created linking their `PersonID`.
3. **License Generation:** A `License` record is created with:
   - Issue Date = Current Jordan Local Date.
   - Expiration Date = Current Date + `DefaultValidityLength` years (from `LicenseClasses`).
   - `IsActive` = `true`.
   - `IssueReason` = `1` (First Time).
4. **Application Completion:** The parent `Application` status is updated to `Completed` (`3`) within the same transaction.

### 2.4 Post-Issuance Workflows
1. **License Renewal:**
   - Allowed only if the current license is expired or within the authorized renewal window.
   - The old license is deactivated (`IsActive = false`), and a new license record is generated linked to the same driver with `IssueReason = 2` (Renew).
2. **License Replacement (Damaged or Lost):**
   - Requires an active license (`IsActive = true`).
   - If replaced as Damaged (`IssueReason = 3`), standard damage fee applies.
   - If replaced as Lost (`IssueReason = 4`), replacement fee applies.
   - The old license is deactivated immediately upon replacement.
3. **License Detention & Release:**
   - An active license may be detained with a recorded fine amount.
   - A detained license cannot be renewed or replaced while actively detained.
   - **Release Workflow:** Requires payment of the fine application fee and fine amount; updates `IsReleased = true`, `ReleaseDate = UTC Now`, and `ReleaseApplicationID`.
