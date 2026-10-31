import type { ApplicationType } from '../../types/ApplicationType'
import { formatCurrency } from '../../utils/formatters'

interface ApplicationTypeRowProps {
  item: ApplicationType
  isEditing: boolean
  editForm: {
    applicationTypeTitle: string
    applicationTypeFees: string
  }
  saving: boolean
  onStartEdit: (item: ApplicationType) => void
  onFormChange: (form: { applicationTypeTitle: string; applicationTypeFees: string }) => void
  onSave: () => void
  onCancel: () => void
}

export default function ApplicationTypeRow({
  item,
  isEditing,
  editForm,
  saving,
  onStartEdit,
  onFormChange,
  onSave,
  onCancel,
}: ApplicationTypeRowProps) {
  return (
    <tr className="align-top">
      <td className="col-compact">
        <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
          #{item.applicationTypeId}
        </span>
      </td>

      <td className="font-semibold text-[#f1f5f9]">
        {isEditing ? (
          <input
            className="search-input"
            value={editForm.applicationTypeTitle}
            maxLength={100}
            aria-label="Application type name"
            onChange={(event) =>
              onFormChange({
                ...editForm,
                applicationTypeTitle: event.target.value,
              })
            }
          />
        ) : (
          item.applicationTypeTitle
        )}
      </td>

      <td className="font-mono text-[#cbd5e1]">
        {isEditing ? (
          <input
            className="search-input"
            type="number"
            min="0"
            step="0.01"
            value={editForm.applicationTypeFees}
            aria-label="Application type fees"
            onChange={(event) =>
              onFormChange({
                ...editForm,
                applicationTypeFees: event.target.value,
              })
            }
          />
        ) : (
          formatCurrency(item.applicationTypeFees, '')
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
