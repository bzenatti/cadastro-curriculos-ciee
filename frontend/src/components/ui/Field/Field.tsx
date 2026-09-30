import { useId } from 'react'
import type { ComponentProps } from 'react'
import './Field.css'

type FieldBaseProps = {
  label: string
  error?: string
}

// `multiline` escolhe o tipo do campo: com ele, as props são as de um <textarea>;
// sem ele, as de um <input>.
type FieldProps =
  | (FieldBaseProps & ComponentProps<'input'> & { multiline?: false })
  | (FieldBaseProps & ComponentProps<'textarea'> & { multiline: true })

export function Field({ label, error, id, multiline, ...rest }: FieldProps) {
  const generatedId = useId()
  const controlId = id ?? generatedId
  const errorId = `${controlId}-error`
  const controlProps = {
    id: controlId,
    'aria-invalid': Boolean(error),
    'aria-describedby': error ? errorId : undefined,
  }

  return (
    <div className="field">
      <label htmlFor={controlId}>{label}</label>
      {multiline ? (
        // o TypeScript não liga o 'rest' ao 'multiline', por isso o 'as'
        <textarea {...controlProps} {...(rest as ComponentProps<'textarea'>)} />
      ) : (
        <input {...controlProps} {...(rest as ComponentProps<'input'>)} />
      )}
      {error && (
        <span id={errorId} role="alert" className="field_error">
          {error}
        </span>
      )}
    </div>
  )
}
