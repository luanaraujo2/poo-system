<!-- US14 - Ver relatório de desempenho: entregues no prazo x atrasadas, tempo estimado x real
     e distribuição das tarefas por categoria. -->
<template>
  <main class="tela">
    <header class="topo">
      <RouterLink :to="{ name: 'tarefas' }" class="voltar" aria-label="Voltar">
        <i class="pi pi-arrow-left" />
      </RouterLink>
      <h1>Relatório de desempenho</h1>
      <span class="espaco" />
    </header>

    <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>

    <div v-if="carregando" class="carregando">
      <ProgressSpinner style="width: 40px; height: 40px" stroke-width="4" />
    </div>

    <section v-else-if="relatorio && relatorio.prazos.totalTarefas === 0" class="vazio">
      <i class="pi pi-chart-bar" />
      <span class="vazio-titulo">Nenhuma tarefa cadastrada ainda</span>
      <span class="dica">Cadastre e conclua tarefas para acompanhar prazos, tempo real e categorias.</span>
    </section>

    <template v-else-if="relatorio">
      <section class="cartao">
        <h2>Prazos e entregas</h2>
        <div class="grade-numeros">
          <div class="indicador verde">
            <span class="valor">{{ relatorio.prazos.entreguesNoPrazo }}</span>
            <span class="legenda">Entregues no prazo</span>
          </div>
          <div class="indicador vermelho">
            <span class="valor">{{ relatorio.prazos.entreguesAtrasadas }}</span>
            <span class="legenda">Entregues atrasadas</span>
          </div>
          <div class="indicador azul">
            <span class="valor">{{ relatorio.prazos.abertasNoPrazo }}</span>
            <span class="legenda">Abertas no prazo</span>
          </div>
          <div class="indicador alerta">
            <span class="valor">{{ relatorio.prazos.abertasAtrasadas }}</span>
            <span class="legenda">Abertas atrasadas</span>
          </div>
        </div>

        <div v-if="relatorio.prazos.concluidas > 0" class="pontualidade">
          <div class="linha-info">
            <span>Pontualidade nas entregas</span>
            <strong>{{ formatarPercentual(relatorio.prazos.taxaPontualidadePercentual) }}%</strong>
          </div>
          <div class="barra-fundo">
            <div class="barra-verde" :style="{ width: `${relatorio.prazos.taxaPontualidadePercentual}%` }" />
          </div>
          <span class="dica">
            {{ relatorio.prazos.entreguesNoPrazo }} de {{ relatorio.prazos.concluidas }}
            {{ relatorio.prazos.concluidas === 1 ? 'tarefa concluída' : 'tarefas concluídas' }} até o prazo.
            <template v-if="relatorio.prazos.canceladas > 0">
              · {{ relatorio.prazos.canceladas }} {{ relatorio.prazos.canceladas === 1 ? 'cancelada' : 'canceladas' }}.
            </template>
          </span>
        </div>
        <p v-else class="dica sem-margem">
          Nenhuma tarefa concluída ainda.
          <template v-if="relatorio.prazos.canceladas > 0">
            {{ relatorio.prazos.canceladas }} {{ relatorio.prazos.canceladas === 1 ? 'tarefa cancelada' : 'tarefas canceladas' }}.
          </template>
        </p>
      </section>

      <section class="cartao">
        <div class="cabecalho-cartao">
          <h2>Tempo estimado × real</h2>
          <Tag
            v-if="relatorio.tempoResumo.tarefasConcluidas > 0"
            :value="textoDiferenca(relatorio.tempoResumo.diferencaMin)"
            :severity="relatorio.tempoResumo.diferencaMin > 0 ? 'warn' : 'success'"
          />
        </div>

        <p v-if="relatorio.tempoResumo.tarefasConcluidas === 0" class="dica sem-margem">
          Conclua pelo menos uma tarefa informando o tempo gasto para comparar com a estimativa.
        </p>

        <template v-else>
          <div class="comparacao-barras">
            <div class="linha-barra">
              <div class="linha-info">
                <span>Estimado</span>
                <strong>{{ formatarDuracao(relatorio.tempoResumo.tempoEstimadoTotalMin) }}</strong>
              </div>
              <div class="barra-fundo">
                <div class="barra-azul" :style="{ width: larguraBarraTempo(relatorio.tempoResumo.tempoEstimadoTotalMin) }" />
              </div>
            </div>

            <div class="linha-barra">
              <div class="linha-info">
                <span>Tempo real gasto</span>
                <strong>{{ formatarDuracao(relatorio.tempoResumo.tempoRealTotalMin) }}</strong>
              </div>
              <div class="barra-fundo">
                <div
                  :class="relatorio.tempoResumo.diferencaMin > 0 ? 'barra-alerta' : 'barra-verde'"
                  :style="{ width: larguraBarraTempo(relatorio.tempoResumo.tempoRealTotalMin) }"
                />
              </div>
            </div>
          </div>

          <ul class="lista-tempos">
            <li v-for="item in relatorio.tempoPorTarefa" :key="item.tarefaId" class="item-tempo">
              <div class="item-tempo-topo">
                <RouterLink :to="{ name: 'editar-tarefa', params: { id: item.tarefaId } }" class="titulo-tarefa">
                  {{ item.titulo }}
                </RouterLink>
                <Tag :value="textoDiferencaCurto(item.diferencaMin)" :severity="item.diferencaMin > 0 ? 'warn' : 'success'" />
              </div>
              <div class="detalhes">
                <span class="detalhe">
                  <span class="ponto-cor" :style="{ background: item.categoriaCor }" />
                  {{ item.categoria }}
                </span>
                <span>Est.: {{ formatarDuracao(item.estimativaMin) }} · Real: {{ formatarDuracao(item.tempoRealMin) }}</span>
                <span :class="item.noPrazo ? 'texto-verde' : 'texto-vermelho'">
                  {{ item.noPrazo ? 'No prazo' : 'Com atraso' }}
                </span>
              </div>
            </li>
          </ul>
        </template>
      </section>

      <section class="cartao">
        <h2>Distribuição por categoria</h2>

        <div class="barra-empilhada" role="img" aria-label="Proporção de tarefas por categoria">
          <div
            v-for="cat in categoriasComTarefas"
            :key="cat.categoriaId"
            class="segmento"
            :style="{ width: `${cat.percentual}%`, background: cat.cor }"
            :title="`${cat.nome}: ${cat.quantidade}`"
          />
        </div>

        <ul class="lista-categorias">
          <li v-for="cat in relatorio.porCategoria" :key="cat.categoriaId" class="item-categoria">
            <div class="linha-info">
              <span class="nome-categoria">
                <span class="ponto-cor" :style="{ background: cat.cor }" />
                {{ cat.nome }}
              </span>
              <span class="contagem">
                {{ cat.quantidade }} {{ cat.quantidade === 1 ? 'tarefa' : 'tarefas' }} ({{ formatarPercentual(cat.percentual) }}%)
              </span>
            </div>
            <div class="barra-fundo">
              <div class="barra-categoria" :style="{ width: `${cat.percentual}%`, background: cat.cor }" />
            </div>
            <span v-if="cat.quantidade > 0" class="dica">
              {{ cat.concluidas }} concluída(s) · {{ cat.abertas }} aberta(s)
              <template v-if="cat.atrasadas > 0"> · {{ cat.atrasadas }} atrasada(s)</template>
              · Est. {{ formatarDuracao(cat.tempoEstimadoMin) }}
              <template v-if="cat.tempoRealMin > 0"> · Real {{ formatarDuracao(cat.tempoRealMin) }}</template>
            </span>
          </li>
        </ul>
      </section>
    </template>
  </main>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import Tag from 'primevue/tag'

