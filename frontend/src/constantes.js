// Textos dos enums da API, como aparecem nas telas.
export const CRITICIDADES = [
  { valor: 'Baixa', texto: 'Baixa' },
  { valor: 'Media', texto: 'Média' },
  { valor: 'Alta', texto: 'Alta' },
  { valor: 'Critica', texto: 'Crítica' },
]

export const COMPLEXIDADES = [
  { valor: 'Baixa', texto: 'Baixa' },
  { valor: 'Media', texto: 'Média' },
  { valor: 'Alta', texto: 'Alta' },
]

export const PRIORIDADES = [
  { valor: 'Urgente_Importante', texto: 'Urgente e importante' },
  { valor: 'Importante_NaoUrgente', texto: 'Importante, não urgente' },
  { valor: 'Urgente_NaoImportante', texto: 'Urgente, não importante' },
  { valor: 'NaoUrgente_NaoImportante', texto: 'Nem urgente nem importante' },
]

export const STATUS = {
  Pendente: 'Pendente',
  EmAndamento: 'Em andamento',
  Concluida: 'Concluída',
  Cancelada: 'Cancelada',
}

// US07: opções do campo Status, na ordem em que aparecem.
export const STATUS_OPCOES = Object.entries(STATUS).map(([valor, texto]) => ({ valor, texto }))

// Cor da etiqueta de status (severity do Tag do PrimeVue).
export function corDoStatus(status) {
  return { Pendente: 'secondary', EmAndamento: 'info', Concluida: 'success', Cancelada: 'contrast' }[status]
}

export function textoDe(lista, valor) {
  return lista.find((item) => item.valor === valor)?.texto ?? valor
}

export function formatarDuracao(minutos) {
  const h = Math.floor(minutos / 60)
  const m = minutos % 60
  if (h && m) return `${h}h ${m}min`
  return h ? `${h}h` : `${m}min`
}

export function formatarPrazo(iso) {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
