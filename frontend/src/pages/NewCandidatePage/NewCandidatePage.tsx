import { useState } from 'react'
import { createCandidate, parseResume } from '../../api/candidates'
import type { ResumeExtraction } from '../../api/candidates'
import { ApiError } from '../../api/http'
import { CandidateForm } from '../../components/CandidateForm/CandidateForm'
import type { CandidateFormValues } from '../../components/CandidateForm/CandidateForm'
import { Alert } from '../../components/ui/Alert/Alert'
import { ResumeUpload } from '../../components/ui/ResumeUpload/ResumeUpload'
import type { CandidateErrors } from '../../validation/validateCandidate'

const EMPTY_VALUES: CandidateFormValues = { name: '', email: '', phone: '', position: '', summary: '' }

// Campos que o backend tenta achar no PDF.
const EXTRACTED_FIELDS = [
  { key: 'name', label: 'nome' },
  { key: 'email', label: 'e-mail' },
  { key: 'phone', label: 'telefone' },
] as const

type Feedback = { variant: 'success' | 'error' | 'info'; message: string }

// Só lista o que continua vazio: o que a pessoa já tinha digitado não é "não encontrado".
function importMessage(found: ResumeExtraction, current: CandidateFormValues): string {
  const missing = EXTRACTED_FIELDS.filter(({ key }) => !current[key].trim() && !found[key]).map(({ label }) => label)
  const next =
    missing.length === 0
      ? 'Confira os dados antes de cadastrar.'
      : `Não encontramos ${new Intl.ListFormat('pt-BR').format(missing)}: preencha à mão.`
  return `Currículo lido. Só os campos vazios foram preenchidos. ${next}`
}

export function NewCandidatePage() {
  const [feedback, setFeedback] = useState<Feedback | null>(null)
  const [values, setValues] = useState(EMPTY_VALUES)
  const [importing, setImporting] = useState(false)
  const [saving, setSaving] = useState(false)
  const [uploadKey, setUploadKey] = useState(0)

  async function handleImport(file: File) {
    setImporting(true)
    setFeedback(null)
    try {
      const found = await parseResume(file)
      // Só preenche o que está vazio; a atualização funcional preserva o que foi digitado enquanto o PDF era lido.
      setValues((current) => {
        const next = { ...current }
        for (const { key } of EXTRACTED_FIELDS) {
          const value = found[key]
          if (value && !current[key].trim()) next[key] = value
        }
        return next
      })
      setFeedback({ variant: 'info', message: importMessage(found, values) })
    } catch (error) {
      // "Arquivo selecionado" só vale para um PDF que foi lido: recria o quadro de upload para apagá-lo.
      setUploadKey((current) => current + 1)
      const message =
        error instanceof ApiError ? error.message : 'Não foi possível ler o currículo. Preencha o formulário à mão.'
      setFeedback({ variant: 'error', message })
    } finally {
      setImporting(false)
    }
  }

  // O que cabe embaixo de um campo volta para o formulário; o resto vira alerta no topo.
  async function handleSubmit(candidate: CandidateFormValues): Promise<CandidateErrors | void> {
    setFeedback(null)
    setSaving(true)
    try {
      await createCandidate(candidate)
      setFeedback({ variant: 'success', message: 'Candidato cadastrado com sucesso.' })
      setValues(EMPTY_VALUES)
      setUploadKey((current) => current + 1)
      // o aviso fica acima do formulário: sobe até ele
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.status === 409) return { email: error.message }
        if (error.status === 400 && Object.keys(error.fieldErrors).length > 0) return error.fieldErrors
      }
      const message = error instanceof ApiError ? error.message : 'Não foi possível cadastrar. Tente novamente.'
      setFeedback({ variant: 'error', message })
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <h1>Novo candidato</h1>
      {importing && <Alert>Lendo o currículo…</Alert>}
      {feedback && <Alert variant={feedback.variant}>{feedback.message}</Alert>}
      <h2>Importar de PDF (opcional)</h2>
      <ResumeUpload key={uploadKey} onSelect={handleImport} disabled={importing || saving} />
      <h2>Dados do candidato</h2>
      <CandidateForm values={values} onChange={setValues} onSubmit={handleSubmit} disabled={importing} />
    </>
  )
}
