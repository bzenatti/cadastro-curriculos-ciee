// A API manda a data em ISO 8601 UTC; o navegador converte para o fuso local. Só a data, sem hora.
export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR')
}
