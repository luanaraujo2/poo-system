<!-- US12 - Consultar histórico: exibe o histórico de mudanças de status de cada tarefa,
     mostrando data, hora e quanto tempo ela ficou em cada etapa para identificar onde travou. -->
<template>
  <main class="tela">
    <header class="topo">
      <RouterLink :to="{ name: 'tarefas' }" class="voltar" aria-label="Voltar">
        <i class="pi pi-arrow-left" />
      </RouterLink>
      <h1>Histórico das tarefas</h1>
      <span class="espaco" />
    </header>

    <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>

    <div v-if="carregando" class="carregando">
      <ProgressSpinner style="width: 40px; height: 40px" stroke-width="4" />
    </div>

    <section v-else-if="tarefas.length === 0 && !erro" class="vazio">
      <i class="pi pi-history" />
      <span class="vazio-titulo">Nenhuma tarefa registrada</span>
      <span class="dica">Quando você cadastrar tarefas e alterar os status, todo o histórico aparecerá aqui.</span>
    </section>

    <template v-else>
      <SelectButton
        v-model="filtroStatus"
        :options="FILTROS"
        option-label="texto"
        option-value="valor"
        :allow-empty="false"
        aria-label="Filtrar histórico por situação"
        class="filtro"
      />

      <div class="campo selecao-tarefa">
        <label for="filtro-tarefa-historico">Tarefa</label>
        <Select
          v-model="tarefaSelecionada"
          input-id="filtro-tarefa-historico"
          :options="opcoesTarefas"
          option-label="titulo"
          option-value="id"
          placeholder="Todas as tarefas"
          fluid
        />
      </div>

      <p v-if="visiveis.length === 0" class="dica sem-resultado">
        Nenhuma tarefa corresponde ao filtro selecionado.
      </p>

      <ul v-else class="lista-tarefas">
        <li v-for="tarefa in visiveis" :key="tarefa.tarefaId" class="cartao">
          <div class="cartao-topo">
            <div class="titulo-grupo">
              <RouterLink :to="{ name: 'editar-tarefa', params: { id: tarefa.tarefaId } }" class="titulo">
                {{ tarefa.titulo }}
              </RouterLink>
              <span class="dica categoria">
                <span class="ponto-cor" :style="{ background: tarefa.categoriaCor }" />
                {{ tarefa.categoria }}
                <template v-if="tarefa.tarefaPaiId"> · Subtarefa</template>
              </span>
            </div>
            <Tag :value="STATUS[tarefa.statusAtual] ?? tarefa.statusAtual" :severity="corDoStatus(tarefa.statusAtual)" />
          </div>

          <p v-if="tarefa.gargalo" class="gargalo">
            <i class="pi pi-clock" aria-hidden="true" />
            Mais tempo em <strong>{{ STATUS[tarefa.gargalo.status] }}</strong>: {{ formatarIntervalo(tarefa.gargalo.ms) }}
          </p>

          <ol v-if="tarefa.registros.length" class="linha-tempo">
            <li v-for="(registro, indice) in tarefa.registros" :key="indice" class="registro">
              <span class="marcador" :class="`marcador-${registro.statusNovo}`" aria-hidden="true" />
              <div class="texto">
                <span class="mudanca">
                  <template v-if="registro.statusAnterior">
                    {{ STATUS[registro.statusAnterior] }} <i class="pi pi-arrow-right" aria-hidden="true" /> {{ STATUS[registro.statusNovo] }}
                  </template>
                  <template v-else>Cadastrada como {{ STATUS[registro.statusNovo] }}</template>
                </span>
                <span class="dica">{{ formatarDataHora(registro.alteradoEm) }}</span>
                <span v-if="registro.tempoNoStatus !== null" class="dica">
                  {{ indice === tarefa.registros.length - 1 ? 'Está assim há' : 'Ficou assim por' }} {{ formatarIntervalo(registro.tempoNoStatus) }}
                </span>
              </div>
            </li>
          </ol>
        </li>
      </ul>
    </template>
  </main>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import Select from 'primevue/select'
