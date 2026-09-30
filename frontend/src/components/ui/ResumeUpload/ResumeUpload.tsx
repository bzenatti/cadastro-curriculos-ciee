import { useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { Alert } from '../Alert/Alert'
import './ResumeUpload.css'

const MAX_SIZE_BYTES = 5 * 1024 * 1024

type ResumeUploadProps = {
  onSelect: (file: File) => void
}

function validatePdf(file: File): string | null {
  const isPdf = file.type === 'application/pdf' || file.name.toLowerCase().endsWith('.pdf')

  if (!isPdf) return 'O arquivo precisa ser um PDF.'
  if (file.size > MAX_SIZE_BYTES) return 'O PDF deve ter no máximo 5 MB.'
  return null
}

export function ResumeUpload({ onSelect }: ResumeUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)

  function handleFile(newFile: File | undefined) {
    if (!newFile) return

    const validationError = validatePdf(newFile)
    setError(validationError)
    setFile(validationError ? null : newFile)

    if (!validationError) onSelect(newFile)
  }

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    handleFile(event.target.files?.[0])
    event.target.value = ''
  }

  return (
    <div className="resume-upload">
      <div className="resume-upload_area">
        <input
          ref={inputRef}
          type="file"
          accept=".pdf,application/pdf"
          aria-label="Arquivo PDF do currículo"
          hidden
          onChange={handleChange}
        />
        <button type="button" className="resume-upload_link" onClick={() => inputRef.current?.click()}>
          Escolher o arquivo
        </button>
        {file && <p className="resume-upload_file">Arquivo selecionado: {file.name}</p>}
      </div>
      {error && <Alert variant="error">{error}</Alert>}
    </div>
  )
}