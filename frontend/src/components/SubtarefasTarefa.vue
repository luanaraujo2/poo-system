<!-- US06 - Subtarefas. Fica na tela "Editar tarefa": lista as subtarefas, mostra a soma das estimativas e cria novas. -->
<template>
  <section class="subtarefas" aria-labelledby="rotulo-subtarefas">
    <div class="cabecalho">
      <span id="rotulo-subtarefas" class="rotulo">Subtarefas</span>
      <span v-if="subtarefas.length" class="dica">{{ subtarefasConcluidas }} de {{ subtarefas.length }} concluídas</span>
    </div>

    <p class="dica soma">
      <template v-if="subtarefas.length">
        As subtarefas somam <strong>{{ formatarDuracao(tarefa.subtarefasMin) }}</strong> de
        {{ formatarDuracao(tarefa.estimativaMin) }} estimados.
        <template v-if="restante > 0">Faltam {{ formatarDuracao(restante) }} para distribuir.</template>
      </template>
      <template v-else>Quebre a tarefa em partes menores para começar mais fácil.</template>
    </p>

    <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>

    <ul v-if="subtarefas.length" class="lista">
      <li v-for="sub in subtarefas" :key="sub.id">
        <RouterLink :to="{ name: 'editar-tarefa', params: { id: sub.id } }" class="item">
          <i :class="['pi', sub.status === 'Concluida' ? 'pi-check-circle feita' : 'pi-circle']" aria-hidden="true" />
          <span :class="['titulo', { riscado: sub.status === 'Concluida' || sub.status === 'Cancelada' }]">{{ sub.titulo }}</span>
          <span class="dica">{{ formatarDuracao(sub.estimativaMin) }}</span>
          <Tag :value="STATUS[sub.status]" :severity="corDoStatus(sub.status)" />
        </RouterLink>
      </li>
    </ul>

    <Button
      v-if="podeAdicionar"
      label="Adicionar subtarefa"
      icon="pi pi-plus"
      severity="secondary"
      outlined
      @click="router.push({ name: 'nova-tarefa', query: { pai: tarefa.id } })"
    />
    <p v-else-if="tarefa.status === 'Concluida'" class="dica">Tarefa concluída não recebe subtarefas novas.</p>
    <p v-else class="dica">As subtarefas já somam toda a estimativa. Aumente o tempo estimado para adicionar outra.</p>
  </section>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Tag from 'primevue/tag'

import { api } from '../servicos/api'
import { STATUS, corDoStatus, formatarDuracao } from '../constantes'

const props = defineProps({ tarefa: { type: Object, required: true } })

const router = useRouter()

const subtarefas = ref([])
const erro = ref('')

const subtarefasConcluidas = computed(() => subtarefas.value.filter((s) => s.status === 'Concluida').length)
const restante = computed(() => props.tarefa.estimativaMin - props.tarefa.subtarefasMin)
const podeAdicionar = computed(() => props.tarefa.status !== 'Concluida' && restante.value > 0)

// A lista geral já traz o id da tarefa-pai de cada tarefa; aqui ficam só as filhas desta.
async function carregar() {
  try {
    const todas = await api('/tarefas')
    subtarefas.value = todas.filter((t) => t.tarefaPaiId === props.tarefa.id)
  } catch (e) {
    erro.value = e.message
  }
}

onMounted(carregar)
// Recarrega quando o status ou a estimativa da tarefa mudam na mesma tela.
watch(() => [props.tarefa.status, props.tarefa.subtarefasMin], carregar)
</script>

<style scoped>
.subtarefas {
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

.soma {
  margin: 0;
  line-height: 1.45;
}

.lista {
  list-style: none;
  margin: 0;
  padding: 0;
  border: 1px solid var(--sgt-borda);
  border-radius: 10px;
}

.lista li + li {
  border-top: 1px solid var(--sgt-borda);
}

.item {
  min-height: 48px;
  padding: 8px 10px;
  display: flex;
  align-items: center;
  gap: 10px;
  color: inherit;
  text-decoration: none;
}

.item:hover {
  background: var(--sgt-fundo);
}

.item:focus-visible {
  outline: 2px solid var(--sgt-azul);
  outline-offset: -2px;
}

.item .pi {
  color: #9aa6b2;
}

.item .pi.feita {
  color: #0e9f6e;
}

.titulo {
  flex: 1;
  min-width: 0;
  overflow-wrap: anywhere;
  font-weight: 500;
}

.titulo.riscado {
  text-decoration: line-through;
  color: var(--sgt-texto-suave);
}
</style>