import SelectButton from 'primevue/selectbutton'
import Tag from 'primevue/tag'

import { api } from '../servicos/api'
import { STATUS, corDoStatus, formatarDataHora } from '../constantes'

const route = useRoute()

const FILTROS = [
  { valor: 'todas', texto: 'Todas' },
  { valor: 'abertas', texto: 'Em aberto' },
  { valor: 'finalizadas', texto: 'Concluídas / Canceladas' },
]

const FINAIS = ['Concluida', 'Cancelada']

const tarefas = ref([])
const carregando = ref(true)
const erro = ref('')
const filtroStatus = ref('todas')
const tarefaSelecionada = ref(route.query.tarefa ? Number(route.query.tarefa) : null)

function calcularEtapas(item) {
  const registros = (item.historico ?? []).map((registro, indice, lista) => {
    const proximo = lista[indice + 1]
    let tempoNoStatus = null
    if (proximo) tempoNoStatus = new Date(proximo.alteradoEm) - new Date(registro.alteradoEm)
    else if (!FINAIS.includes(registro.statusNovo)) tempoNoStatus = Date.now() - new Date(registro.alteradoEm)
    return { ...registro, tempoNoStatus }
  })

  let gargalo = null
  for (const registro of registros) {
    if (registro.tempoNoStatus === null) continue
    if (!gargalo || registro.tempoNoStatus > gargalo.ms) gargalo = { status: registro.statusNovo, ms: registro.tempoNoStatus }
  }

  return {
    ...item,
    registros,
    gargalo: gargalo && gargalo.ms >= 60000 ? gargalo : null,
  }
}

const tarefasComEtapas = computed(() => tarefas.value.map(calcularEtapas))

const filtradasPorStatus = computed(() => {
  if (filtroStatus.value === 'abertas') {
    return tarefasComEtapas.value.filter((t) => ['Pendente', 'EmAndamento'].includes(t.statusAtual))
  }
  if (filtroStatus.value === 'finalizadas') {
    return tarefasComEtapas.value.filter((t) => FINAIS.includes(t.statusAtual))
  }
  return tarefasComEtapas.value
})

const opcoesTarefas = computed(() => [
  { id: null, titulo: 'Todas as tarefas' },
  ...filtradasPorStatus.value.map((t) => ({ id: t.tarefaId, titulo: t.titulo })),
])

const visiveis = computed(() => {
  if (tarefaSelecionada.value === null) return filtradasPorStatus.value
  return filtradasPorStatus.value.filter((t) => t.tarefaId === tarefaSelecionada.value)
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

onMounted(async () => {
  try {
    tarefas.value = await api('/tarefas/historico')
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
  margin-bottom: 12px;
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

.vazio {
  margin-top: 24px;
  padding: 28px 18px;
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

.filtro {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-bottom: 12px;
}

.filtro :deep(.p-togglebutton) {
  padding: 0;
  min-height: 36px;
  font-size: 12px;
}

.filtro :deep(.p-togglebutton-content) {
  width: 100%;
  justify-content: center;
  padding: 4px;
  text-align: center;
}

.selecao-tarefa {
  margin-bottom: 16px;
}

.sem-resultado {
  text-align: center;
  margin-top: 24px;
}

.lista-tarefas {
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
  gap: 10px;
}

.cartao-topo {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.titulo-grupo {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.titulo {
  font-size: 16px;
  font-weight: 600;
  color: var(--sgt-texto);
  text-decoration: none;
  overflow-wrap: anywhere;
}

.titulo:hover {
  text-decoration: underline;
}

.categoria {
  display: flex;
  align-items: center;
  gap: 6px;
}

.gargalo {
  margin: 0;
  padding: 8px 10px;
  border-radius: 8px;
  background: #fffbeb;
  color: #92400e;
  font-size: 13px;
  display: flex;
  align-items: center;
  gap: 6px;
}

.linha-tempo {
  list-style: none;
  margin: 0;
  padding: 4px 0 0;
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
