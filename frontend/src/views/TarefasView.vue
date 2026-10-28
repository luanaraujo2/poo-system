<!-- Minhas tarefas: cada usuário vê só as próprias (US02). Daqui sai para "Nova tarefa" (US03),
     para editar uma tarefa tocando no cartão (US05), para "Categorias" (US04) e para "Sair". -->
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

    <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>

    <div v-if="carregando" class="carregando">
      <ProgressSpinner style="width: 40px; height: 40px" stroke-width="4" />
    </div>

    <section v-else-if="tarefas.length === 0 && !erro" class="vazio">
      <i class="pi pi-inbox" />
      <span class="vazio-titulo">Nenhuma tarefa ainda</span>
      <span class="dica">Cadastre a primeira para começar a organizar o seu tempo.</span>
    </section>

    <ul v-else class="lista">
      <li v-for="tarefa in tarefas" :key="tarefa.id">
        <RouterLink
          :to="{ name: 'editar-tarefa', params: { id: tarefa.id } }"
          class="cartao"
        >
          <div class="linha-titulo">
            <span class="titulo">{{ tarefa.titulo }}</span>
            <Tag :value="STATUS[tarefa.status] ?? tarefa.status" severity="secondary" />
          </div>
          <p v-if="tarefa.descricao" class="descricao">{{ tarefa.descricao }}</p>
          <div class="detalhes">
            <span class="detalhe">
              <span class="ponto-cor" :style="{ background: corDaCategoria(tarefa.categoriaId) }" />
              {{ tarefa.categoria }}
            </span>
            <span class="detalhe"><i class="pi pi-calendar" /> {{ formatarPrazo(tarefa.prazo) }}</span>
            <span class="detalhe"><i class="pi pi-clock" /> {{ formatarDuracao(tarefa.estimativaMin) }}</span>
          </div>
          <div class="etiquetas">
            <Tag :value="textoDe(PRIORIDADES, tarefa.prioridade)" :severity="corDaPrioridade(tarefa.prioridade)" />
            <Tag :value="`Criticidade ${textoDe(CRITICIDADES, tarefa.criticidade).toLowerCase()}`" severity="secondary" />
          </div>
        </RouterLink>
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
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import Tag from 'primevue/tag'

import { api } from '../servicos/api'
import { sair, sessao } from '../servicos/sessao'
import { CRITICIDADES, PRIORIDADES, STATUS, formatarDuracao, formatarPrazo, textoDe } from '../constantes'

const router = useRouter()

const tarefas = ref([])
const categorias = ref([])
const carregando = ref(true)
const erro = ref('')

const primeiroNome = computed(() => sessao.usuario?.nome?.split(' ')[0] ?? '')

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
  try {
    ;[tarefas.value, categorias.value] = await Promise.all([api('/tarefas'), api('/categorias')])
  } catch (e) {
    erro.value = e.message
  } finally {
    carregando.value = false
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

.cartao {
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
