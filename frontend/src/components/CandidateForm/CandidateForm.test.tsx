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
    await user.type(screen.getByLabelText('Telefone'), '41998765432')
    await user.type(screen.getByLabelText('Área ou cargo de interesse'), 'Desenvolvimento')
    await user.type(screen.getByLabelText('Resumo profissional'), 'Estudante de sistemas.')
    await user.click(screen.getByRole('button', { name: 'Cadastrar' }))

    expect(onSubmit).toHaveBeenCalledWith({
      name: 'Maria da Silva',
      email: 'maria@exemplo.com',
      phone: '41998765432',
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

    await user.type(screen.getByLabelText('Nome completo'), 'Maria da Silva')
    await user.type(screen.getByLabelText('E-mail'), 'maria@exemplo.com')
    await user.click(screen.getByRole('button', { name: 'Cadastrar' }))
    await user.click(screen.getByRole('button', { name: 'Enviando' }))

    expect(onSubmit).toHaveBeenCalledTimes(1)

    finishSending()

    expect(await screen.findByRole('button', { name: 'Cadastrar' })).toBeEnabled()
  })

  it('com dados inválidos, mostra o erro de cada campo e não chama onSubmit', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<CandidateForm onSubmit={onSubmit} />)

    await user.type(screen.getByLabelText('E-mail'), 'maria')
    await user.click(screen.getByRole('button', { name: 'Cadastrar' }))

    expect(screen.getByLabelText('Nome completo')).toHaveAccessibleDescription('Informe o nome completo.')
    expect(screen.getByLabelText('E-mail')).toHaveAccessibleDescription(
      'Informe um e-mail válido, como nome@exemplo.com.',
    )
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('ao corrigir um campo, só o erro dele some', async () => {
    const user = userEvent.setup()
    render(<CandidateForm onSubmit={vi.fn()} />)

    await user.click(screen.getByRole('button', { name: 'Cadastrar' }))
    await user.type(screen.getByLabelText('Nome completo'), 'Maria')

    expect(screen.queryByText('Informe o nome completo.')).not.toBeInTheDocument()
    expect(screen.getByText('Informe o e-mail.')).toBeInTheDocument()
  })

  it('Enter num campo passa para o próximo em vez de cadastrar', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<CandidateForm onSubmit={onSubmit} />)

    await user.type(screen.getByLabelText('Nome completo'), 'Maria da Silva')
    await user.type(screen.getByLabelText('E-mail'), 'maria@exemplo.com{Enter}')

    expect(screen.getByLabelText('Telefone')).toHaveFocus()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('no resumo, Enter continua quebrando a linha', async () => {
    const user = userEvent.setup()
    render(<CandidateForm onSubmit={vi.fn()} />)

    await user.type(screen.getByLabelText('Resumo profissional'), 'linha 1{Enter}linha 2')

    expect(screen.getByLabelText('Resumo profissional')).toHaveValue('linha 1\nlinha 2')
  })

  it('ao sair de um campo inválido, mostra o erro antes de cadastrar', async () => {
    const user = userEvent.setup()
    render(<CandidateForm onSubmit={vi.fn()} />)

    await user.type(screen.getByLabelText('E-mail'), 'maria')
    await user.tab()

    expect(screen.getByLabelText('E-mail')).toHaveAccessibleDescription(
      'Informe um e-mail válido, como nome@exemplo.com.',
    )
    expect(screen.queryByText('Informe o nome completo.')).not.toBeInTheDocument()
  })

})
