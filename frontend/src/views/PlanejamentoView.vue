<!-- US11 - Gerar planejamento da semana: distribui as tarefas abertas nos blocos de tempo livre,
     garante que nenhuma tarefa fique depois do prazo, lista o que não couber em "sem espaço"
     e permite mover tarefas entre blocos. -->
<template>
  <main class="tela">
    <header class="topo">
      <RouterLink :to="{ name: 'tarefas' }" class="voltar" aria-label="Voltar">
        <i class="pi pi-arrow-left" />
      </RouterLink>
      <h1>Planejamento semanal</h1>
      <RouterLink :to="{ name: 'disponibilidade' }" class="atalho-topo" aria-label="Disponibilidade" title="Disponibilidade">
        <i class="pi pi-clock" />
      </RouterLink>
    </header>

    <SelectButton
      v-model="semanaSelecionada"
      :options="opcoesSemana"
      option-label="texto"
      option-value="valor"
      :allow-empty="false"
      aria-label="Selecionar semana"
      class="seletor-semana"
    />

    <Message v-if="erro" severity="error" :closable="false" class="aviso">{{ erro }}</Message>

    <div v-if="carregando" class="carregando">
      <ProgressSpinner style="width: 40px; height: 40px" stroke-width="4" />
    </div>

    <template v-else-if="planejamento">
      <section class="resumo">
        <div class="resumo-texto">
          <span class="periodo">
            {{ formatarDataCurta(planejamento.semanaInicio) }} a {{ formatarDataCurta(planejamento.semanaFim) }}
          </span>
          <span class="dica">
            Planejado: <strong>{{ formatarDuracao(planejamento.tempoPlanejadoMin) }}</strong> de
            {{ formatarDuracao(planejamento.tempoLivreTotalMin) }} disponíveis
          </span>
        </div>
        <Button
          :label="planejamento.tempoPlanejadoMin > 0 ? 'Regerar' : 'Gerar planejamento'"
          icon="pi pi-calendar-plus"
          :loading="gerando"
          :disabled="planejamento.blocos.length === 0"
          @click="gerar"
        />
      </section>

      <section v-if="planejamento.blocos.length === 0" class="vazio">
        <i class="pi pi-clock" />
        <span class="vazio-titulo">Nenhum horário cadastrado</span>
        <span class="dica">Cadastre seus blocos semanais de tempo livre para montar o planejamento.</span>
        <Button
          label="Cadastrar disponibilidade"
          icon="pi pi-arrow-right"
          severity="secondary"
          outlined
          @click="router.push({ name: 'disponibilidade' })"
        />
      </section>

      <template v-else>
        <section class="grupo">
          <h2>Blocos da semana</h2>
          <ul class="lista-blocos">
            <li
              v-for="bloco in planejamento.blocos"
              :key="bloco.disponibilidadeId"
              :class="['bloco', { passado: bloco.passado }]"
            >
              <div class="bloco-topo">
                <div class="bloco-titulo">
                  <span class="dia">{{ DIAS_SEMANA[bloco.diaSemana] }} · {{ formatarDataCurta(bloco.data) }}</span>
                  <span class="dica">{{ hora(bloco.horaInicio) }} – {{ hora(bloco.horaFim) }}</span>
                </div>
                <Tag v-if="bloco.passado" value="Já passou" severity="secondary" />
                <Tag v-else-if="bloco.tempoLivreMin === 0" value="Lotado" severity="warn" />
                <Tag v-else :value="`${formatarDuracao(bloco.tempoLivreMin)} livre(s)`" severity="success" />
              </div>

              <p v-if="bloco.tarefas.length === 0" class="dica bloco-vazio">
                {{ bloco.passado ? 'Horário já encerrado nesta semana.' : 'Nenhuma tarefa alocada neste bloco.' }}
              </p>

              <ul v-else class="lista-tarefas">
                <li v-for="tarefa in bloco.tarefas" :key="tarefa.tarefaId" class="item-tarefa">
                  <div class="item-principal">
                    <span class="horario-slot">{{ hora(tarefa.inicioPrevisto) }} – {{ hora(tarefa.fimPrevisto) }}</span>
                    <RouterLink :to="{ name: 'editar-tarefa', params: { id: tarefa.tarefaId } }" class="titulo-tarefa">
                      {{ tarefa.titulo }}
                    </RouterLink>
                    <div class="detalhes">
                      <span class="detalhe">
                        <span class="ponto-cor" :style="{ background: tarefa.categoriaCor }" />
                        {{ tarefa.categoria }}
                      </span>
                      <span class="detalhe"><i class="pi pi-clock" /> {{ formatarDuracao(tarefa.estimativaMin) }}</span>
                      <span class="detalhe"><i class="pi pi-calendar" /> Prazo {{ formatarPrazo(tarefa.prazo) }}</span>
                    </div>
                  </div>
                  <Button
                    label="Mover"
                    icon="pi pi-arrow-right-arrow-left"
                    severity="secondary"
                    text
                    size="small"
                    @click="abrirMover(tarefa, bloco.disponibilidadeId)"
                  />
                </li>
              </ul>
            </li>
          </ul>
        </section>

        <section class="grupo">
          <div class="cabecalho-sem-espaco">
            <h2>Sem espaço</h2>
            <Tag :value="String(planejamento.semEspaco.length)" :severity="planejamento.semEspaco.length ? 'warn' : 'secondary'" />
          </div>

          <Message
            v-if="planejamento.semEspaco.length === 0 && planejamento.tempoPlanejadoMin > 0"
            severity="success"
            :closable="false"
          >
            Todas as tarefas abertas couberam no planejamento desta semana.
          </Message>
          <p v-else-if="planejamento.semEspaco.length === 0" class="dica">
            Nenhuma tarefa aberta aguardando alocação.
          </p>

          <ul v-else class="lista-sem-espaco">
            <li v-for="tarefa in planejamento.semEspaco" :key="tarefa.tarefaId" class="cartao-sem-espaco">
              <div class="item-principal">
                <RouterLink :to="{ name: 'editar-tarefa', params: { id: tarefa.tarefaId } }" class="titulo-tarefa">
                  {{ tarefa.titulo }}
                </RouterLink>
                <div class="detalhes">
                  <span class="detalhe">
                    <span class="ponto-cor" :style="{ background: tarefa.categoriaCor }" />
                    {{ tarefa.categoria }}
                  </span>
                  <span class="detalhe"><i class="pi pi-clock" /> {{ formatarDuracao(tarefa.estimativaMin) }}</span>
                  <span class="detalhe"><i class="pi pi-calendar" /> Prazo {{ formatarPrazo(tarefa.prazo) }}</span>
                </div>
                <Tag :value="tarefa.motivo" severity="warn" class="etiqueta-motivo" />
              </div>
              <Button
                label="Alocar"
                icon="pi pi-arrow-right-arrow-left"
                severity="secondary"
                outlined
                size="small"
                @click="abrirMover(tarefa, null)"
              />
            </li>
          </ul>
        </section>
      </template>
    </template>

    <Dialog
      v-model:visible="dialogoMover"
      modal
      header="Mover tarefa"
      :style="{ width: 'min(92vw, 420px)' }"
      :draggable="false"
    >
      <template v-if="tarefaMover">
        <div class="resumo-mover">
          <strong>{{ tarefaMover.titulo }}</strong>
          <span class="dica">
            Duração: {{ formatarDuracao(tarefaMover.estimativaMin) }} · Prazo: {{ formatarPrazo(tarefaMover.prazo) }}
          </span>
        </div>

        <Message v-if="erroMover" severity="error" :closable="false" class="aviso-mover">{{ erroMover }}</Message>

        <ul class="opcoes-blocos">
          <li v-for="opcao in opcoesDestino" :key="opcao.bloco.disponibilidadeId">
            <button
              type="button"
              class="opcao-bloco"
              :disabled="!opcao.podeMover || movendo"
              @click="moverPara(opcao.bloco.disponibilidadeId)"
            >
              <div class="opcao-info">
                <span class="dia">{{ DIAS_SEMANA[opcao.bloco.diaSemana] }} · {{ formatarDataCurta(opcao.bloco.data) }}</span>
                <span class="dica">
                  {{ hora(opcao.bloco.horaInicio) }} – {{ hora(opcao.bloco.horaFim) }}
                  · {{ formatarDuracao(opcao.bloco.tempoLivreMin) }} livre(s)
                </span>
              </div>
              <Tag v-if="opcao.motivo" :value="opcao.motivo" severity="secondary" />
              <i v-else class="pi pi-chevron-right seta" />
            </button>
          </li>
        </ul>

        <Button
          v-if="blocoAtualId !== null"
          label="Tirar do bloco (mover para Sem espaço)"
          icon="pi pi-times-circle"
          severity="secondary"
          outlined
          :loading="movendo"
          class="largura-total botao-sem-espaco"
          @click="moverPara(null)"
        />
      </template>
    </Dialog>
  </main>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import SelectButton from 'primevue/selectbutton'
