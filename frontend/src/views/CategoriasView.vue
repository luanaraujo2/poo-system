<!-- US04 - Categorias: as 5 padrão (fixas) e as próprias, que dá para criar, renomear e excluir. -->
<template>
  <main class="tela">
    <header class="topo">
      <RouterLink :to="{ name: 'tarefas' }" class="voltar" aria-label="Voltar">
        <i class="pi pi-arrow-left" />
      </RouterLink>
      <h1>Categorias</h1>
      <span class="espaco" />
    </header>

    <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>

    <div v-if="carregando" class="carregando">
      <ProgressSpinner style="width: 40px; height: 40px" stroke-width="4" />
    </div>

    <template v-else>
      <section class="grupo">
        <h2>Suas categorias</h2>
        <p v-if="proprias.length === 0" class="dica">
          Você ainda não criou nenhuma. Crie para separar o que as categorias padrão não cobrem.
        </p>
        <ul v-else class="lista">
          <li v-for="categoria in proprias" :key="categoria.id" class="item">
            <span class="ponto" :style="{ background: categoria.cor }" />
            <div class="texto">
              <span class="nome">{{ categoria.nome }}</span>
              <span class="dica">{{ textoTarefas(categoria.tarefas) }}</span>
            </div>
            <Button
              icon="pi pi-pencil"
              severity="secondary"
              text
              rounded
              :aria-label="`Renomear ${categoria.nome}`"
              @click="abrirEdicao(categoria)"
            />
            <Button
              icon="pi pi-trash"
              severity="danger"
              text
              rounded
              :aria-label="`Excluir ${categoria.nome}`"
              @click="abrirExclusao(categoria)"
            />
          </li>
        </ul>
      </section>

      <section class="grupo">
        <h2>Padrão</h2>
        <p class="dica">Criadas junto com a conta. Não podem ser renomeadas nem excluídas.</p>
        <ul class="lista">
          <li v-for="categoria in padrao" :key="categoria.id" class="item">
            <span class="ponto" :style="{ background: categoria.cor }" />
            <div class="texto">
              <span class="nome">{{ categoria.nome }}</span>
              <span class="dica">{{ textoTarefas(categoria.tarefas) }}</span>
            </div>
            <i class="pi pi-lock cadeado" aria-label="Categoria padrão" />
          </li>
        </ul>
      </section>
    </template>

    <div class="acoes">
      <Button label="Nova categoria" icon="pi pi-plus" size="large" class="largura-total" @click="abrirCriacao" />
    </div>

    <!-- Criar / renomear -->
    <Dialog
      v-model:visible="formularioAberto"
      modal
      :header="emEdicao ? 'Renomear categoria' : 'Nova categoria'"
      :style="{ width: 'min(92vw, 400px)' }"
      :draggable="false"
    >
      <form id="form-categoria" class="formulario" @submit.prevent="salvar">
        <Message v-if="erroFormulario" severity="error" :closable="false">{{ erroFormulario }}</Message>
        <div class="campo">
          <label for="nome-categoria">Nome</label>
          <InputText id="nome-categoria" v-model="nome" maxlength="50" placeholder="Ex.: Academia" autofocus />
        </div>
        <div class="campo">
          <span id="rotulo-cor" class="rotulo">Cor</span>
          <div class="cores" role="radiogroup" aria-labelledby="rotulo-cor">
            <button
              v-for="opcao in CORES"
              :key="opcao"
              type="button"
              role="radio"
              :aria-checked="cor === opcao"
              :aria-label="opcao"
              :class="['cor', { selecionada: cor === opcao }]"
              :style="{ background: opcao }"
              @click="cor = opcao"
            >
              <i v-if="cor === opcao" class="pi pi-check" />
            </button>
          </div>
        </div>
      </form>
      <template #footer>
        <Button label="Cancelar" severity="secondary" outlined @click="formularioAberto = false" />
        <Button type="submit" form="form-categoria" label="Salvar" :loading="salvando" />
      </template>
    </Dialog>

    <!-- Excluir: se a categoria tiver tarefas, escolhe para onde elas vão -->
    <Dialog
      v-model:visible="exclusaoAberta"
      modal
      header="Excluir categoria?"
      :style="{ width: 'min(92vw, 400px)' }"
      :draggable="false"
    >
      <Message v-if="erroExclusao" severity="error" :closable="false">{{ erroExclusao }}</Message>
      <template v-if="paraExcluir?.tarefas">
        <p class="texto-dialogo">
          <strong>{{ paraExcluir.nome }}</strong> tem {{ textoTarefas(paraExcluir.tarefas) }}.
          Escolha para qual categoria {{ paraExcluir.tarefas === 1 ? 'ela vai' : 'elas vão' }}.
        </p>
        <div class="campo">
          <label for="mover-para">Mover para</label>
          <Select
            v-model="moverPara"
            input-id="mover-para"
            :options="destinos"
            option-label="nome"
            option-value="id"
            placeholder="Escolha a categoria"
            fluid
          />
        </div>
      </template>
      <p v-else class="texto-dialogo">
        <strong>{{ paraExcluir?.nome }}</strong> será excluída. Ela não tem tarefas.
      </p>
      <template #footer>
        <Button label="Cancelar" severity="secondary" outlined @click="exclusaoAberta = false" />
        <Button
          label="Excluir"
          severity="danger"
          :loading="excluindo"
          :disabled="paraExcluir?.tarefas > 0 && !moverPara"
          @click="excluir"
        />
      </template>
    </Dialog>
  </main>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import Select from 'primevue/select'
