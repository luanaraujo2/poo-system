<!-- US02 - Entrar -->
<template>
  <main class="tela entrar">
    <header class="cabecalho">
      <div class="logo" aria-hidden="true">
        <i class="pi pi-clock" />
      </div>
      <h1>Gestão do Tempo</h1>
      <p>Suas tarefas, o seu tempo livre e o que fazer primeiro, em um lugar só.</p>
    </header>

    <form class="formulario" @submit.prevent="enviar">
      <Message v-if="avisoExpirou" severity="info" :closable="false">Sua sessão expirou. Entre de novo.</Message>
      <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>

      <div class="campo">
        <label for="email">E-mail</label>
        <InputText id="email" v-model="email" type="email" autocomplete="email" size="large" />
      </div>

      <div class="campo">
        <label for="senha">Senha</label>
        <Password
          v-model="senha"
          input-id="senha"
          :feedback="false"
          toggle-mask
          autocomplete="current-password"
          size="large"
          fluid
        />
      </div>

      <Button type="submit" label="Entrar" size="large" :loading="enviando" class="largura-total" />
    </form>

    <footer class="rodape">
      <span>Ainda não tem conta?</span>
      <RouterLink :to="{ name: 'criar-conta' }">Criar conta</RouterLink>
    </footer>
  </main>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Password from 'primevue/password'

import { api } from '../servicos/api'
import { entrar } from '../servicos/sessao'

const router = useRouter()
const route = useRoute()

const email = ref('')
const senha = ref('')
const erro = ref('')
const enviando = ref(false)

const avisoExpirou = computed(() => route.query.expirou === '1' && !erro.value)

async function enviar() {
  erro.value = ''
  enviando.value = true
  try {
    const resposta = await api('/login', { metodo: 'POST', corpo: { email: email.value, senha: senha.value } })
    entrar(resposta)
    router.push({ name: 'tarefas' })
  } catch (e) {
    erro.value = e.message
  } finally {
    enviando.value = false
  }
}
</script>

<style scoped>
.entrar {
  padding-top: 72px;
}

.cabecalho {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.logo {
  width: 56px;
  height: 56px;
  border-radius: 14px;
  background: var(--sgt-azul);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
}

.logo .pi {
  font-size: 28px;
}

h1 {
  margin: 0;
  font-size: 30px;
  line-height: 1.15;
}

p {
  margin: 0;
  font-size: 16px;
  line-height: 1.45;
  color: var(--sgt-texto-suave);
}

.formulario {
  display: flex;
  flex-direction: column;
  gap: 16px;
  margin-top: 44px;
}

.formulario :deep(.p-button) {
  margin-top: 8px;
}

.rodape {
  margin-top: auto;
  padding-top: 32px;
  display: flex;
  justify-content: center;
  gap: 6px;
  font-size: 15px;
}

.rodape span {
  color: var(--sgt-texto-suave);
}

.rodape a {
  font-weight: 600;
  text-decoration: none;
}
</style>
