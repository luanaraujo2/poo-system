import assert from 'node:assert/strict'
import { test } from 'node:test'

import { estaAtrasada, moverNaOrdem, ordenarTarefas, pontuacao } from '../src/servicos/ordemTarefas.js'

const agora = new Date(2026, 10, 8, 12)

function tarefa(id, diasAtePrazo, { status = 'Pendente', criticidade = 'Baixa', prioridade = 'NaoUrgente_NaoImportante', complexidade = 'Baixa' } = {}) {
  const prazo = new Date(agora)
  prazo.setDate(prazo.getDate() + diasAtePrazo)
  prazo.setHours(23, 59, 0, 0)
  return { id, status, prazo: prazo.toISOString(), criticidade, prioridade, complexidade }
}

test('pontua prazo, criticidade, prioridade e complexidade até 100 pontos', () => {
  const item = tarefa(1, -1, {
    criticidade: 'Critica',
    prioridade: 'Urgente_Importante',
    complexidade: 'Alta',
  })

  assert.equal(pontuacao(item, agora), 100)
})

test('atribui pontos de prazo decrescentes conforme a data se afasta', () => {
  assert.equal(pontuacao(tarefa(1, 0), agora), 35)
  assert.equal(pontuacao(tarefa(2, 1), agora), 30)
  assert.equal(pontuacao(tarefa(3, 3), agora), 20)
  assert.equal(pontuacao(tarefa(4, 7), agora), 10)
  assert.equal(pontuacao(tarefa(5, 8), agora), 0)
})

test('ordena tarefas atrasadas abertas antes das demais e sinaliza o atraso', () => {
  const atrasada = tarefa(1, -1)
  const futuraUrgente = tarefa(2, 2, { criticidade: 'Alta', prioridade: 'Urgente_Importante' })
  const concluidaAtrasada = tarefa(3, -1, { status: 'Concluida' })

  assert.equal(estaAtrasada(atrasada, agora), true)
  assert.equal(estaAtrasada(concluidaAtrasada, agora), false)
  assert.deepEqual(ordenarTarefas([futuraUrgente, concluidaAtrasada, atrasada], { agora }).map((x) => x.id), [1, 2, 3])
})

test('modo manual respeita a ordem escolhida sem tirar atrasadas do topo', () => {
  const abertaAtrasada = tarefa(1, -1)
  const outraAtrasada = tarefa(2, -2, { prioridade: 'Urgente_Importante' })
  const normalA = tarefa(3, 5)
  const normalB = tarefa(4, 3, { criticidade: 'Critica' })

  const ordenadas = ordenarTarefas([normalA, normalB, abertaAtrasada, outraAtrasada], {
    modo: 'manual',
    ordemManual: [4, 3, 2, 1],
    agora,
  })
  assert.deepEqual(ordenadas.map((x) => x.id), [2, 1, 4, 3])
})

test('troca a posição de duas tarefas sem alterar a ordem original', () => {
  const original = [1, 2, 3]
  assert.deepEqual(moverNaOrdem(original, 2, 3), [1, 3, 2])
  assert.deepEqual(original, [1, 2, 3])
})
