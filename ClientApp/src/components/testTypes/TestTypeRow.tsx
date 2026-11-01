import type { TestType } from '../../types/TestType'
import { formatCurrency } from '../../utils/formatters'
import { Input, Textarea, Button } from '../ui'
import { Loader2 } from 'lucide-react'

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
          <Input
            className="h-9 text-xs rounded-lg min-w-[140px]"
            value={editForm.testTypeTitle}
            maxLength={100}
            aria-label="Test type title"
            disabled={saving}
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
          <Textarea
            className="min-h-[72px] text-xs leading-relaxed p-2.5 rounded-lg resize-y"
            rows={3}
            value={editForm.testTypeDescription}
            maxLength={500}
            aria-label="Test type description"
            disabled={saving}
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
          <Input
            className="h-9 text-xs font-mono rounded-lg w-28"
            type="number"
            min="0"
            step="0.01"
            value={editForm.testTypeFees}
            aria-label="Test type fees"
            disabled={saving}
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
          <div className="flex items-center gap-2">
            <Button
              type="button"
              size="sm"
              disabled={saving}
              onClick={onSave}
            >
              {saving && <Loader2 className="size-3.5 animate-spin" />}
              <span>{saving ? 'Saving...' : 'Save'}</span>
            </Button>

            <Button
              type="button"
              variant="secondary"
              size="sm"
              disabled={saving}
              onClick={onCancel}
            >
              Cancel
            </Button>
          </div>
        ) : (
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => onStartEdit(item)}
          >
            Edit
          </Button>
        )}
      </td>
    </tr>
  )
}
