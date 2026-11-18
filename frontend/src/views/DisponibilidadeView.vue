<!-- US08 - Cadastro dos blocos semanais de disponibilidade do usuário. -->
<template>
  <main class="tela">
    <header class="topo">
      <RouterLink :to="{ name: 'tarefas' }" class="voltar" aria-label="Voltar"><i class="pi pi-arrow-left" /></RouterLink>
      <h1>Disponibilidade</h1>
      <span class="espaco" />
    </header>

    <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>
    <div v-if="carregando" class="carregando"><ProgressSpinner style="width: 40px; height: 40px" stroke-width="4" /></div>

    <template v-else>
      <p class="dica">Cadastre os períodos em que você costuma ter tempo livre em cada dia da semana.</p>
      <Message v-if="erroEdicao" severity="error" :closable="false" class="aviso-lista">{{ erroEdicao }}</Message>
      <ul v-if="blocos.length" class="lista">
        <li v-for="bloco in blocos" :key="bloco.id" class="item">
          <template v-if="editando?.id === bloco.id">
            <div class="campos-edicao">
              <Select v-model="editando.diaSemana" :options="DIAS" option-label="label" option-value="value" aria-label="Dia da semana" />
              <InputText
                :model-value="editando.horaInicio"
                inputmode="numeric"
                maxlength="5"
                placeholder="09:00"
                autocomplete="off"
                aria-label="Hora de início"
                @input="(e) => formatarHoraCampo(e, editando, 'horaInicio')"
              />
              <span>até</span>
              <InputText
                :model-value="editando.horaFim"
                inputmode="numeric"
                maxlength="5"
                placeholder="12:00"
                autocomplete="off"
                aria-label="Hora de fim"
                @input="(e) => formatarHoraCampo(e, editando, 'horaFim')"
              />
            </div>
            <Button icon="pi pi-check" text rounded aria-label="Salvar alteração" :loading="salvando" @click="salvarEdicao" />
            <Button icon="pi pi-times" severity="secondary" text rounded aria-label="Cancelar" @click="cancelarEdicao" />
          </template>
          <template v-else>
            <div class="texto">
              <span class="nome">{{ nomeDia(bloco.diaSemana) }}</span>
              <span class="dica">{{ hora(bloco.horaInicio) }} – {{ hora(bloco.horaFim) }}</span>
            </div>
            <Button icon="pi pi-pencil" severity="secondary" text rounded :aria-label="`Editar bloco de ${nomeDia(bloco.diaSemana)}`" @click="iniciarEdicao(bloco)" />
            <Button icon="pi pi-trash" severity="danger" text rounded :aria-label="`Excluir bloco de ${nomeDia(bloco.diaSemana)}`" :loading="excluindo === bloco.id" @click="excluir(bloco)" />
          </template>
        </li>
      </ul>
      <section v-else class="vazio"><i class="pi pi-clock" /><span class="vazio-titulo">Nenhum horário cadastrado</span><span class="dica">Adicione seu primeiro bloco de tempo livre.</span></section>

      <section class="grupo">
        <h2>Novo bloco</h2>
        <form class="formulario" @submit.prevent="criar">
          <Message v-if="erroFormulario" severity="error" :closable="false">{{ erroFormulario }}</Message>
          <div class="campo">
            <label for="dia-disponibilidade">Dia da semana</label>
            <Select v-model="novo.diaSemana" input-id="dia-disponibilidade" :options="DIAS" option-label="label" option-value="value" placeholder="Selecione o dia" fluid />
          </div>
          <div class="horarios">
            <div class="campo">
              <label for="inicio-disponibilidade">Hora de início</label>
              <InputText
                id="inicio-disponibilidade"
                :model-value="novo.horaInicio"
                inputmode="numeric"
                maxlength="5"
                placeholder="ex.: 09:00"
                autocomplete="off"
                fluid
                @input="(e) => formatarHoraCampo(e, novo, 'horaInicio')"
              />
            </div>
            <div class="campo">
              <label for="fim-disponibilidade">Hora de fim</label>
              <InputText
                id="fim-disponibilidade"
                :model-value="novo.horaFim"
                inputmode="numeric"
                maxlength="5"
                placeholder="ex.: 12:00"
                autocomplete="off"
                fluid
                @input="(e) => formatarHoraCampo(e, novo, 'horaFim')"
              />
            </div>
          </div>
          <span class="dica">Formato 24h (digite só os números, ex.: 0900 para 09:00).</span>
          <Button type="submit" label="Adicionar bloco" icon="pi pi-plus" :loading="salvando" fluid />
        </form>
      </section>
    </template>
  </main>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import Select from 'primevue/select'
import { api } from '../servicos/api'

const DIAS = [
  { value: 'Segunda', label: 'Segunda-feira' }, { value: 'Terca', label: 'Terça-feira' },
  { value: 'Quarta', label: 'Quarta-feira' }, { value: 'Quinta', label: 'Quinta-feira' },
  { value: 'Sexta', label: 'Sexta-feira' }, { value: 'Sabado', label: 'Sábado' },
  { value: 'Domingo', label: 'Domingo' },
]
const blocos = ref([])
const carregando = ref(true)
const salvando = ref(false)
const excluindo = ref(null)
const erro = ref('')
const erroEdicao = ref('')
const erroFormulario = ref('')
const editando = ref(null)
const novo = ref({ diaSemana: null, horaInicio: '', horaFim: '' })

