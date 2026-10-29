<!-- US03 - Cadastrar tarefa / US05 - Editar e excluir tarefa (a mesma tela, em modo edição quando a rota traz o id) -->
<template>
  <main class="tela">
    <header class="topo">
      <RouterLink :to="{ name: 'tarefas' }" class="fechar" aria-label="Fechar">
        <i class="pi pi-times" />
      </RouterLink>
      <h1>{{ modoEdicao ? 'Editar tarefa' : 'Nova tarefa' }}</h1>
      <span class="espaco" />
    </header>

    <div v-if="carregandoTarefa" class="carregando">
      <ProgressSpinner style="width: 40px; height: 40px" stroke-width="4" />
    </div>

    <form v-show="!carregandoTarefa" id="form-tarefa" class="formulario" @submit.prevent="salvar">
      <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>
      <Message v-if="avisoAtrasada" severity="warn" :closable="false">
        Esta tarefa está atrasada. Dá para salvar sem mudar o prazo; se mudar, o novo prazo não pode ter passado.
      </Message>

      <div class="campo">
        <label for="titulo">Título *</label>
        <InputText id="titulo" v-model="titulo" placeholder="Ex.: Estudar para a prova de Estatística" size="large" />
      </div>

      <div class="campo">
        <label for="descricao">Descrição</label>
        <Textarea id="descricao" v-model="descricao" rows="2" auto-resize placeholder="Detalhes, links, observações" />
      </div>

      <div class="campo">
        <span id="rotulo-categoria" class="rotulo">Categoria *</span>
        <div class="chips" role="radiogroup" aria-labelledby="rotulo-categoria">
          <button
            v-for="categoria in categorias"
            :key="categoria.id"
            type="button"
            role="radio"
            :aria-checked="categoriaId === categoria.id"
            :class="['chip', { selecionado: categoriaId === categoria.id }]"
            @click="categoriaId = categoria.id"
          >
            <span class="ponto-cor" :style="{ background: categoriaId === categoria.id ? '#fff' : categoria.cor }" />
            {{ categoria.nome }}
          </button>
          <span v-if="carregandoCategorias" class="dica">Carregando categorias…</span>
        </div>
      </div>

      <div class="campo">
        <span id="rotulo-prazo" class="rotulo">Prazo *</span>
        <div class="atalhos" role="group" aria-labelledby="rotulo-prazo">
          <button
            v-for="atalho in ATALHOS_PRAZO"
            :key="atalho.texto"
            type="button"
            :class="['chip', { selecionado: prazo === atalho.data() }]"
            @click="prazo = atalho.data()"
          >
            {{ atalho.texto }}
          </button>
        </div>
        <div class="prazo-hora">
          <div class="campo">
            <label for="prazo" class="rotulo-pequeno">Dia</label>
            <InputText id="prazo" v-model="prazo" type="date" :min="dataMinima" />
          </div>
          <div class="campo">
            <label for="hora" class="rotulo-pequeno">Hora (opcional)</label>
            <InputText
              id="hora"
              :model-value="hora"
              inputmode="numeric"
              maxlength="5"
              placeholder="ex.: 18:00"
              autocomplete="off"
              @input="formatarHora"
            />
          </div>
        </div>
        <span class="dica">O prazo não pode ser uma data que já passou. Sem hora, vale até 23:59.</span>
      </div>

      <div class="campo">
        <span id="rotulo-estimado" class="rotulo">Tempo estimado *</span>
        <div class="atalhos" role="group" aria-labelledby="rotulo-estimado">
          <button
            v-for="minutosAtalho in ATALHOS_TEMPO"
            :key="minutosAtalho"
            type="button"
            :class="['chip', { selecionado: totalMinutos === minutosAtalho }]"
            @click="definirTempo(minutosAtalho)"
          >
            {{ formatarDuracao(minutosAtalho) }}
          </button>
        </div>
        <div class="tempo">
          <InputNumber v-model="horas" :min="0" :max="999" input-class="numero" placeholder="0" aria-label="Horas" aria-describedby="rotulo-estimado" />
          <span class="dica">h</span>
          <InputNumber v-model="minutos" :min="0" :max="59" input-class="numero" placeholder="0" aria-label="Minutos" aria-describedby="rotulo-estimado" />
          <span class="dica">min</span>
        </div>
      </div>

      <div class="campo">
        <span id="rotulo-criticidade" class="rotulo">Criticidade *</span>
        <SelectButton
          v-model="criticidade"
          :options="CRITICIDADES"
          option-label="texto"
          option-value="valor"
          aria-labelledby="rotulo-criticidade"
          class="grade grade-4"
        />
      </div>

      <div class="campo">
        <span id="rotulo-complexidade" class="rotulo">Complexidade *</span>
        <SelectButton
          v-model="complexidade"
          :options="COMPLEXIDADES"
          option-label="texto"
          option-value="valor"
          aria-labelledby="rotulo-complexidade"
          class="grade grade-3"
        />
      </div>

      <div class="campo">
        <span id="rotulo-prioridade" class="rotulo">Prioridade *</span>
        <SelectButton
          v-model="prioridade"
          :options="PRIORIDADES"
          option-label="texto"
          option-value="valor"
          aria-labelledby="rotulo-prioridade"
          class="grade grade-2"
        />
      </div>
    </form>

    <div v-show="!carregandoTarefa" class="acoes">
      <Button label="Cancelar" severity="secondary" outlined size="large" @click="router.push({ name: 'tarefas' })" />
      <Button type="submit" form="form-tarefa" label="Salvar" size="large" :loading="salvando" />
    </div>
    <span v-if="!modoEdicao" class="dica rodape">* campos obrigatórios. A tarefa começa como Pendente.</span>
    <Button
      v-if="modoEdicao && !carregandoTarefa && tarefaOriginal"
      label="Excluir tarefa"
      icon="pi pi-trash"
      severity="danger"
      text
      class="excluir"
      @click="confirmandoExclusao = true"
    />

    <!-- US05: a exclusão sempre pede confirmação. -->
    <Dialog
      v-model:visible="confirmandoExclusao"
      modal
      header="Excluir tarefa?"
      :style="{ width: 'min(92vw, 400px)' }"
      :draggable="false"
    >
      <p class="texto-dialogo">
        <strong>{{ tarefaOriginal?.titulo }}</strong> será excluída. Isso não pode ser desfeito.
      </p>
      <p v-if="quantidadeSubtarefas" class="texto-dialogo">
        {{ quantidadeSubtarefas === 1 ? 'A subtarefa dela também será excluída.' : `As ${quantidadeSubtarefas} subtarefas dela também serão excluídas.` }}
      </p>
      <template #footer>
        <Button label="Cancelar" severity="secondary" outlined @click="confirmandoExclusao = false" />
        <Button label="Excluir" severity="danger" :loading="excluindo" @click="excluir" />
      </template>
    </Dialog>
  </main>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import SelectButton from 'primevue/selectbutton'
