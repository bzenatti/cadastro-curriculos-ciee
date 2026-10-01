import { useState } from 'react'
import type { ChangeEvent, FormEvent, KeyboardEvent } from 'react'
import { MAX_LENGTH, validateCandidate } from '../../validation/validateCandidate'
import type { CandidateErrors } from '../../validation/validateCandidate'
import { Button } from '../ui/Button/Button'
import { Field } from '../ui/Field/Field'
import './CandidateForm.css'

export type CandidateFormValues = {
  name: string
  email: string
  phone: string
  position: string
  summary: string
}

const EMPTY_VALUES: CandidateFormValues = {
  name: '',
  email: '',
  phone: '',
  position: '',
  summary: '',
}

type CandidateFormProps = {
  // quem chama trata a API; pode devolver erros (ex.: do servidor) para o formulário mostrar embaixo dos campos
  onSubmit: (values: CandidateFormValues) => Promise<CandidateErrors | void>
}

export function CandidateForm({ onSubmit }: CandidateFormProps) {
  const [values, setValues] = useState(EMPTY_VALUES)
  const [errors, setErrors] = useState<CandidateErrors>({})
  const [submitting, setSubmitting] = useState(false)

  function handleChange(event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) {
    const { name, value } = event.target
    setValues((current) => ({ ...current, [name]: value }))
    setErrors((current) => ({ ...current, [name]: undefined }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const foundErrors = validateCandidate(values)
    setErrors(foundErrors)
    if (Object.keys(foundErrors).length > 0) return

    setSubmitting(true)
    try {
      const serverErrors = await onSubmit(values)
      if (serverErrors) setErrors(serverErrors)
    } finally {
      setSubmitting(false)
    }
  }

  // Enter num input não envia: passa para o próximo campo (no textarea continua sendo quebra de linha)
  function handleKeyDown(event: KeyboardEvent<HTMLFormElement>) {
    if (event.key !== 'Enter' || !(event.target instanceof HTMLInputElement)) return
    event.preventDefault()
    const controls = Array.from(event.currentTarget.elements)
    const next = controls[controls.indexOf(event.target) + 1]
    if (next instanceof HTMLElement) next.focus()
  }

  // ao sair de um campo, mostra só o erro dele (o cadastro valida todos)
  function validateField(name: keyof CandidateFormValues) {
    const fieldError = validateCandidate(values)[name]
    setErrors((current) => ({ ...current, [name]: fieldError }))
  }

  // o que todo campo tem em comum: nome, valor, erro, limite de tamanho e o handler
  function fieldProps(name: keyof CandidateFormValues) {
    return {
      name,
      value: values[name],
      error: errors[name],
      maxLength: MAX_LENGTH[name],
      onChange: handleChange,
      onBlur: () => validateField(name),
    }
  }

  return (
    <form className="candidate-form" noValidate onSubmit={handleSubmit} onKeyDown={handleKeyDown}>
      <Field label="Nome completo" {...fieldProps('name')} />
      <Field label="E-mail" type="email" {...fieldProps('email')} />
      <Field label="Telefone" type="tel" {...fieldProps('phone')} />
      <Field label="Área ou cargo de interesse" {...fieldProps('position')} />
      <Field label="Resumo profissional" multiline {...fieldProps('summary')} />
      <Button
        type="submit"
        loading={submitting}
        // sem isso, o erro do campo que perde o foco desloca o botão, o clique se perde e nem todos os erros aparecem
        onMouseDown={(event) => event.preventDefault()}
      >
        Cadastrar
      </Button>
    </form>
  )
}
