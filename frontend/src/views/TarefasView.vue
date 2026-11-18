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
        <Button label="Sair" icon="pi pi-sign-out" severity="secondary" text @click="sairDaConta" />
      </div>
    </header>
    <h1>Minhas tarefas</h1>

    <nav class="atalhos-nav" aria-label="Atalhos">
      <Button
        label="Planejamento"
        icon="pi pi-calendar"
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'planejamento' })"
      />
      <Button
        label="Histórico"
        icon="pi pi-history"
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'historico' })"
      />
      <Button
        label="Relatório"
        icon="pi pi-chart-bar"
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'relatorio' })"
      />
      <Button
        label="Disponibilidade"
        icon="pi pi-clock"
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'disponibilidade' })"
      />
    </nav>

    <Message v-if="sobrecarga && sobrecarga.sobrecargaMin > 0" severity="warn" :closable="false" class="alerta-sobrecarga">
      Nesta semana, as tarefas abertas passam o tempo livre em
      <strong>{{ formatarDuracao(sobrecarga.sobrecargaMin) }}</strong>.
      Estimativa: {{ formatarDuracao(sobrecarga.tempoEstimadoMin) }}; tempo livre: {{ formatarDuracao(sobrecarga.tempoLivreMin) }}.
    </Message>
    <Message v-if="erroSobrecarga" severity="error" :closable="false">{{ erroSobrecarga }}</Message>

    <!-- US13: lembretes das tarefas com prazo chegando. -->
    <section v-if="lembretes.length" class="lembretes" aria-label="Lembretes">
      <Message v-for="lembrete in lembretes" :key="lembrete.tarefaId" severity="info" :closable="false" class="lembrete">
        <div class="conteudo-lembrete">
          <span>
            <i class="pi pi-bell" aria-hidden="true" />
            <RouterLink :to="{ name: 'editar-tarefa', params: { id: lembrete.tarefaId } }">{{ lembrete.titulo }}</RouterLink>
            vence {{ quandoVence(lembrete.prazo) }}.
          </span>
          <Button
            label="Dispensar"
            severity="secondary"
            text
            size="small"
            :loading="dispensando === lembrete.tarefaId"
            @click="dispensar(lembrete)"
          />
        </div>
      </Message>
    </section>

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
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import SelectButton from 'primevue/selectbutton'
import Tag from 'primevue/tag'
import { useToast } from 'primevue/usetoast'

import { api } from '../servicos/api'
import { sair, sessao } from '../servicos/sessao'
import { CRITICIDADES, PRIORIDADES, STATUS, corDoStatus, formatarDuracao, formatarPrazo, textoDe } from '../constantes'

const router = useRouter()
const toast = useToast()

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
  return [...filtradas].sort((a, b) => {
    const atrasoA = estaAtrasada(a)
    const atrasoB = estaAtrasada(b)
    if (atrasoA !== atrasoB) return atrasoA ? -1 : 1

    if (modoOrdem.value === 'manual') {
      return indiceManual(a.id) - indiceManual(b.id) || a.id - b.id
    }
    return pontuacao(b) - pontuacao(a) || new Date(a.prazo) - new Date(b.prazo) || a.id - b.id
  })
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

function indiceManual(id) {
  const indice = ordemManual.value.indexOf(id)
  return indice < 0 ? Number.MAX_SAFE_INTEGER : indice
}

function estaAtrasada(tarefa) {
  return ['Pendente', 'EmAndamento'].includes(tarefa.status) && new Date(tarefa.prazo) < new Date()
}

function prazoPontos(tarefa) {
  if (estaAtrasada(tarefa)) return 40

  const hoje = new Date()
  hoje.setHours(0, 0, 0, 0)
  const prazo = new Date(tarefa.prazo)
  prazo.setHours(0, 0, 0, 0)
  const dias = Math.round((prazo - hoje) / 86400000)
  if (dias <= 0) return 35
  if (dias <= 1) return 30
  if (dias <= 3) return 20
  if (dias <= 7) return 10
  return 0
}

function pontuacao(tarefa) {
  const pontosCriticidade = { Baixa: 0, Media: 10, Alta: 20, Critica: 30 }
  const pontosPrioridade = {
    Urgente_Importante: 20,
    Importante_NaoUrgente: 15,
    Urgente_NaoImportante: 10,
    NaoUrgente_NaoImportante: 0,
  }
  const pontosComplexidade = { Baixa: 0, Media: 5, Alta: 10 }
  return prazoPontos(tarefa)
    + pontosCriticidade[tarefa.criticidade]
    + pontosPrioridade[tarefa.prioridade]
    + pontosComplexidade[tarefa.complexidade]
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
  const ordem = [...ordemManual.value]
  const indiceTarefa = ordem.indexOf(tarefa.id)
  const indiceVizinha = ordem.indexOf(vizinha.id)
  if (indiceTarefa < 0 || indiceVizinha < 0) return
  ;[ordem[indiceTarefa], ordem[indiceVizinha]] = [ordem[indiceVizinha], ordem[indiceTarefa]]
  ordemManual.value = ordem
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

// US13: os lembretes são conferidos ao abrir a tela e a cada minuto.
// Um lembrete que aparece com a tela aberta também vira um aviso (toast).
const lembretes = ref([])
const dispensando = ref(null)
let temporizadorLembretes = null

async function carregarLembretes() {
  try {
    const novos = await api('/tarefas/lembretes')
    const jaMostrados = new Set(lembretes.value.map((l) => l.tarefaId))
    if (temporizadorLembretes) {
      for (const lembrete of novos.filter((l) => !jaMostrados.has(l.tarefaId))) {
        toast.add({ severity: 'info', summary: 'Lembrete', detail: `${lembrete.titulo} vence ${quandoVence(lembrete.prazo)}.`, life: 8000 })
      }
    }
    lembretes.value = novos
  } catch {
    // Sem lembretes nesta conferência; tenta de novo no próximo minuto.
  }
}

async function dispensar(lembrete) {
  dispensando.value = lembrete.tarefaId
  try {
    await api(`/tarefas/${lembrete.tarefaId}/lembrete/dispensar`, { metodo: 'POST' })
    lembretes.value = lembretes.value.filter((l) => l.tarefaId !== lembrete.tarefaId)
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lembrete', detail: e.message, life: 4000 })
  } finally {
    dispensando.value = null
  }
}

function quandoVence(prazo) {
  const minutos = Math.max(0, Math.round((new Date(prazo) - new Date()) / 60000))
  if (minutos < 1) return 'em menos de 1min'
  if (minutos < 24 * 60) return `em ${formatarDuracao(minutos)}`
  return `em ${formatarPrazo(prazo)}`
}

onUnmounted(() => clearInterval(temporizadorLembretes))

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

  await carregarLembretes()
  temporizadorLembretes = setInterval(carregarLembretes, 60000)
})
</script>

<style scoped>
.topo {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.botoes-topo {
  display: flex;
  gap: 2px;
  margin-right: -10px;
}

.ola {
  font-size: 14px;
  color: var(--sgt-texto-suave);
}

h1 {
  margin: 0 0 12px;
  font-size: 26px;
}

.atalhos-nav {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
  margin-bottom: 16px;
}

.atalhos-nav :deep(.p-button) {
  justify-content: center;
  background: #fff;
}

.filtro {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-bottom: 14px;
}

.lembretes {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 14px;
}

.lembrete {
  margin: 0;
}

.conteudo-lembrete {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
}

.conteudo-lembrete .pi {
  margin-right: 4px;
}

.conteudo-lembrete a {
  font-weight: 600;
  color: inherit;
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
