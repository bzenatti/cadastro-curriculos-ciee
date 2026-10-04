import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// o jsdom não implementa a rolagem da janela
window.scrollTo = () => {}

afterEach(() => {
  cleanup()
})
