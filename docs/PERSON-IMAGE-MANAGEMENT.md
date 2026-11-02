# DVLD Person Photograph Management Architecture & Operations

## 1. Architecture Overview

The person photograph management system provides end-to-end, secure, and production-ready image handling across the entire DVLD application stack:

```
┌─────────────────┐       FormData (Multipart)       ┌──────────────────────┐
│  React 19 Client │ ───────────────────────────────> │ PeopleController.cs  │
│  (PersonPhoto)  │ <─────────────────────────────── │ (ASP.NET Core 8 API) │
└─────────────────┘       Blob (Authenticated)       └──────────┬───────────┘
                                                                │
                                    ┌───────────────────────────┴───────────────────────────┐
                                    ▼                                                       ▼
                    ┌───────────────────────────────┐                       ┌───────────────────────────────┐
                    │     ImageProcessingService    │                       │         PersonService         │
                    │   (SkiaSharp 4.154.0 - MIT)   │                       │    (Application Layer)        │
                    └───────────────┬───────────────┘                       └───────────────┬───────────────┘
                                    │ WebP Stream                                           │
                                    ▼                                                       ▼
                    ┌───────────────────────────────┐                       ┌───────────────────────────────┐
                    │   PhysicalFileStorageService  │                       │   SQL Server & EF Core        │
                    │  (IFileStorageService Abstr.) │                       │   People.ImagePath (NVARCHAR) │
                    └───────────────┬───────────────┘                       └───────────────────────────────┘
                                    ▼
                    ┌───────────────────────────────┐
                    │ Private Storage Directory     │
                    │ (Outside wwwroot, e.g. .webp) │
                    └───────────────────────────────┘
```