import Tag from 'primevue/tag'
import { useToast } from 'primevue/usetoast'

import { api } from '../servicos/api'
import { DIAS_SEMANA, formatarDataCurta, formatarDuracao, formatarPrazo } from '../constantes'

const router = useRouter()
const toast = useToast()

function inicioDaSemanaTexto(deslocamentoSemanas = 0) {
  const hoje = new Date()
  const diasDesdeSegunda = (hoje.getDay() + 6) % 7
  hoje.setDate(hoje.getDate() - diasDesdeSegunda + deslocamentoSemanas * 7)
  const ano = hoje.getFullYear()
  const mes = String(hoje.getMonth() + 1).padStart(2, '0')
  const dia = String(hoje.getDate()).padStart(2, '0')
  return `${ano}-${mes}-${dia}`
}

const semanaAtual = inicioDaSemanaTexto(0)
const proximaSemana = inicioDaSemanaTexto(1)

const opcoesSemana = [
  { valor: semanaAtual, texto: `Esta semana (${formatarDataCurta(semanaAtual)})` },
  { valor: proximaSemana, texto: `Próxima semana (${formatarDataCurta(proximaSemana)})` },
]

const semanaSelecionada = ref(semanaAtual)
const planejamento = ref(null)
const carregando = ref(true)
const gerando = ref(false)
const erro = ref('')

