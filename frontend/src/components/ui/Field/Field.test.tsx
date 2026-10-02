import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Field } from './Field'

describe('Field', () => {
  it('liga cada label ao seu próprio input', () => {
    render(
      <>
        <Field label="Nome completo" />
        <Field label="E-mail" />
      </>,
    )

    const nome = screen.getByLabelText('Nome completo')
    const email = screen.getByLabelText('E-mail')

    expect(nome).not.toBe(email)
  })

  it('com erro, mostra a mensagem e marca o input como inválido', () => {
    render(<Field label="E-mail" error="E-mail inválido" />)

    const input = screen.getByLabelText('E-mail')

    expect(screen.getByRole('alert')).toHaveTextContent('E-mail inválido')
    expect(input).toBeInvalid()
    expect(input).toHaveAccessibleDescription('E-mail inválido')
  })

  it('com multiline, usa um textarea com o mesmo vínculo de label e erro', () => {
    render(<Field label="Resumo profissional" multiline error="Texto muito longo" />)

    const textarea = screen.getByLabelText('Resumo profissional')

    expect(textarea).toBeInstanceOf(HTMLTextAreaElement)
    expect(textarea).toBeInvalid()
    expect(textarea).toHaveAccessibleDescription('Texto muito longo')
  })
})
