import { createRouter, createWebHistory } from 'vue-router'
import { sessao } from './servicos/sessao'

import EntrarView from './views/EntrarView.vue'
import CriarContaView from './views/CriarContaView.vue'
import TarefasView from './views/TarefasView.vue'
import NovaTarefaView from './views/NovaTarefaView.vue'
import CategoriasView from './views/CategoriasView.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/tarefas' },
    { path: '/entrar', name: 'entrar', component: EntrarView, meta: { publica: true } },
    { path: '/criar-conta', name: 'criar-conta', component: CriarContaView, meta: { publica: true } },
    { path: '/tarefas', name: 'tarefas', component: TarefasView },
    { path: '/tarefas/nova', name: 'nova-tarefa', component: NovaTarefaView },
    // A mesma tela do cadastro abre em modo edição quando recebe o id (US05).
    { path: '/tarefas/:id(\\d+)', name: 'editar-tarefa', component: NovaTarefaView, props: true },
    { path: '/categorias', name: 'categorias', component: CategoriasView },
    { path: '/:pathMatch(.*)*', redirect: '/tarefas' },
  ],
})

// Telas que não são públicas exigem login (US02).
router.beforeEach((para) => {
  if (!para.meta.publica && !sessao.token) return { name: 'entrar' }
  if (para.meta.publica && sessao.token) return { name: 'tarefas' }
})
