import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Alert } from './Alert'

describe('Alert', () => {
  it('erro usa role="alert" para o leitor de tela avisar na hora', () => {
    render(<Alert variant="error">Falha ao enviar</Alert>)

    expect(screen.getByRole('alert')).toHaveTextContent('Falha ao enviar')
  })

  it('sucesso usa role="status" para avisar sem interromper', () => {
    render(<Alert variant="success">Cadastrado</Alert>)

    expect(screen.getByRole('status')).toHaveTextContent('Cadastrado')
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