import { api } from '../servicos/api'
import { formatarDuracao } from '../constantes'

const relatorio = ref(null)
const carregando = ref(true)
const erro = ref('')

const categoriasComTarefas = computed(() =>
  (relatorio.value?.porCategoria ?? []).filter((c) => c.quantidade > 0),
)

function formatarPercentual(valor) {
  return Number.isInteger(valor) ? String(valor) : valor.toFixed(1).replace('.', ',')
}

function larguraBarraTempo(minutos) {
  const resumo = relatorio.value?.tempoResumo
  if (!resumo) return '0%'
  const maximo = Math.max(resumo.tempoEstimadoTotalMin, resumo.tempoRealTotalMin, 1)
  return `${Math.round((minutos / maximo) * 100)}%`
}

function textoDiferenca(diferencaMin) {
  if (diferencaMin === 0) return 'No tempo estimado'
  const duracao = formatarDuracao(Math.abs(diferencaMin))
  return diferencaMin > 0 ? `+${duracao} acima do estimado` : `${duracao} abaixo do estimado`
}

function textoDiferencaCurto(diferencaMin) {
  if (diferencaMin === 0) return 'Exato'
  const duracao = formatarDuracao(Math.abs(diferencaMin))
  return diferencaMin > 0 ? `+${duracao}` : `-${duracao}`
}