import Textarea from 'primevue/textarea'
import { useToast } from 'primevue/usetoast'

import { api } from '../servicos/api'
import { COMPLEXIDADES, CRITICIDADES, PRIORIDADES, formatarDuracao } from '../constantes'

// id vem da rota /tarefas/:id; sem id, a tela é o cadastro.
const props = defineProps({ id: { type: String, default: null } })
const modoEdicao = computed(() => props.id !== null)

const router = useRouter()
const toast = useToast()

// Data no formato do campo de data do navegador (aaaa-mm-dd), no horário local.
function dataTexto(data) {
  const ano = data.getFullYear()
  const mes = String(data.getMonth() + 1).padStart(2, '0')
  const dia = String(data.getDate()).padStart(2, '0')
  return `${ano}-${mes}-${dia}`
}

// Hora no formato do campo de hora (hh:mm), no horário local.
function horaTexto(data) {
  return `${String(data.getHours()).padStart(2, '0')}:${String(data.getMinutes()).padStart(2, '0')}`
}

function daquiA(dias) {
  const data = new Date()
  data.setDate(data.getDate() + dias)
  return dataTexto(data)
}

const hojeTexto = dataTexto(new Date())

const ATALHOS_PRAZO = [
  { texto: 'Hoje', data: () => daquiA(0) },
  { texto: 'Amanhã', data: () => daquiA(1) },
  { texto: 'Em 1 semana', data: () => daquiA(7) },
]

const ATALHOS_TEMPO = [15, 30, 60, 120, 240]

const categorias = ref([])
const carregandoCategorias = ref(true)

const titulo = ref('')
const descricao = ref('')
const categoriaId = ref(null)
const prazo = ref('')
const hora = ref('')
const horas = ref(null)
const minutos = ref(null)
const criticidade = ref(null)
const complexidade = ref(null)
const prioridade = ref(null)

