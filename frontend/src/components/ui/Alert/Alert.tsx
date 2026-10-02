import type { ComponentProps } from 'react'
import './Alert.css'

type AlertProps = ComponentProps<'div'> & {
  variant?: 'success' | 'error' | 'info'
}

export function Alert({ variant = 'info', children, ...rest }: AlertProps) {
  const role = variant === 'error' ? 'alert' : 'status'

  return (
    <div role={role} className={`alert alert--${variant}`} {...rest}>
      {children}
    </div>
  )
}