onMounted(async () => {
  try {
    relatorio.value = await api('/relatorios/desempenho')
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

.cartao {
  background: #fff;
  border: 1px solid var(--sgt-borda);
  border-radius: 12px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 14px;
}

h2 {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--sgt-texto-suave);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.cabecalho-cartao {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  flex-wrap: wrap;
}

.grade-numeros {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
}

.indicador {
  padding: 12px;
  border-radius: 10px;
  background: var(--sgt-fundo);
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.indicador .valor {
  font-size: 22px;
  font-weight: 700;
}

.indicador .legenda {
  font-size: 13px;
  color: var(--sgt-texto-suave);
}

.indicador.verde {
  background: #ecfdf5;
  color: #065f46;
}

.indicador.vermelho {
  background: #fef2f2;
  color: #991b1b;
}

.indicador.azul {
  background: #eff6ff;
  color: #1e40af;
}

.indicador.alerta {
  background: #fffbeb;
  color: #92400e;
}

.pontualidade,
.comparacao-barras {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.linha-barra {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.linha-info {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  font-size: 14px;
}

.barra-fundo {
  height: 8px;
  background: #e5e7eb;
  border-radius: 999px;
  overflow: hidden;
}

.barra-verde,
.barra-azul,
.barra-alerta,
.barra-categoria {
  height: 100%;
  border-radius: 999px;
  transition: width 0.2s ease;
}

.barra-verde {
  background: #10b981;
}

.barra-azul {
  background: var(--sgt-azul);
}

.barra-alerta {
  background: #f59e0b;
}

.lista-tempos,
.lista-categorias {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.lista-tempos {
  border-top: 1px solid var(--sgt-borda);
  padding-top: 12px;
}

.item-tempo {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding-bottom: 10px;
  border-bottom: 1px solid var(--sgt-borda);
}

.item-tempo:last-child {
  padding-bottom: 0;
  border-bottom: none;
}

.item-tempo-topo {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.titulo-tarefa {
  font-weight: 600;
  font-size: 14px;
  color: var(--sgt-texto);
  text-decoration: none;
  overflow-wrap: anywhere;
}

.titulo-tarefa:hover {
  text-decoration: underline;
}

.detalhes {
  display: flex;
  flex-wrap: wrap;
  gap: 4px 10px;
  font-size: 13px;
  color: var(--sgt-texto-suave);
}

.detalhe {
  display: flex;
  align-items: center;
  gap: 5px;
}

.texto-verde {
  color: #047857;
  font-weight: 600;
}

.texto-vermelho {
  color: #b91c1c;
  font-weight: 600;
}

.barra-empilhada {
  height: 12px;
  border-radius: 999px;
  overflow: hidden;
  background: #e5e7eb;
  display: flex;
}

.segmento {
  height: 100%;
}

.item-categoria {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.nome-categoria {
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 600;
}

.contagem {
  font-size: 13px;
  color: var(--sgt-texto-suave);
}

.sem-margem {
  margin: 0;
}
</style>