const dialogoMover = ref(false)
const tarefaMover = ref(null)
const blocoAtualId = ref(null)
const movendo = ref(false)
const erroMover = ref('')

const hora = (valor) => valor?.slice(0, 5) ?? ''

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    planejamento.value = await api(`/planejamento?semanaInicio=${semanaSelecionada.value}`)
  } catch (e) {
    erro.value = e.message
  } finally {
    carregando.value = false
  }
}

async function gerar() {
  gerando.value = true
  erro.value = ''
  try {
    planejamento.value = await api(`/planejamento/gerar?semanaInicio=${semanaSelecionada.value}`, {
      metodo: 'POST',
      corpo: { semanaInicio: semanaSelecionada.value },
    })
    const qtdSemEspaco = planejamento.value.semEspaco.length
    toast.add({
      severity: qtdSemEspaco ? 'warn' : 'success',
      summary: 'Planejamento gerado',
      detail: qtdSemEspaco
        ? `${qtdSemEspaco} ${qtdSemEspaco === 1 ? 'tarefa ficou' : 'tarefas ficaram'} sem espaço.`
        : 'Tarefas distribuídas nos blocos livres da semana.',
      life: 3500,
    })
  } catch (e) {
    erro.value = e.message
  } finally {
    gerando.value = false
  }
}

function abrirMover(tarefa, idBlocoAtual) {
  tarefaMover.value = tarefa
  blocoAtualId.value = idBlocoAtual
  erroMover.value = ''
  dialogoMover.value = true
}

function inicioPrevistoNoDestino(bloco) {
  const [hInicio, mInicio] = bloco.horaInicio.split(':').map(Number)
  const [ano, mes, dia] = bloco.data.split('-').map(Number)
  let base = new Date(ano, mes - 1, dia, hInicio, mInicio, 0, 0)

  if (bloco.tarefas.length > 0) {
    const ultima = bloco.tarefas[bloco.tarefas.length - 1]
    const [hFim, mFim] = ultima.fimPrevisto.split(':').map(Number)
    const fimUltima = new Date(ano, mes - 1, dia, hFim, mFim, 0, 0)
    if (fimUltima > base) base = fimUltima
  }

  const agora = new Date()
  if (agora > base) {
    base = new Date(agora.getFullYear(), agora.getMonth(), agora.getDate(), agora.getHours(), agora.getMinutes() + (agora.getSeconds() > 0 ? 1 : 0), 0, 0)
  }
  return base
}

const opcoesDestino = computed(() => {
  if (!planejamento.value || !tarefaMover.value) return []
  const prazoTarefa = new Date(tarefaMover.value.prazo)

  return planejamento.value.blocos
    .filter((b) => b.disponibilidadeId !== blocoAtualId.value)
    .map((bloco) => {
      if (bloco.passado) return { bloco, podeMover: false, motivo: 'Já passou' }
      if (bloco.tempoLivreMin < tarefaMover.value.estimativaMin) {
        return { bloco, podeMover: false, motivo: 'Sem espaço' }
      }
      const inicioPrev = inicioPrevistoNoDestino(bloco)
      const fimPrev = new Date(inicioPrev.getTime() + tarefaMover.value.estimativaMin * 60000)
      if (fimPrev > prazoTarefa) {
        return { bloco, podeMover: false, motivo: 'Passa do prazo' }
      }
      return { bloco, podeMover: true, motivo: '' }
    })
})