### Key Architectural Tenets:
1. **Storage Provider Independence:** Application workflows depend strictly on [`IFileStorageService`](file:///d:/DVLD-WPF/Application/Interfaces/IFileStorageService.cs). No physical disk paths or filesystem APIs are used inside controllers or domain entities.
2. **Private & Authenticated Access:** Photograph files reside strictly outside `wwwroot`. There is no public static file hosting. All image streaming occurs via authenticated HTTP GET with JWT validation and private caching headers (`Cache-Control: private, max-age=86400`).
3. **Normalized Re-Encoding:** Uploaded images are sanitized, oriented, resized to portrait proportions ($\le 600 \times 800$ px), stripped of EXIF metadata, and re-encoded to WebP format.

---

## 2. Configuration Keys

The storage subsystem is configured via the standard .NET options pattern:

| Configuration Path | Environment Variable | Default (Dev) | Description |
| :--- | :--- | :--- | :--- |
| `Storage:PersonImagesPath` | `Storage__PersonImagesPath` | `../DVLD_Storage/People` | Physical directory where processed person photos are stored. Supports relative and rooted paths. |

### Development Setup (`appsettings.Development.json`):
```json
{
  "Storage": {
    "PersonImagesPath": "../DVLD_Storage/People"
  }
}
```

### Production Setup:
In production, set the environment variable or configure `appsettings.Production.json` to an absolute, isolated path outside the IIS/Kestrel publish directory:
```bash
Storage__PersonImagesPath="D:\DVLD_Data\Storage\People"
```
Or in Linux/Docker containers:
```bash
Storage__PersonImagesPath="/var/dvld/storage/people"
```

---

## 3. Required Filesystem Permissions

For the configured `PersonImagesPath` directory:
- **Windows (IIS / Kestrel):**
  - The application pool identity (e.g. `IIS AppPool\DVLD-Api` or `NETWORK SERVICE` / `LocalSystem`) requires:
    - **Read & Execute**
    - **List Folder Contents**
    - **Read**
    - **Write**
    - **Modify** (for deleting obsolete photos)
  - Deny access to anonymous web users (`IUSR`).
- **Linux / Docker:**
  - `chmod 750 /var/dvld/storage/people`
  - `chown dvldapp:dvldgroup /var/dvld/storage/people`

---

## 4. API Contracts

All endpoints are protected under the `StaffOrAdmin` policy and require a valid Bearer token.

### 4.1. Upload or Replace Photograph
- **Endpoint:** `POST /api/People/{id}/photo`
- **Content-Type:** `multipart/form-data`
- **Form Key:** `file` (single image binary)
- **Response (200 OK):**
  ```json
  {
    "personId": 12,
    "imagePath": "people/7a9b8c0d1e2f3a4b5c6d7e8f9a0b1c2d.webp"
  }
  ```
- **Error Responses:**
  - `400 Bad Request`: Missing file, file exceeds 5MB, corrupt bytes, non-image format, or decompression bomb.
  - `401 Unauthorized`: Anonymous or missing token.
  - `403 Forbidden`: User role lacks Staff/Admin permissions.
  - `404 Not Found`: Person ID does not exist.

### 4.2. Retrieve Photograph
- **Endpoint:** `GET /api/People/{id}/photo`
- **Headers:** `Authorization: Bearer <token>`
- **Response (200 OK):**
  - **Headers:** `Content-Type: image/webp`, `Cache-Control: private, max-age=86400, must-revalidate`
  - **Body:** Binary WebP image stream.
- **Error Responses:**
  - `401 Unauthorized`: Unauthenticated request.
  - `404 Not Found`: Person not found, or person does not have a photo, or file is missing on storage.

### 4.3. Remove Photograph
- **Endpoint:** `DELETE /api/People/{id}/photo`
- **Response (204 NoContent):** Successfully cleared database reference and deleted physical file.
- **Error Responses:**
  - `401 Unauthorized` / `403 Forbidden`
  - `404 Not Found`: Person does not exist.

---

## 5. Accepted Formats and Safety Limits

- **Allowed Formats:** JPEG, PNG, WebP.
- **Explicitly Rejected:** SVG, XML, HTML, GIF, BMP, Executables (MZ, ELF), scripts, unknown binary formats.
- **Magic-Byte Signature Verification:** Pre-validates file header before attempting decoding.
- **Maximum Upload Size:** 5 MB (`5 * 1024 * 1024` bytes).
- **Dimension Safety Limit:** Max $4096 \times 4096$ pixels, max 16,000,000 total pixels (prevents decompression bombs).
- **Normalized Output Dimensions:** Resized while preserving aspect ratio to fit inside $600 \times 800$ portrait dimensions.
- **Normalized Encoding:** Re-encoded into WebP with 85% quality via SkiaSharp 4.154.0.

---

## 6. Database Reference & Data Consistency

- **Database Column:** `People.ImagePath NVARCHAR(500) NULL` in SQL Server.
- **Stored Value:** Normalized relative storage key, e.g. `people/8b5c4d3e2a104f6789abcde012345678.webp`.
- **Reference Preservation:**
  - Regular updates to person details (name, national ID, address, phone) via `PUT /api/People/{id}` **never overwrite or erase** an existing `ImagePath`.
  - Photo removal occurs exclusively through `DELETE /api/People/{id}/photo` or explicit file replacement.

---

## 7. Failure Safety, Cleanup, and Recovery

File storage and SQL Server do not share a distributed ACID transaction. Consistency is enforced through deliberate step ordering:

1. **Upload / Replacement:**
   - The new image is processed and written to disk under a new random key before modifying the database.
   - If SQL Server persistence fails, the newly saved file is deleted as a compensating action, preserving the previous photo.
   - The old photo is deleted from disk only after the database transaction commits successfully.
2. **Photo Removal:**
   - The database reference is cleared and committed first.
   - The file is deleted from disk only after the commit succeeds. Missing files are handled idempotently.
3. **Orphaned File Audit & Recovery:**
   - If an unexpected server crash occurs during deletion, an orphaned `.webp` file may remain on disk.
   - Because all files are named `{Guid:N}.webp`, a routine maintenance task can scan the storage folder and delete any file whose storage key does not exist in `SELECT ImagePath FROM People WHERE ImagePath IS NOT NULL`.

---

## 8. Backup and Restore Considerations

- **Backup Order:**
  1. Perform a consistent snapshot or filesystem backup of `Storage:PersonImagesPath`.
  2. Perform a full backup of SQL Server database `DVLDf`.
- **Restore Order:**
  1. Restore the SQL Server database.
  2. Restore the filesystem directory to the location configured in `Storage:PersonImagesPath`.
  3. Ensure application pool identity has read/write permissions on the restored directory.

---

## 9. Future Migration to Shared or Cloud Object Storage

Because `PersonService` and `PeopleController` communicate solely with `IFileStorageService`:
- To switch to **Azure Blob Storage** or **Amazon S3**:
  1. Implement `AzureBlobStorageService : IFileStorageService` or `S3StorageService : IFileStorageService`.
  2. Update DI registration in `Program.cs`.
  3. No changes are required to `PersonService`, `Person`, database schema, or frontend components.
