import type { ComponentProps } from 'react'
import './Button.css'

type ButtonProps = ComponentProps<'button'> & {
  variant?: 'primary' | 'secondary'
  loading?: boolean
}

export function Button({ variant = 'primary', type = 'button', loading, disabled, children, ...rest }: ButtonProps) {
  return (
    <button type={type} className={`btn btn--${variant}`} disabled={loading || disabled} {...rest}>
      {loading ? 'Enviando' : children}
    </button>
  )
}
