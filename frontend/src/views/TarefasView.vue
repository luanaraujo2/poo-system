<!-- Minhas tarefas: cada usuário vê só as próprias (US02). Daqui sai para "Nova tarefa" (US03),
     para editar uma tarefa tocando no cartão (US05), para "Categorias" (US04) e para "Sair".
     A lista mostra só as tarefas principais; as subtarefas aparecem dentro delas (US06).
     O filtro separa as abertas das concluídas e canceladas (US07). -->
<template>
  <main class="tela">
    <header class="topo">
      <span class="ola">Olá, {{ primeiroNome }}</span>
      <div class="botoes-topo">
        <Button
          label="Categorias"
          icon="pi pi-tags"
          severity="secondary"
          text
          @click="router.push({ name: 'categorias' })"
        />
        <Button
          label="Disponibilidade"
          icon="pi pi-clock"
          severity="secondary"
          text
          @click="router.push({ name: 'disponibilidade' })"
        />
        <Button label="Sair" icon="pi pi-sign-out" severity="secondary" text @click="sairDaConta" />
      </div>
    </header>
    <h1>Minhas tarefas</h1>

    <Message v-if="sobrecarga && sobrecarga.sobrecargaMin > 0" severity="warn" :closable="false" class="alerta-sobrecarga">
      Nesta semana, as tarefas abertas passam o tempo livre em
      <strong>{{ formatarDuracao(sobrecarga.sobrecargaMin) }}</strong>.
      Estimativa: {{ formatarDuracao(sobrecarga.tempoEstimadoMin) }}; tempo livre: {{ formatarDuracao(sobrecarga.tempoLivreMin) }}.
    </Message>
    <Message v-if="erroSobrecarga" severity="error" :closable="false">{{ erroSobrecarga }}</Message>

    <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>

    <div v-if="carregando" class="carregando">
      <ProgressSpinner style="width: 40px; height: 40px" stroke-width="4" />
    </div>

    <SelectButton
      v-if="!carregando && principais.length"
      v-model="filtro"
      :options="FILTROS"
      option-label="texto"
      option-value="valor"
      :allow-empty="false"
      aria-label="Filtrar tarefas por status"
      class="filtro"
    />

    <div v-if="!carregando && principais.length" class="controles-ordem">
      <span class="rotulo-ordem">Ordem</span>
      <SelectButton
        v-model="modoOrdem"
        :options="MODOS_ORDEM"
        option-label="texto"
        option-value="valor"
        :allow-empty="false"
        aria-label="Ordenar tarefas"
        class="seletor-ordem"
      />
    </div>

    <section v-if="!carregando && principais.length === 0 && !erro" class="vazio">
      <i class="pi pi-inbox" />
      <span class="vazio-titulo">Nenhuma tarefa ainda</span>
      <span class="dica">Cadastre a primeira para começar a organizar o seu tempo.</span>
    </section>

    <section v-else-if="!carregando && visiveis.length === 0 && !erro" class="vazio">
      <i class="pi pi-check-square" />
      <span class="vazio-titulo">{{ FILTROS.find((f) => f.valor === filtro).vazio }}</span>
    </section>

    <ul v-else-if="!carregando" class="lista">
      <li v-for="tarefa in visiveis" :key="tarefa.id" class="item-tarefa">
        <RouterLink
          :to="{ name: 'editar-tarefa', params: { id: tarefa.id } }"
          :class="['cartao', { atrasada: estaAtrasada(tarefa) }]"
        >
          <div class="linha-titulo">
            <span class="titulo">{{ tarefa.titulo }}</span>
            <Tag :value="STATUS[tarefa.status] ?? tarefa.status" :severity="corDoStatus(tarefa.status)" />
          </div>
          <Tag v-if="estaAtrasada(tarefa)" value="Atrasada" severity="danger" class="etiqueta-atrasada" />
          <p v-if="tarefa.descricao" class="descricao">{{ tarefa.descricao }}</p>
          <div class="detalhes">
            <span class="detalhe">
              <span class="ponto-cor" :style="{ background: corDaCategoria(tarefa.categoriaId) }" />
              {{ tarefa.categoria }}
            </span>
            <span class="detalhe"><i class="pi pi-calendar" /> {{ formatarPrazo(tarefa.prazo) }}</span>
            <span class="detalhe"><i class="pi pi-clock" /> {{ formatarDuracao(tarefa.estimativaMin) }}</span>
            <span v-if="tarefa.subtarefasIds.length" class="detalhe">
              <i class="pi pi-sitemap" />
              {{ tarefa.subtarefasConcluidas }}/{{ tarefa.subtarefasIds.length }} subtarefas
            </span>
          </div>
          <div class="etiquetas">
            <Tag :value="textoDe(PRIORIDADES, tarefa.prioridade)" :severity="corDaPrioridade(tarefa.prioridade)" />
            <Tag :value="`Criticidade ${textoDe(CRITICIDADES, tarefa.criticidade).toLowerCase()}`" severity="secondary" />
            <Tag :value="`Pontuação ${pontuacao(tarefa)}`" severity="contrast" />
          </div>
        </RouterLink>
        <div v-if="modoOrdem === 'manual'" class="controles-manual" :aria-label="`Reordenar ${tarefa.titulo}`">
          <Button
            icon="pi pi-arrow-up"
            severity="secondary"
            text
            rounded
            :disabled="!podeMover(tarefa, -1)"
            :aria-label="`Mover ${tarefa.titulo} para cima`"
            @click="mover(tarefa, -1)"
          />
          <Button
            icon="pi pi-arrow-down"
            severity="secondary"
            text
            rounded
            :disabled="!podeMover(tarefa, 1)"
            :aria-label="`Mover ${tarefa.titulo} para baixo`"
            @click="mover(tarefa, 1)"
          />
        </div>
      </li>
    </ul>

    <div class="acoes">
      <Button
        label="Nova tarefa"
        icon="pi pi-plus"
        size="large"
        class="largura-total"
        @click="router.push({ name: 'nova-tarefa' })"
      />
    </div>
  </main>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import SelectButton from 'primevue/selectbutton'
