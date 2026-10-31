import type { TestType } from '../../types/TestType'
import { formatCurrency } from '../../utils/formatters'

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
    <tr className="align-top">
      <td className="col-compact pt-3.5">
        <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
          #{item.testTypeId}
        </span>
      </td>

      <td className="whitespace-nowrap font-semibold text-[#f1f5f9] pt-3.5">
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

      <td className="table-cell-description pt-3.5">
        {isEditing ? (
          <textarea
            className="search-input w-full min-h-[72px] resize-y leading-relaxed p-2 text-xs"
            rows={3}
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
          <p className="m-0 leading-relaxed text-[#c0cddf] text-xs whitespace-normal break-words">
            {item.testTypeDescription}
          </p>
        )}
      </td>

      <td className="whitespace-nowrap font-mono font-semibold text-[#cbd5e1] pt-3.5">
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
          formatCurrency(item.testTypeFees, '')
        )}
      </td>

      <td className="whitespace-nowrap pt-3">
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
