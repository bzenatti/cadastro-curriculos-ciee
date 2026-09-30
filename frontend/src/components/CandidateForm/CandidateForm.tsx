import { useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
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
  // quem chama trata os erros da API; o formulário só espera terminar
  onSubmit: (values: CandidateFormValues) => Promise<void>
}

export function CandidateForm({ onSubmit }: CandidateFormProps) {
  const [values, setValues] = useState(EMPTY_VALUES)
  const [submitting, setSubmitting] = useState(false)

  function handleChange(event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) {
    const { name, value } = event.target
    setValues((current) => ({ ...current, [name]: value }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    try {
      await onSubmit(values)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="candidate-form" onSubmit={handleSubmit}>
      <Field label="Nome completo" name="name" value={values.name} onChange={handleChange} />
      <Field label="E-mail" name="email" type="email" value={values.email} onChange={handleChange} />
      <Field label="Telefone" name="phone" type="tel" value={values.phone} onChange={handleChange} />
      <Field label="Área ou cargo de interesse" name="position" value={values.position} onChange={handleChange} />
      <Field label="Resumo profissional" name="summary" multiline value={values.summary} onChange={handleChange} />
      <Button type="submit" loading={submitting}>
        Cadastrar
      </Button>
    </form>
  )
}