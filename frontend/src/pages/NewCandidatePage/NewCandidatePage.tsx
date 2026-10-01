import { useState } from 'react'
import { createCandidate } from '../../api/candidates'
import { ApiError } from '../../api/http'
import { CandidateForm } from '../../components/CandidateForm/CandidateForm'
import type { CandidateFormValues } from '../../components/CandidateForm/CandidateForm'
import { Alert } from '../../components/ui/Alert/Alert'
import { ResumeUpload } from '../../components/ui/ResumeUpload/ResumeUpload'
import type { CandidateErrors } from '../../validation/validateCandidate'

// @TODO troca pela chamada de importação do PDF quando ela existir.
function importResume(file: File): void {
  console.log('Importaria o PDF:', file.name)
}

type Feedback = { variant: 'success' | 'error'; message: string }

export function NewCandidatePage() {
  const [feedback, setFeedback] = useState<Feedback | null>(null)
  const [formKey, setFormKey] = useState(0)

  // O que cabe embaixo de um campo volta para o formulário; o resto vira alerta no topo.
  async function handleSubmit(values: CandidateFormValues): Promise<CandidateErrors | void> {
    setFeedback(null)
    try {
      await createCandidate(values)
      setFeedback({ variant: 'success', message: 'Candidato cadastrado com sucesso.' })
      setFormKey((current) => current + 1)
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.status === 409) return { email: error.message }
        if (error.status === 400 && Object.keys(error.fieldErrors).length > 0) return error.fieldErrors
      }
      const message = error instanceof ApiError ? error.message : 'Não foi possível cadastrar. Tente novamente.'
      setFeedback({ variant: 'error', message })
    }
  }

  return (
    <>
      <h1>Novo candidato</h1>
      {feedback && <Alert variant={feedback.variant}>{feedback.message}</Alert>}
      <h2>Importar de PDF (opcional)</h2>
      <ResumeUpload onSelect={importResume} />
      <h2>Dados do candidato</h2>
      <CandidateForm key={formKey} onSubmit={handleSubmit} />
    </>
  )
}
