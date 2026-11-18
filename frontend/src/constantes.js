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

// US13: antecedência do lembrete, em minutos antes do prazo. null = sem lembrete.
export const LEMBRETE_PADRAO_MIN = 24 * 60
export const OPCOES_LEMBRETE = [
  { valor: null, texto: 'Sem lembrete' },
  { valor: 30, texto: '30min antes' },
  { valor: 60, texto: '1h antes' },
  { valor: 3 * 60, texto: '3h antes' },
  { valor: 12 * 60, texto: '12h antes' },
  { valor: LEMBRETE_PADRAO_MIN, texto: '24h antes' },
  { valor: 2 * 24 * 60, texto: '2 dias antes' },
  { valor: 7 * 24 * 60, texto: '1 semana antes' },
]

export const DIAS_SEMANA = {
  Segunda: 'Segunda-feira',
  Terca: 'Terça-feira',
  Quarta: 'Quarta-feira',
  Quinta: 'Quinta-feira',
  Sexta: 'Sexta-feira',
  Sabado: 'Sábado',
  Domingo: 'Domingo',
}

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

export function formatarDataCurta(dataIso) {
  const [ano, mes, dia] = dataIso.split('-')
  return `${dia}/${mes}`
}

export function formatarDataHora(iso) {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
}
