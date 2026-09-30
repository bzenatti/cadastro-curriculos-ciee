import { useState } from 'react'
import { CandidateForm } from '../../components/CandidateForm/CandidateForm'
import type { CandidateFormValues } from '../../components/CandidateForm/CandidateForm'
import { Alert } from '../../components/ui/Alert/Alert'
import { ResumeUpload } from '../../components/ui/ResumeUpload/ResumeUpload'

// Provisório: troca por uma chamada ao backend quando o api/ existir.
async function sendCandidate(values: CandidateFormValues): Promise<void> {
  console.log('Enviaria ao backend:', values)
  await new Promise((resolve) => setTimeout(resolve, 1000))
}

// Provisório: troca pela chamada de importação do PDF (outra rota da API) quando ela existir.
function importResume(file: File): void {
  console.log('Importaria o PDF:', file.name)
}

export function NewCandidatePage() {
  const [status, setStatus] = useState<'success' | 'error' | null>(null)
  const [formKey, setFormKey] = useState(0)

  async function handleSubmit(values: CandidateFormValues) {
    setStatus(null)
    try {
      await sendCandidate(values)
      setStatus('success')
      setFormKey((current) => current + 1)
    } catch {
      setStatus('error')
    }
  }

  return (
    <>
      <h1>Novo candidato</h1>
      {status === 'success' && <Alert variant="success">Candidato cadastrado com sucesso.</Alert>}
      {status === 'error' && <Alert variant="error">Não foi possível cadastrar. Tente novamente.</Alert>}
      <h2>Importar de PDF (opcional)</h2>
      <ResumeUpload onSelect={importResume} />
      <h2>Dados do candidato</h2>
      <CandidateForm key={formKey} onSubmit={handleSubmit} />
    </>
  )
}
