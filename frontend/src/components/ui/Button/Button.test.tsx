import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Button } from './Button'

describe('Button', () => {
  it('usa type="button" por padrão para não enviar formulários sem querer', () => {
    render(<Button>Limpar</Button>)
    expect(screen.getByRole('button', { name: 'Limpar' })).toHaveAttribute('type', 'button')
  })

  it('em loading fica desabilitado e ignora cliques', async () => {
    const onClick = vi.fn()
    render(<Button loading onClick={onClick}>Salvar</Button>)

    const button = screen.getByRole('button', { name: 'Enviando' })
    await userEvent.click(button)

    expect(button).toBeDisabled()
    expect(onClick).not.toHaveBeenCalled()
  })
})
