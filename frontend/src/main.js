import { createApp } from 'vue'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import 'primeicons/primeicons.css'

import App from './App.vue'
import { router } from './router'
import { TemaSgt } from './tema'
import { aoSessaoExpirar } from './servicos/api'
import { sair } from './servicos/sessao'
import './estilo.css'

// Se o token vencer no meio do uso, volta para a tela de entrar.
aoSessaoExpirar(() => {
  sair()
  router.push({ name: 'entrar', query: { expirou: '1' } })
})

createApp(App)
  .use(router)
  .use(PrimeVue, {
    theme: {
      preset: TemaSgt,
      // Só tema claro, como no protótipo.
      options: { darkModeSelector: '.sgt-modo-escuro' },
    },
    locale: {
      dayNames: ['domingo', 'segunda', 'terça', 'quarta', 'quinta', 'sexta', 'sábado'],
      dayNamesShort: ['dom', 'seg', 'ter', 'qua', 'qui', 'sex', 'sáb'],
      dayNamesMin: ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'],
      monthNames: ['janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho', 'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'],
      monthNamesShort: ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'],
      today: 'Hoje',
      clear: 'Limpar',
      firstDayOfWeek: 0,
      dateFormat: 'dd/mm/yy',
      weak: 'Fraca',
      medium: 'Média',
      strong: 'Forte',
      passwordPrompt: 'Digite a senha',
    },
  })
  .use(ToastService)
  .mount('#app')
