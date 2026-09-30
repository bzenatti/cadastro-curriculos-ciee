import { useId } from 'react'
import type { ComponentProps } from 'react'
import './Field.css'

type FieldProps = ComponentProps<'input'> & {
  label: string
  error?: string
}

export function Field({ label, error, id, ...rest }: FieldProps) {
  const generatedId = useId()
  const inputId = id ?? generatedId
  const errorId = `${inputId}-error`

  return (
    <div className="field">
      <label htmlFor={inputId}>{label}</label>
      <input
        id={inputId}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        {...rest}
      />
      {error && (
        <span id={errorId} role="alert" className="field_error">
          {error}
        </span>
      )}
    </div>
  )
}
