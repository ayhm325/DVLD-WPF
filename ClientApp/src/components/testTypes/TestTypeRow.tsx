import type { TestType } from '../../services/testTypeService'

interface TestTypeRowProps {
  item: TestType
  isEditing: boolean
  editForm: {
    testTypeTitle: string
    testTypeDescription: string
    testTypeFees: string
  }
  saving: boolean
  onStartEdit: (item: TestType) => void
  onFormChange: (form: {
    testTypeTitle: string
    testTypeDescription: string
    testTypeFees: string
  }) => void
  onSave: () => void
  onCancel: () => void
}

export default function TestTypeRow({
  item,
  isEditing,
  editForm,
  saving,
  onStartEdit,
  onFormChange,
  onSave,
  onCancel,
}: TestTypeRowProps) {
  return (
    <tr>
      <td>
        <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
          #{item.testTypeId}
        </span>
      </td>

      <td>
        {isEditing ? (
          <input
            className="search-input"
            value={editForm.testTypeTitle}
            maxLength={100}
            aria-label="Test type title"
            onChange={(event) =>
              onFormChange({
                ...editForm,
                testTypeTitle: event.target.value,
              })
            }
          />
        ) : (
          item.testTypeTitle
        )}
      </td>

      <td>
        {isEditing ? (
          <input
            className="search-input"
            value={editForm.testTypeDescription}
            maxLength={500}
            aria-label="Test type description"
            onChange={(event) =>
              onFormChange({
                ...editForm,
                testTypeDescription: event.target.value,
              })
            }
          />
        ) : (
          item.testTypeDescription
        )}
      </td>

      <td>
        {isEditing ? (
          <input
            className="search-input"
            type="number"
            min="0"
            step="0.01"
            value={editForm.testTypeFees}
            aria-label="Test type fees"
            onChange={(event) =>
              onFormChange({
                ...editForm,
                testTypeFees: event.target.value,
              })
            }
          />
        ) : (
          item.testTypeFees.toFixed(2)
        )}
      </td>

      <td>
        {isEditing ? (
          <div style={{ display: 'flex', gap: 8 }}>
            <button
              type="button"
              className="primary-button"
              disabled={saving}
              onClick={onSave}
            >
              {saving ? 'Saving...' : 'Save'}
            </button>

            <button
              type="button"
              className="secondary-button"
              disabled={saving}
              onClick={onCancel}
            >
              Cancel
            </button>
          </div>
        ) : (
          <button
            type="button"
            className="secondary-button"
            onClick={() => onStartEdit(item)}
          >
            Edit
          </button>
        )}
      </td>
    </tr>
  )
}