import Tag from 'primevue/tag'

import { api } from '../servicos/api'
import { sair, sessao } from '../servicos/sessao'
import { CRITICIDADES, PRIORIDADES, STATUS, corDoStatus, formatarDuracao, formatarPrazo, textoDe } from '../constantes'
import { estaAtrasada, moverNaOrdem, ordenarTarefas, pontuacao } from '../servicos/ordemTarefas'

const router = useRouter()

const tarefas = ref([])
const categorias = ref([])
const carregando = ref(true)
const erro = ref('')
const sobrecarga = ref(null)
const erroSobrecarga = ref('')
const idUsuario = sessao.usuario?.id ?? 'usuario'
const CHAVE_ORDEM = `sgt.ordemTarefas.${idUsuario}`
const CHAVE_MODO_ORDEM = `sgt.modoOrdemTarefas.${idUsuario}`
const modoOrdem = ref(lerModoOrdem())
const ordemManual = ref(lerOrdemManual())

const primeiroNome = computed(() => sessao.usuario?.nome?.split(' ')[0] ?? '')

// US07: filtro por status. "Abertas" (pendentes e em andamento) é o padrão.
const FILTROS = [
  { valor: 'abertas', texto: 'Abertas', status: ['Pendente', 'EmAndamento'], vazio: 'Nenhuma tarefa aberta' },
  { valor: 'concluidas', texto: 'Concluídas', status: ['Concluida'], vazio: 'Nenhuma tarefa concluída' },
  { valor: 'canceladas', texto: 'Canceladas', status: ['Cancelada'], vazio: 'Nenhuma tarefa cancelada' },
  { valor: 'todas', texto: 'Todas', status: null, vazio: 'Nenhuma tarefa' },
]
const filtro = ref('abertas')
const MODOS_ORDEM = [
  { valor: 'sugestao', texto: 'Sugestão' },
  { valor: 'manual', texto: 'Manual' },
]

// US06: só as tarefas principais; as subtarefas ficam dentro da tela de cada uma.
const principais = computed(() => tarefas.value.filter((t) => t.tarefaPaiId === null))
const visiveis = computed(() => {
  const status = FILTROS.find((f) => f.valor === filtro.value).status
  const filtradas = status ? principais.value.filter((t) => status.includes(t.status)) : principais.value
  return ordenarTarefas(filtradas, { modo: modoOrdem.value, ordemManual: ordemManual.value })
})

function lerOrdemManual() {
  try {
    const salva = JSON.parse(localStorage.getItem(CHAVE_ORDEM) ?? '[]')
    return Array.isArray(salva) ? salva.filter(Number.isInteger) : []
  } catch {
    return []
  }
}

function lerModoOrdem() {
  try {
    return localStorage.getItem(CHAVE_MODO_ORDEM) === 'manual' ? 'manual' : 'sugestao'
  } catch {
    return 'sugestao'
  }
}

watch(modoOrdem, (modo) => {
  try {
    localStorage.setItem(CHAVE_MODO_ORDEM, modo)
  } catch {
    // O modo escolhido continua valendo até sair desta tela.
  }
})

function salvarOrdemManual() {
  try {
    localStorage.setItem(CHAVE_ORDEM, JSON.stringify(ordemManual.value))
  } catch {
    // A ordenação manual continua valendo até sair desta tela.
  }
}