const erro = ref('')
const salvando = ref(false)

// Modo edição (US05)
const tarefaOriginal = ref(null)
const carregandoTarefa = ref(false)
const prazoOriginal = ref({ dia: '', hora: '' })
const confirmandoExclusao = ref(false)
const excluindo = ref(false)

const prazoMudou = computed(
  () => !modoEdicao.value || prazo.value !== prazoOriginal.value.dia || hora.value !== prazoOriginal.value.hora,
)
const quantidadeSubtarefas = computed(() => tarefaOriginal.value?.subtarefasIds?.length ?? 0)
const avisoAtrasada = computed(
  () => modoEdicao.value && tarefaOriginal.value && new Date(tarefaOriginal.value.prazo) < new Date(),
)
// Uma tarefa atrasada continua mostrando o dia original no campo de data.
const dataMinima = computed(() =>
  modoEdicao.value && prazoOriginal.value.dia && prazoOriginal.value.dia < hojeTexto ? prazoOriginal.value.dia : hojeTexto,
)

const totalMinutos = computed(() => (horas.value ?? 0) * 60 + (minutos.value ?? 0))

function definirTempo(total) {
  horas.value = Math.floor(total / 60)
  minutos.value = total % 60
}

onMounted(async () => {
  if (modoEdicao.value) carregandoTarefa.value = true
  try {
    const [listaCategorias, tarefa] = await Promise.all([
      api('/categorias'),
      modoEdicao.value ? api(`/tarefas/${props.id}`) : null,
    ])
    categorias.value = listaCategorias
    if (tarefa) preencher(tarefa)
  } catch (e) {
    erro.value = e.status === 404 ? 'Tarefa não encontrada.' : e.message
  } finally {
    carregandoCategorias.value = false
    carregandoTarefa.value = false
  }
})

// Coloca os dados da tarefa nos campos (modo edição).
function preencher(tarefa) {
  tarefaOriginal.value = tarefa
  const dataPrazo = new Date(tarefa.prazo)
  prazoOriginal.value = { dia: dataTexto(dataPrazo), hora: horaTexto(dataPrazo) }

  titulo.value = tarefa.titulo
  descricao.value = tarefa.descricao ?? ''
  categoriaId.value = tarefa.categoriaId
  prazo.value = prazoOriginal.value.dia
  hora.value = prazoOriginal.value.hora
  definirTempo(tarefa.estimativaMin)
  criticidade.value = tarefa.criticidade
  complexidade.value = tarefa.complexidade
  prioridade.value = tarefa.prioridade
}

// Campo de hora: aceita só números e coloca os dois-pontos sozinho (1800 vira 18:00).
function formatarHora(evento) {
  const digitos = evento.target.value.replace(/\D/g, '').slice(0, 4)
  hora.value = digitos.length > 2 ? `${digitos.slice(0, 2)}:${digitos.slice(2)}` : digitos
  evento.target.value = hora.value
}

function horaValida(texto) {
  if (!/^\d{2}:\d{2}$/.test(texto)) return false
  const [h, m] = texto.split(':').map(Number)
  return h <= 23 && m <= 59
}

// Junta o dia com a hora (ou 23:59, se a hora ficar em branco), no horário local.
// Na edição, se o dia e a hora não mudaram, devolve o prazo gravado sem mexer nos segundos.
function prazoParaEnviar() {
  if (!prazoMudou.value) return tarefaOriginal.value.prazo
  return new Date(`${prazo.value}T${hora.value || '23:59'}:00`).toISOString()
}

// Confere no navegador o que dá para conferir; o resto (regras da classe Tarefa) a API responde.
function conferir() {
  if (!titulo.value.trim()) return 'O título é obrigatório.'
  if (!categoriaId.value) return 'Escolha uma categoria.'
  if (!prazo.value) return 'O prazo é obrigatório.'
  if (prazoMudou.value && prazo.value < hojeTexto) return 'O prazo não pode ser uma data que já passou.'
  if (hora.value && !horaValida(hora.value)) return 'Hora inválida. Digite só os números, ex.: 1800 para 18:00.'
  if (totalMinutos.value <= 0) return 'O tempo estimado é obrigatório.'
  if (!criticidade.value) return 'Informe a criticidade.'
  if (!complexidade.value) return 'Informe a complexidade.'
  if (!prioridade.value) return 'Informe a prioridade.'
  return ''
}

