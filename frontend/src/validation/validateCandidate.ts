import type { CandidateFormValues } from '../components/CandidateForm/CandidateForm'

export type CandidateErrors = Partial<Record<keyof CandidateFormValues, string>>

// Os mesmos limites vão para o DTO e para as colunas do banco no backend.
export const MAX_LENGTH: Record<keyof CandidateFormValues, number> = {
  name: 150,
  email: 254,
  phone: 20,
  position: 100,
  summary: 2000,
}

// A mesma regex vai no backend: texto@texto.texto, sem espaços e com um só @.
const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/

// A mesma regex vai no backend: só números, DDD mais 8 ou 9 dígitos (10 ou 11 no total).
const PHONE_PATTERN = /^[0-9]{10,11}$/

export function validateCandidate(values: CandidateFormValues): CandidateErrors {
  const errors: CandidateErrors = {}

  if (values.name.trim() === '') errors.name = 'Informe o nome completo.'

  if (values.email.trim() === '') {
    errors.email = 'Informe o e-mail.'
  } else if (!EMAIL_PATTERN.test(values.email)) {
    errors.email = 'Informe um e-mail válido, como nome@exemplo.com.'
  }

  if (values.phone !== '' && !PHONE_PATTERN.test(values.phone)) {
    errors.phone = 'Informe só números, com DDD. Ex.: 41998765432.'
  }

  for (const field of Object.keys(MAX_LENGTH) as (keyof CandidateFormValues)[]) {
    if (!errors[field] && values[field].length > MAX_LENGTH[field]) {
      errors[field] = `Use no máximo ${MAX_LENGTH[field]} caracteres.`
    }
  }

  return errors
}
