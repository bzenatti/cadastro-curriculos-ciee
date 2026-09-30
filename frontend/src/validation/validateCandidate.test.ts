import { describe, expect, it } from 'vitest'
import { MAX_LENGTH, validateCandidate } from './validateCandidate'

const valid = { name: 'Maria da Silva', email: 'maria@exemplo.com', phone: '', position: '', summary: '' }
const INVALID_EMAIL = 'Informe um e-mail válido, como nome@exemplo.com.'
const INVALID_PHONE = 'Informe só números, com DDD. Ex.: 41998765432.'

describe('validateCandidate', () => {
  it('aceita só nome e e-mail: os outros campos são opcionais', () => {
    expect(validateCandidate(valid)).toEqual({})
  })

  it('exige nome e e-mail, e nome só com espaços conta como vazio', () => {
    const errors = validateCandidate({ ...valid, name: '   ', email: '' })

    expect(errors.name).toBe('Informe o nome completo.')
    expect(errors.email).toBe('Informe o e-mail.')
  })

  it.each(['maria', 'maria@', 'maria@exemplo', 'maria @exemplo.com', 'maria@@exemplo.com'])(
    'recusa o e-mail "%s"',
    (email) => {
      expect(validateCandidate({ ...valid, email }).email).toBe(INVALID_EMAIL)
    },
  )

  it('aceita e-mail com ponto, + e domínio .com.br', () => {
    expect(validateCandidate({ ...valid, email: 'maria.silva+cv@empresa.com.br' })).toEqual({})
  })

  it('aceita o tamanho máximo e recusa um caractere a mais', () => {
    const limit = MAX_LENGTH.summary

    expect(validateCandidate({ ...valid, summary: 'a'.repeat(limit) })).toEqual({})
    expect(validateCandidate({ ...valid, summary: 'a'.repeat(limit + 1) }).summary).toBe(
      `Use no máximo ${limit} caracteres.`,
    )
  })

  it.each(['(41) 99876-5432', '41-99876-5432', '+5541998765432', '419987654', '419987654321', 'abc'])(
    'recusa o telefone "%s"',
    (phone) => {
      expect(validateCandidate({ ...valid, phone }).phone).toBe(INVALID_PHONE)
    },
  )

  it('aceita telefone com 10 ou 11 números', () => {
    expect(validateCandidate({ ...valid, phone: '4133334444' })).toEqual({})
    expect(validateCandidate({ ...valid, phone: '41998765432' })).toEqual({})
  })
})