async function salvar() {
  erro.value = conferir()
  if (erro.value) {
    window.scrollTo({ top: 0, behavior: 'smooth' })
    return
  }

  salvando.value = true
  try {
    const tarefa = await api(modoEdicao.value ? `/tarefas/${props.id}` : '/tarefas', {
      metodo: modoEdicao.value ? 'PUT' : 'POST',
      corpo: {
        titulo: titulo.value.trim(),
        descricao: descricao.value.trim() || null,
        prazo: prazoParaEnviar(),
        estimativaMin: totalMinutos.value,
        categoriaId: categoriaId.value,
        criticidade: criticidade.value,
        complexidade: complexidade.value,
        prioridade: prioridade.value,
      },
    })
    const resumo = modoEdicao.value ? 'Tarefa atualizada' : 'Tarefa cadastrada'
    toast.add({ severity: 'success', summary: resumo, detail: tarefa.titulo, life: 3000 })
    router.push({ name: 'tarefas' })
  } catch (e) {
    erro.value = e.message
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    salvando.value = false
  }
}

async function excluir() {
  excluindo.value = true
  try {
    await api(`/tarefas/${props.id}`, { metodo: 'DELETE' })
    toast.add({ severity: 'success', summary: 'Tarefa excluída', detail: tarefaOriginal.value.titulo, life: 3000 })
    router.push({ name: 'tarefas' })
  } catch (e) {
    confirmandoExclusao.value = false
    erro.value = e.message
    window.scrollTo({ top: 0, behavior: 'smooth' })
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
}

.fechar {
  width: 44px;
  height: 44px;
  margin-left: -10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--sgt-texto);
  text-decoration: none;
}

.fechar .pi {
  font-size: 18px;
}

h1 {
  margin: 0;
  font-size: 18px;
}

.espaco {
  width: 34px;
}

.formulario {
  display: flex;
  flex-direction: column;
  gap: 18px;
  margin-top: 10px;
}

.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.chip {
  height: 38px;
  padding: 0 12px;
  border-radius: 999px;
  border: 1px solid #c9d1da;
  background: #fff;
  color: var(--sgt-texto);
  font-family: inherit;
  font-size: 14px;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
}

.chip.selecionado {
  border-color: var(--sgt-azul);
  background: var(--sgt-azul);
  color: #fff;
}

.chip:focus-visible {
  outline: 2px solid var(--sgt-azul);
  outline-offset: 2px;
}

.prazo-hora {
  display: grid;
  grid-template-columns: 3fr 2fr;
  gap: 10px;
}

.rotulo-pequeno {
  font-size: 13px !important;
  font-weight: 500 !important;
  color: var(--sgt-texto-suave);
}

.prazo-hora :deep(input) {
  height: 44px;
  width: 100%;
  font-family: inherit;
}

.atalhos {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.atalhos .chip {
  height: 34px;
}

.tempo {
  display: flex;
  align-items: center;
  gap: 10px;
}

.tempo :deep(.numero) {
  width: 80px;
}

/* SelectButton em grade, como os botões do protótipo. */
.grade {
  display: grid;
  gap: 6px;
}

.grade-4 {
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

.grade-3 {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.grade-2 {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.grade :deep(.p-togglebutton) {
  border-radius: 10px !important;
  min-height: 42px;
  padding: 0;
  background: #fff;
  border: 1px solid #c9d1da !important;
  color: var(--sgt-texto);
  font-weight: 600;
}

.grade :deep(.p-togglebutton-content) {
  width: 100%;
  height: 100%;
  justify-content: center;
  padding: 6px 8px;
  background: transparent !important;
  box-shadow: none !important;
  color: inherit !important;
  text-align: center;
}

.grade :deep(.p-togglebutton.p-togglebutton-checked) {
  background: var(--sgt-azul);
  border-color: var(--sgt-azul) !important;
  color: #fff;
}

.acoes {
  margin-top: 28px;
  display: grid;
  grid-template-columns: 1fr 2fr;
  gap: 10px;
}

.rodape {
  margin-top: 12px;
  text-align: center;
}

.carregando {
  display: flex;
  justify-content: center;
  padding: 48px 0;
}

.excluir {
  margin-top: 16px;
  align-self: center;
}

.texto-dialogo {
  margin: 0 0 10px;
  line-height: 1.45;
}
</style>
