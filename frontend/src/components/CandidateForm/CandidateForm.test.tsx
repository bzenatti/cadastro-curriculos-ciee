import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { CandidateForm } from './CandidateForm'

describe('CandidateForm', () => {
  it('ao cadastrar, entrega para onSubmit o que foi digitado em cada campo', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<CandidateForm onSubmit={onSubmit} />)

    await user.type(screen.getByLabelText('Nome completo'), 'Maria da Silva')
    await user.type(screen.getByLabelText('E-mail'), 'maria@exemplo.com')
    await user.type(screen.getByLabelText('Telefone'), '(41) 99876-5432')
    await user.type(screen.getByLabelText('Área ou cargo de interesse'), 'Desenvolvimento')
    await user.type(screen.getByLabelText('Resumo profissional'), 'Estudante de sistemas.')
    await user.click(screen.getByRole('button', { name: 'Cadastrar' }))

    expect(onSubmit).toHaveBeenCalledWith({
      name: 'Maria da Silva',
      email: 'maria@exemplo.com',
      phone: '(41) 99876-5432',
      position: 'Desenvolvimento',
      summary: 'Estudante de sistemas.',
    })
  })

  it('enquanto envia, mostra "Enviando" e não deixa cadastrar de novo', async () => {
    const user = userEvent.setup()
    let finishSending = () => {}
    const onSubmit = vi.fn(
      () => new Promise<void>((resolve) => { finishSending = () => resolve() }),
    )
    render(<CandidateForm onSubmit={onSubmit} />)

    await user.click(screen.getByRole('button', { name: 'Cadastrar' }))
    await user.click(screen.getByRole('button', { name: 'Enviando' }))

    expect(onSubmit).toHaveBeenCalledTimes(1)

    finishSending()

    expect(await screen.findByRole('button', { name: 'Cadastrar' })).toBeEnabled()
  })
})
