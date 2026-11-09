const PONTOS_PRAZO = { atrasada: 40, hoje: 35, amanha: 30, curta: 20, semana: 10, longa: 0 }

export function estaAtrasada(tarefa, agora = new Date()) {
  return ['Pendente', 'EmAndamento'].includes(tarefa.status) && new Date(tarefa.prazo) < agora
}

function pontosPrazo(tarefa, agora) {
  if (estaAtrasada(tarefa, agora)) return PONTOS_PRAZO.atrasada

  const hoje = new Date(agora)
  hoje.setHours(0, 0, 0, 0)
  const prazo = new Date(tarefa.prazo)
  prazo.setHours(0, 0, 0, 0)
  const dias = Math.round((prazo - hoje) / 86400000)
  if (dias <= 0) return PONTOS_PRAZO.hoje
  if (dias === 1) return PONTOS_PRAZO.amanha
  if (dias <= 3) return PONTOS_PRAZO.curta
  if (dias <= 7) return PONTOS_PRAZO.semana
  return PONTOS_PRAZO.longa
}

export function pontuacao(tarefa, agora = new Date()) {
  const pontosCriticidade = { Baixa: 0, Media: 10, Alta: 20, Critica: 30 }
  const pontosPrioridade = {
    Urgente_Importante: 20,
    Importante_NaoUrgente: 15,
    Urgente_NaoImportante: 10,
    NaoUrgente_NaoImportante: 0,
  }
  const pontosComplexidade = { Baixa: 0, Media: 5, Alta: 10 }

  return pontosPrazo(tarefa, agora)
    + pontosCriticidade[tarefa.criticidade]
    + pontosPrioridade[tarefa.prioridade]
    + pontosComplexidade[tarefa.complexidade]
}

export function ordenarTarefas(tarefas, { modo = 'sugestao', ordemManual = [], agora = new Date() } = {}) {
  const indiceManual = (id) => {
    const indice = ordemManual.indexOf(id)
    return indice < 0 ? Number.MAX_SAFE_INTEGER : indice
  }

  return [...tarefas].sort((a, b) => {
    const atrasoA = estaAtrasada(a, agora)
    const atrasoB = estaAtrasada(b, agora)
    if (atrasoA !== atrasoB) return atrasoA ? -1 : 1

    if (modo === 'manual')
      return indiceManual(a.id) - indiceManual(b.id) || a.id - b.id

    return pontuacao(b, agora) - pontuacao(a, agora)
      || new Date(a.prazo) - new Date(b.prazo)
      || a.id - b.id
  })
}

export function moverNaOrdem(ordem, id, idVizinho) {
  const indice = ordem.indexOf(id)
  const indiceVizinho = ordem.indexOf(idVizinho)
  if (indice < 0 || indiceVizinho < 0 || indice === indiceVizinho) return ordem

  const atualizada = [...ordem]
  ;[atualizada[indice], atualizada[indiceVizinho]] = [atualizada[indiceVizinho], atualizada[indice]]
  return atualizada
}