async function moverPara(destinoId) {
  if (!tarefaMover.value) return
  movendo.value = true
  erroMover.value = ''
  try {
    planejamento.value = await api(`/planejamento/tarefas/${tarefaMover.value.tarefaId}`, {
      metodo: 'PATCH',
      corpo: {
        disponibilidadeId: destinoId,
        semanaInicio: semanaSelecionada.value,
      },
    })
    dialogoMover.value = false
    toast.add({
      severity: 'success',
      summary: destinoId === null ? 'Movida para Sem espaço' : 'Tarefa movida',
      detail: tarefaMover.value.titulo,
      life: 2500,
    })
  } catch (e) {
    erroMover.value = e.message
  } finally {
    movendo.value = false
  }
}

watch(semanaSelecionada, carregar)
onMounted(carregar)
</script>

<style scoped>
.topo {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.voltar,
.atalho-topo {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--sgt-texto);
  text-decoration: none;
}

.voltar {
  margin-left: -10px;
}

.atalho-topo {
  margin-right: -10px;
}

.voltar .pi,
.atalho-topo .pi {
  font-size: 18px;
}

h1 {
  margin: 0;
  font-size: 18px;
}

.seletor-semana {
  display: grid;
  grid-template-columns: 1fr 1fr;
  margin-bottom: 14px;
}

.seletor-semana :deep(.p-togglebutton) {
  padding: 0;
  min-height: 36px;
  font-size: 13px;
}

.seletor-semana :deep(.p-togglebutton-content) {
  width: 100%;
  justify-content: center;
  padding: 4px 6px;
}

.aviso {
  margin-bottom: 12px;
}

.carregando {
  display: flex;
  justify-content: center;
  padding: 48px 0;
}

.resumo {
  background: #fff;
  border: 1px solid var(--sgt-borda);
  border-radius: 12px;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.resumo-texto {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.periodo {
  font-weight: 600;
  font-size: 15px;
}

.grupo {
  margin-top: 22px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

h2 {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--sgt-texto-suave);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.cabecalho-sem-espaco {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.lista-blocos,
.lista-tarefas,
.lista-sem-espaco,
.opcoes-blocos {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.bloco {
  background: #fff;
  border: 1px solid var(--sgt-borda);
  border-radius: 12px;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.bloco.passado {
  opacity: 0.65;
}

.bloco-topo {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.bloco-titulo {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.dia {
  font-weight: 600;
  font-size: 15px;
}

.bloco-vazio {
  margin: 0;
}

.lista-tarefas {
  border-top: 1px solid var(--sgt-borda);
  padding-top: 10px;
  gap: 8px;
}

.item-tarefa {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 10px 12px;
  background: var(--sgt-fundo);
  border-radius: 10px;
}

.item-principal {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.horario-slot {
  font-size: 12px;
  font-weight: 600;
  color: var(--sgt-azul);
}

.titulo-tarefa {
  font-weight: 600;
  font-size: 15px;
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
  gap: 4px 12px;
  font-size: 13px;
  color: var(--sgt-texto-suave);
}

.detalhe {
  display: flex;
  align-items: center;
  gap: 5px;
}

.detalhe .pi {
  font-size: 12px;
}

.cartao-sem-espaco {
  background: #fff;
  border: 1px solid #f59e0b;
  border-radius: 12px;
  padding: 12px 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.etiqueta-motivo {
  align-self: flex-start;
  margin-top: 2px;
}

.vazio {
  margin-top: 24px;
  padding: 28px 18px;
  border: 1px dashed #c9d1da;
  border-radius: 12px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  text-align: center;
}

.vazio .pi {
  font-size: 28px;
  color: #9aa6b2;
}

.vazio-titulo {
  font-weight: 600;
}

.resumo-mover {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-bottom: 12px;
}

.aviso-mover {
  margin-bottom: 12px;
}

.opcoes-blocos {
  gap: 8px;
}

.opcao-bloco {
  width: 100%;
  text-align: left;
  padding: 10px 12px;
  background: #fff;
  border: 1px solid var(--sgt-borda);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  cursor: pointer;
  font-family: inherit;
  color: inherit;
}

.opcao-bloco:hover:not(:disabled) {
  border-color: var(--sgt-azul);
  background: #f8fbff;
}

.opcao-bloco:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.opcao-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.seta {
  color: var(--sgt-texto-suave);
  font-size: 13px;
}

.botao-sem-espaco {
  margin-top: 12px;
}
</style>
