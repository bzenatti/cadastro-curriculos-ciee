import { useRef, useState } from 'react'
import type { ChangeEvent, DragEvent } from 'react'
import { Alert } from '../Alert/Alert'
import './ResumeUpload.css'

const MAX_SIZE_BYTES = 5 * 1024 * 1024

type ResumeUploadProps = {
  onSelect: (file: File) => void
  disabled?: boolean
}

function validatePdf(file: File): string | null {
  const isPdf = file.type === 'application/pdf' || file.name.toLowerCase().endsWith('.pdf')

  if (!isPdf) return 'O arquivo precisa ser um PDF.'
  if (file.size > MAX_SIZE_BYTES) return 'O PDF deve ter no máximo 5 MB.'
  return null
}

export function ResumeUpload({ onSelect, disabled }: ResumeUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isDragging, setIsDragging] = useState(false)

  function handleFile(newFile: File | undefined) {
    if (disabled || !newFile) return

    const validationError = validatePdf(newFile)
    setError(validationError)
    setFile(validationError ? null : newFile)

    if (!validationError) onSelect(newFile)
  }

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    handleFile(event.target.files?.[0])
    event.target.value = ''
  }

  function handleDragOver(event: DragEvent<HTMLDivElement>) {
    event.preventDefault()
    setIsDragging(!disabled)
  }

  function handleDragLeave(event: DragEvent<HTMLDivElement>) {
    if (event.currentTarget.contains(event.relatedTarget as Node | null)) return
    setIsDragging(false)
  }

  function handleDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault()
    setIsDragging(false)
    handleFile(event.dataTransfer.files[0])
  }

  return (
    <div className="resume-upload">
      <div
        className={`resume-upload_area${isDragging ? ' resume-upload_area--dragging' : ''}`}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".pdf,application/pdf"
          aria-label="Arquivo PDF do currículo"
          hidden
          onChange={handleChange}
        />
        <p className="resume-upload_text">
          Arraste o currículo em PDF para cá ou{' '}
          <button
            type="button"
            className="resume-upload_link"
            disabled={disabled}
            onClick={() => inputRef.current?.click()}
          >
            escolha o arquivo
          </button>
        </p>
        {file && <p className="resume-upload_file">Arquivo selecionado: {file.name}</p>}
      </div>
      {error && <Alert variant="error">{error}</Alert>}
    </div>
  )
}