function podeMover(tarefa, direcao) {
  const indice = visiveis.value.findIndex((item) => item.id === tarefa.id)
  const vizinha = visiveis.value[indice + direcao]
  return Boolean(vizinha && estaAtrasada(vizinha) === estaAtrasada(tarefa))
}

function mover(tarefa, direcao) {
  if (!podeMover(tarefa, direcao)) return
  const indice = visiveis.value.findIndex((item) => item.id === tarefa.id)
  const vizinha = visiveis.value[indice + direcao]
  ordemManual.value = moverNaOrdem(ordemManual.value, tarefa.id, vizinha.id)
  salvarOrdemManual()
}

function corDaCategoria(id) {
  return categorias.value.find((c) => c.id === id)?.cor ?? '#9aa6b2'
}

function corDaPrioridade(prioridade) {
  return {
    Urgente_Importante: 'danger',
    Importante_NaoUrgente: 'info',
    Urgente_NaoImportante: 'warn',
    NaoUrgente_NaoImportante: 'secondary',
  }[prioridade]
}

function sairDaConta() {
  sair()
  router.push({ name: 'entrar' })
}

onMounted(async () => {
  let tarefasCarregadas = false
  try {
    ;[tarefas.value, categorias.value] = await Promise.all([api('/tarefas'), api('/categorias')])
    tarefasCarregadas = true
  } catch (e) {
    erro.value = e.message
  } finally {
    carregando.value = false
  }

  if (tarefasCarregadas) {
    const idsAtuais = new Set(principais.value.map((t) => t.id))
    const ordemAtual = ordemManual.value.filter((id) => idsAtuais.has(id))
    for (const tarefa of [...principais.value].sort((a, b) => pontuacao(b) - pontuacao(a))) {
      if (!ordemAtual.includes(tarefa.id)) ordemAtual.push(tarefa.id)
    }
    ordemManual.value = ordemAtual
    salvarOrdemManual()
  }

  try {
    sobrecarga.value = await api('/tarefas/sobrecarga-semana')
  } catch (e) {
    erroSobrecarga.value = e.message
  }
})
</script>

<style scoped>
.topo {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.ola {
  font-size: 14px;
  color: var(--sgt-texto-suave);
}

h1 {
  margin: 0 0 20px;
  font-size: 26px;
}

.filtro {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-bottom: 14px;
}

.controles-ordem {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
  margin: -4px 0 14px;
}

.rotulo-ordem {
  color: var(--sgt-texto-suave);
  font-size: 13px;
}

.seletor-ordem :deep(.p-togglebutton) {
  min-height: 34px;
  padding: 4px 10px;
  font-size: 13px;
}

.filtro :deep(.p-togglebutton) {
  padding: 0;
  min-height: 36px;
  font-size: 13px;
}

.filtro :deep(.p-togglebutton-content) {
  width: 100%;
  justify-content: center;
  padding: 4px 2px;
}

.carregando {
  display: flex;
  justify-content: center;
  padding: 48px 0;
}

.vazio {
  margin-top: 32px;
  padding: 32px 20px;
  border: 1px dashed #c9d1da;
  border-radius: 12px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  text-align: center;
}

.vazio .pi {
  font-size: 28px;
  color: #9aa6b2;
}

.vazio-titulo {
  font-weight: 600;
}

.lista {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.item-tarefa {
  display: flex;
  align-items: center;
  gap: 6px;
}

.cartao {
  flex: 1;
  min-width: 0;
  background: #fff;
  border: 1px solid var(--sgt-borda);
  border-radius: 12px;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  color: inherit;
  text-decoration: none;
}

.cartao.atrasada {
  border-color: #ef4444;
  background: #fff7f7;
}

.etiqueta-atrasada {
  align-self: flex-start;
}

.controles-manual {
  display: flex;
  flex-direction: column;
}

.cartao:hover {
  border-color: #b8c2cd;
}

.cartao:focus-visible {
  outline: 2px solid var(--sgt-azul);
  outline-offset: 2px;
}

.botoes-topo {
  display: flex;
  gap: 2px;
  margin-right: -10px;
}

.linha-titulo {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.titulo {
  font-size: 16px;
  font-weight: 600;
}

.descricao {
  margin: 0;
  font-size: 14px;
  color: var(--sgt-texto-suave);
}

.detalhes {
  display: flex;
  flex-wrap: wrap;
  gap: 6px 14px;
  font-size: 13px;
  color: var(--sgt-texto-suave);
}

.detalhe {
  display: flex;
  align-items: center;
  gap: 6px;
}

.detalhe .pi {
  font-size: 13px;
}

.etiquetas {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.acoes {
  margin-top: auto;
  padding-top: 24px;
  position: sticky;
  bottom: 0;
  padding-bottom: 8px;
  background: linear-gradient(to bottom, transparent, var(--sgt-fundo) 30%);
}
</style>
