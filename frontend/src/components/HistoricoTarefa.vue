<!-- US12 - Histórico de status da tarefa. Mostra cada mudança com data e hora e quanto tempo
     a tarefa ficou em cada status, para dar para ver onde ela travou. -->
<template>
  <section class="cartao-historico" aria-labelledby="rotulo-historico">
    <div class="cabecalho">
      <span id="rotulo-historico" class="rotulo">Histórico</span>
      <span v-if="maisTempoParada" class="dica">
        Mais tempo em {{ STATUS[maisTempoParada.status] }}: {{ formatarIntervalo(maisTempoParada.ms) }}
      </span>
    </div>

    <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>
    <p v-else-if="carregando" class="dica">Carregando histórico…</p>
    <p v-else-if="!registros.length" class="dica">Nenhuma mudança registrada.</p>

    <ol v-else class="linha-tempo">
      <li v-for="(registro, indice) in registros" :key="indice" class="registro">
        <span class="marcador" :class="`marcador-${registro.statusNovo}`" aria-hidden="true" />
        <div class="texto">
          <span class="mudanca">
            <template v-if="registro.statusAnterior">
              {{ STATUS[registro.statusAnterior] }} <i class="pi pi-arrow-right" aria-hidden="true" /> {{ STATUS[registro.statusNovo] }}
            </template>
            <template v-else>Cadastrada como {{ STATUS[registro.statusNovo] }}</template>
          </span>
          <span class="dica">{{ formatarPrazo(registro.alteradoEm) }}</span>
          <span v-if="registro.tempoNoStatus !== null" class="dica">
            {{ indice === registros.length - 1 ? 'Está assim há' : 'Ficou assim por' }} {{ formatarIntervalo(registro.tempoNoStatus) }}
          </span>
        </div>
      </li>
    </ol>
  </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import Message from 'primevue/message'

import { api } from '../servicos/api'
import { STATUS, formatarPrazo } from '../constantes'

const props = defineProps({ tarefa: { type: Object, required: true } })

const lista = ref([])
const carregando = ref(true)
const erro = ref('')

const FINAIS = ['Concluida', 'Cancelada']

// Tempo em cada status: até a próxima mudança ou, no último, até agora (se a tarefa continua aberta).
const registros = computed(() =>
  lista.value.map((registro, indice) => {
    const proximo = lista.value[indice + 1]
    let tempoNoStatus = null
    if (proximo) tempoNoStatus = new Date(proximo.alteradoEm) - new Date(registro.alteradoEm)
    else if (!FINAIS.includes(registro.statusNovo)) tempoNoStatus = Date.now() - new Date(registro.alteradoEm)
    return { ...registro, tempoNoStatus }
  }),
)

const maisTempoParada = computed(() => {
  let maior = null
  for (const registro of registros.value) {
    if (registro.tempoNoStatus === null) continue
    if (!maior || registro.tempoNoStatus > maior.ms) maior = { status: registro.statusNovo, ms: registro.tempoNoStatus }
  }
  return maior && maior.ms >= 60000 ? maior : null
})

function formatarIntervalo(ms) {
  const minutosTotais = Math.max(0, Math.floor(ms / 60000))
  const dias = Math.floor(minutosTotais / 1440)
  const horas = Math.floor((minutosTotais % 1440) / 60)
  const minutos = minutosTotais % 60
  if (dias) return horas ? `${dias} ${dias === 1 ? 'dia' : 'dias'} e ${horas}h` : `${dias} ${dias === 1 ? 'dia' : 'dias'}`
  if (horas) return minutos ? `${horas}h ${minutos}min` : `${horas}h`
  return minutos ? `${minutos}min` : 'menos de 1min'
}

async function carregar() {
  erro.value = ''
  try {
    lista.value = await api(`/tarefas/${props.tarefa.id}/historico`)
  } catch (e) {
    erro.value = e.message
  } finally {
    carregando.value = false
  }
}

// Recarrega quando o status muda no quadro de status (US07).
watch(() => [props.tarefa.id, props.tarefa.status], carregar, { immediate: true })
</script>

<style scoped>
.cartao-historico {
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
  align-items: baseline;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 4px 10px;
}

.rotulo {
  font-size: 14px;
  font-weight: 600;
}

.cartao-historico > .dica {
  margin: 0;
}

.linha-tempo {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
}

.registro {
  display: flex;
  gap: 12px;
  position: relative;
  padding-bottom: 12px;
}

.registro:last-child {
  padding-bottom: 0;
}

/* Linha vertical ligando os registros. */
.registro:not(:last-child)::before {
  content: '';
  position: absolute;
  left: 5px;
  top: 14px;
  bottom: 0;
  width: 2px;
  background: var(--sgt-borda);
}

.marcador {
  flex: none;
  width: 12px;
  height: 12px;
  margin-top: 4px;
  border-radius: 50%;
  background: #9aa6b2;
}

.marcador-EmAndamento {
  background: var(--sgt-azul);
}

.marcador-Concluida {
  background: #16a34a;
}

.marcador-Cancelada {
  background: #334155;
}

.texto {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.mudanca {
  font-size: 14px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 6px;
}

.mudanca .pi {
  font-size: 11px;
  color: var(--sgt-texto-suave);
}
</style>