import { useToast } from 'primevue/usetoast'

import { api } from '../servicos/api'

// Cores oferecidas para as categorias novas (formato #RRGGBB, como as padrão).
const CORES = ['#2457A6', '#0E9F6E', '#D97706', '#DC2626', '#7C3AED', '#DB2777', '#0891B2', '#4B5563']

const toast = useToast()

const categorias = ref([])
const carregando = ref(true)
const erro = ref('')

const proprias = computed(() => categorias.value.filter((c) => !c.padrao))
const padrao = computed(() => categorias.value.filter((c) => c.padrao))

function textoTarefas(quantidade) {
  if (quantidade === 0) return 'nenhuma tarefa'
  return quantidade === 1 ? '1 tarefa' : `${quantidade} tarefas`
}

async function carregar() {
  try {
    categorias.value = await api('/categorias')
  } catch (e) {
    erro.value = e.message
  } finally {
    carregando.value = false
  }
}

onMounted(carregar)

// Criar / renomear
const formularioAberto = ref(false)
const emEdicao = ref(null)
const nome = ref('')
const cor = ref(CORES[0])
const erroFormulario = ref('')
const salvando = ref(false)

function abrirCriacao() {
  emEdicao.value = null
  nome.value = ''
  cor.value = CORES[0]
  erroFormulario.value = ''
  formularioAberto.value = true
}

function abrirEdicao(categoria) {
  emEdicao.value = categoria
  nome.value = categoria.nome
  cor.value = categoria.cor?.toUpperCase() ?? CORES[0]
  erroFormulario.value = ''
  formularioAberto.value = true
}

async function salvar() {
  if (nome.value.trim().length < 3) {
    erroFormulario.value = 'O nome da categoria deve ter pelo menos 3 caracteres.'
    return
  }
  salvando.value = true
  erroFormulario.value = ''
  try {
    const corpo = { nome: nome.value.trim(), cor: cor.value }
    const categoria = emEdicao.value
      ? await api(`/categorias/${emEdicao.value.id}`, { metodo: 'PUT', corpo })
      : await api('/categorias', { metodo: 'POST', corpo })
    formularioAberto.value = false
    toast.add({
      severity: 'success',
      summary: emEdicao.value ? 'Categoria atualizada' : 'Categoria criada',
      detail: categoria.nome,
      life: 3000,
    })
    await carregar()
  } catch (e) {
    erroFormulario.value = e.message
  } finally {
    salvando.value = false
  }
}

// Excluir
const exclusaoAberta = ref(false)
const paraExcluir = ref(null)
const moverPara = ref(null)
const erroExclusao = ref('')
const excluindo = ref(false)

const destinos = computed(() => categorias.value.filter((c) => c.id !== paraExcluir.value?.id))

function abrirExclusao(categoria) {
  paraExcluir.value = categoria
  moverPara.value = null
  erroExclusao.value = ''
  exclusaoAberta.value = true
}

async function excluir() {
  excluindo.value = true
  erroExclusao.value = ''
  try {
    const consulta = paraExcluir.value.tarefas > 0 ? `?moverPara=${moverPara.value}` : ''
    await api(`/categorias/${paraExcluir.value.id}${consulta}`, { metodo: 'DELETE' })
    exclusaoAberta.value = false
    toast.add({ severity: 'success', summary: 'Categoria excluída', detail: paraExcluir.value.nome, life: 3000 })
    await carregar()
  } catch (e) {
    erroExclusao.value = e.message
  } finally {
    excluindo.value = false
  }
}
</script>

<style scoped>
.topo {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.voltar {
  width: 44px;
  height: 44px;
  margin-left: -10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--sgt-texto);
  text-decoration: none;
}

.voltar .pi {
  font-size: 18px;
}

h1 {
  margin: 0;
  font-size: 18px;
}

.espaco {
  width: 34px;
}

.carregando {
  display: flex;
  justify-content: center;
  padding: 48px 0;
}

.grupo {
  margin-top: 18px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

h2 {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--sgt-texto-suave);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.grupo > .dica {
  margin: 0;
}

.lista {
  list-style: none;
  margin: 0;
  padding: 0;
  background: #fff;
  border: 1px solid var(--sgt-borda);
  border-radius: 12px;
}

.item {
  min-height: 60px;
  padding: 8px 8px 8px 16px;
  display: flex;
  align-items: center;
  gap: 12px;
}

.item + .item {
  border-top: 1px solid var(--sgt-borda);
}

.ponto {
  width: 12px;
  height: 12px;
  border-radius: 6px;
  flex: none;
}

.texto {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

.nome {
  font-weight: 600;
  overflow-wrap: anywhere;
}

.cadeado {
  width: 40px;
  text-align: center;
  color: #9aa6b2;
  font-size: 14px;
}

.acoes {
  margin-top: auto;
  padding-top: 24px;
}

.formulario {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.cores {
  display: grid;
  grid-template-columns: repeat(8, 1fr);
  gap: 8px;
}

.cor {
  aspect-ratio: 1;
  border: none;
  border-radius: 50%;
  cursor: pointer;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
}

.cor.selecionada {
  outline: 2px solid var(--sgt-texto);
  outline-offset: 2px;
}

.cor:focus-visible {
  outline: 2px solid var(--sgt-azul);
  outline-offset: 2px;
}

.texto-dialogo {
  margin: 0 0 12px;
  line-height: 1.45;
}
</style>
