<!-- US07 - Atualizar status. Fica no topo da tela "Editar tarefa" e grava na hora, sem precisar do botão Salvar. -->
<template>
  <section class="cartao-status" aria-labelledby="rotulo-status">
    <div class="cabecalho">
      <span id="rotulo-status" class="rotulo">Status</span>
      <Tag :value="STATUS[tarefa.status]" :severity="corDoStatus(tarefa.status)" />
    </div>

    <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>

    <SelectButton
      :model-value="tarefa.status"
      :options="STATUS_OPCOES"
      option-label="texto"
      option-value="valor"
      :allow-empty="false"
      :disabled="concluida || salvando"
      aria-labelledby="rotulo-status"
      class="grade"
      @update:model-value="escolher"
    />

    <p v-if="concluida" class="dica">
      Concluída em {{ formatarPrazo(tarefa.concluidoEm) }}. Tempo real: {{ formatarDuracao(tarefa.tempoRealMin) }}
      (estimado: {{ formatarDuracao(tarefa.estimativaMin) }}). Tarefa concluída não muda mais de status.
    </p>
    <p v-else-if="subtarefasAbertas" class="dica">
      Para concluir, conclua antes {{ subtarefasAbertas === 1 ? 'a subtarefa' : `as ${subtarefasAbertas} subtarefas` }}.
    </p>

    <!-- Ao concluir, o usuário informa quanto tempo levou de verdade. -->
    <Dialog
      v-model:visible="perguntandoTempo"
      modal
      header="Concluir tarefa"
      :style="{ width: 'min(92vw, 400px)' }"
      :draggable="false"
    >
      <form id="form-concluir" class="formulario" @submit.prevent="concluir">
        <p class="texto-dialogo">Quanto tempo você levou de verdade? O estimado era {{ formatarDuracao(tarefa.estimativaMin) }}.</p>
        <Message v-if="erroTempo" severity="error" :closable="false">{{ erroTempo }}</Message>
        <div class="tempo">
          <InputNumber v-model="horas" :min="0" :max="999" input-class="numero" placeholder="0" aria-label="Horas" />
          <span class="dica">h</span>
          <InputNumber v-model="minutos" :min="0" :max="59" input-class="numero" placeholder="0" aria-label="Minutos" />
          <span class="dica">min</span>
        </div>
      </form>
      <template #footer>
        <Button label="Cancelar" severity="secondary" outlined @click="perguntandoTempo = false" />
        <Button type="submit" form="form-concluir" label="Concluir" icon="pi pi-check" :loading="salvando" />
      </template>
    </Dialog>
  </section>
</template>

<script setup>
import { computed, ref } from 'vue'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import Message from 'primevue/message'
import SelectButton from 'primevue/selectbutton'
import Tag from 'primevue/tag'
import { useToast } from 'primevue/usetoast'

import { api } from '../servicos/api'
import { STATUS, STATUS_OPCOES, corDoStatus, formatarDuracao, formatarPrazo } from '../constantes'

const props = defineProps({ tarefa: { type: Object, required: true } })
const emit = defineEmits(['atualizada'])

const toast = useToast()

const erro = ref('')
const salvando = ref(false)
const perguntandoTempo = ref(false)
const erroTempo = ref('')
const horas = ref(null)
const minutos = ref(null)

const concluida = computed(() => props.tarefa.status === 'Concluida')
const subtarefasAbertas = computed(
  () => (props.tarefa.subtarefasIds?.length ?? 0) - (props.tarefa.subtarefasConcluidas ?? 0),
)

function escolher(status) {
  if (!status || status === props.tarefa.status) return
  erro.value = ''
  if (status === 'Concluida') {
    // Já sugere o tempo estimado; o usuário corrige se levou mais ou menos.
    horas.value = Math.floor(props.tarefa.estimativaMin / 60)
    minutos.value = props.tarefa.estimativaMin % 60
    erroTempo.value = ''
    perguntandoTempo.value = true
    return
  }
  enviar({ status })
}

async function concluir() {
  const total = (horas.value ?? 0) * 60 + (minutos.value ?? 0)
  if (total <= 0) {
    erroTempo.value = 'Informe o tempo real gasto.'
    return
  }
  const deuCerto = await enviar({ status: 'Concluida', tempoRealMin: total })
  if (deuCerto) perguntandoTempo.value = false
}

async function enviar(corpo) {
  salvando.value = true
  try {
    const tarefa = await api(`/tarefas/${props.tarefa.id}/status`, { metodo: 'PATCH', corpo })
    toast.add({ severity: 'success', summary: `Status: ${STATUS[tarefa.status]}`, detail: tarefa.titulo, life: 3000 })
    emit('atualizada', tarefa)
    return true
  } catch (e) {
    // Ex.: concluir com subtarefas abertas. No diálogo, a mensagem aparece lá dentro.
    if (perguntandoTempo.value) erroTempo.value = e.message
    else erro.value = e.message
    return false
  } finally {
    salvando.value = false
  }
}
</script>

<style scoped>
.cartao-status {
  background: #fff;
  border: 1px solid var(--sgt-borda);
  border-radius: 12px;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.cabecalho {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.rotulo {
  font-size: 14px;
  font-weight: 600;
}

.cartao-status > .dica {
  margin: 0;
  line-height: 1.4;
}

.grade {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 6px;
}

.grade :deep(.p-togglebutton) {
  border-radius: 10px !important;
  min-height: 40px;
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
}

.grade :deep(.p-togglebutton.p-togglebutton-checked) {
  background: var(--sgt-azul);
  border-color: var(--sgt-azul) !important;
  color: #fff;
}

.formulario {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.texto-dialogo {
  margin: 0;
  line-height: 1.45;
}

.tempo {
  display: flex;
  align-items: center;
  gap: 10px;
}

.tempo :deep(.numero) {
  width: 80px;
}
</style>