const nomeDia = (valor) => DIAS.find((dia) => dia.value === valor)?.label ?? valor
const hora = (valor) => valor?.slice(0, 5) ?? ''
const horarioJson = (valor) => `${valor}:00`

function formatarHoraCampo(evento, alvo, campo) {
  const digitos = evento.target.value.replace(/\D/g, '').slice(0, 4)
  const formatado = digitos.length > 2 ? `${digitos.slice(0, 2)}:${digitos.slice(2)}` : digitos
  alvo[campo] = formatado
  evento.target.value = formatado
}

function horaValida(texto) {
  if (!/^\d{2}:\d{2}$/.test(texto)) return false
  const [h, m] = texto.split(':').map(Number)
  return h <= 23 && m <= 59
}

async function carregar() {
  try { blocos.value = await api('/disponibilidades') } catch (e) { erro.value = e.message } finally { carregando.value = false }
}

function validarHorario(inicio, fim) {
  if (!inicio || !fim) return 'Informe a hora de início e a hora de fim.'
  if (!horaValida(inicio) || !horaValida(fim)) return 'Hora inválida. Use o formato 24h, ex.: 09:00 até 12:00.'
  if (fim <= inicio) return 'A hora de fim deve ser maior que a hora de início.'
  return ''
}

function corpo(diaSemana, horaInicio, horaFim) {
  return { diaSemana, horaInicio: horarioJson(horaInicio), horaFim: horarioJson(horaFim) }
}

async function criar() {
  erroFormulario.value = validarHorario(novo.value.horaInicio, novo.value.horaFim)
  if (!novo.value.diaSemana) erroFormulario.value ||= 'Selecione um dia da semana.'
  if (erroFormulario.value) return
  salvando.value = true
  try {
    blocos.value = [...blocos.value, await api('/disponibilidades', { metodo: 'POST', corpo: corpo(novo.value.diaSemana, novo.value.horaInicio, novo.value.horaFim) })]
      .sort((a, b) => DIAS.findIndex((d) => d.value === a.diaSemana) - DIAS.findIndex((d) => d.value === b.diaSemana) || a.horaInicio.localeCompare(b.horaInicio))
    novo.value = { diaSemana: null, horaInicio: '', horaFim: '' }
    erroFormulario.value = ''
  } catch (e) { erroFormulario.value = e.message } finally { salvando.value = false }
}

function iniciarEdicao(bloco) {
  editando.value = { ...bloco, horaInicio: hora(bloco.horaInicio), horaFim: hora(bloco.horaFim) }
  erroEdicao.value = ''
}

function cancelarEdicao() {
  editando.value = null
  erroEdicao.value = ''
}

async function salvarEdicao() {
  const atual = editando.value
  erroEdicao.value = validarHorario(atual.horaInicio, atual.horaFim)
  if (erroEdicao.value) return
  salvando.value = true
  try {
    const salvo = await api(`/disponibilidades/${atual.id}`, { metodo: 'PUT', corpo: corpo(atual.diaSemana, atual.horaInicio, atual.horaFim) })
    blocos.value = blocos.value.map((bloco) => bloco.id === salvo.id ? salvo : bloco)
      .sort((a, b) => DIAS.findIndex((d) => d.value === a.diaSemana) - DIAS.findIndex((d) => d.value === b.diaSemana) || a.horaInicio.localeCompare(b.horaInicio))
    editando.value = null
    erroEdicao.value = ''
  } catch (e) { erroEdicao.value = e.message } finally { salvando.value = false }
}

async function excluir(bloco) {
  excluindo.value = bloco.id
  try {
    await api(`/disponibilidades/${bloco.id}`, { metodo: 'DELETE' })
    blocos.value = blocos.value.filter((item) => item.id !== bloco.id)
  } catch (e) { erro.value = e.message } finally { excluindo.value = null }
}

onMounted(carregar)
</script>

<style scoped>
.topo { display: flex; align-items: center; justify-content: space-between; margin-bottom: 8px; }
.voltar { width: 44px; height: 44px; margin-left: -10px; display: flex; align-items: center; justify-content: center; color: var(--sgt-texto); text-decoration: none; }
.voltar .pi { font-size: 18px; }
h1 { margin: 0; font-size: 18px; }
.espaco { width: 34px; }
.carregando { display: flex; justify-content: center; padding: 48px 0; }
.aviso-lista { margin-top: 12px; }
.lista { list-style: none; margin: 16px 0 24px; padding: 0; display: flex; flex-direction: column; gap: 8px; }
.item { min-height: 56px; display: flex; align-items: center; gap: 4px; padding: 8px 10px 8px 14px; background: #fff; border: 1px solid var(--sgt-borda); border-radius: 10px; }
.texto { display: flex; flex: 1; flex-direction: column; gap: 3px; }
.nome { font-weight: 600; }
.campos-edicao { display: flex; flex: 1; align-items: center; gap: 6px; flex-wrap: wrap; }
.campos-edicao :deep(.p-select) { min-width: 140px; }
.campos-edicao input { width: 78px; }
.grupo { margin-top: 28px; }
.formulario { display: flex; flex-direction: column; gap: 12px; }
.horarios { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.vazio { margin-top: 24px; padding: 28px 18px; border: 1px dashed #c9d1da; border-radius: 12px; display: flex; flex-direction: column; align-items: center; gap: 8px; text-align: center; }
.vazio .pi { font-size: 28px; color: #9aa6b2; }
</style>
