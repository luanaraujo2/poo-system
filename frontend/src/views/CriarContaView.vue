<!-- US01 - Cadastrar conta -->
<template>
  <main class="tela">
    <RouterLink :to="{ name: 'entrar' }" class="voltar" aria-label="Voltar">
      <i class="pi pi-arrow-left" />
    </RouterLink>

    <h1>Criar conta</h1>
    <p class="subtitulo">Suas tarefas ficam salvas e só você tem acesso a elas.</p>

    <form id="form-conta" class="formulario" @submit.prevent="enviar">
      <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>

      <div class="campo">
        <label for="nome">Nome</label>
        <InputText id="nome" v-model="nome" autocomplete="name" size="large" />
      </div>

      <div class="campo">
        <label for="email">E-mail</label>
        <InputText
          id="email"
          v-model="email"
          type="email"
          autocomplete="email"
          size="large"
          @invalid="validarEmail"
          @input="limparValidacaoEmail"
        />
      </div>

      <div class="campo">
        <label for="senha">Senha</label>
        <Password
          v-model="senha"
          input-id="senha"
          :feedback="false"
          toggle-mask
          autocomplete="new-password"
          size="large"
          fluid
        />
        <span class="dica">Mínimo de 8 caracteres.</span>
      </div>
      <div class="campo">
        <label for="confirmar-senha">Confirmar senha</label>
        <Password
          v-model="senhaConfirmacao"
          input-id="confirmar-senha"
          :feedback="false"
          toggle-mask
          autocomplete="new-password"
          size="large"
          fluid
        />
      </div>
    </form>

    <section class="categorias">
      <span class="titulo-categorias">Você já começa com 5 categorias</span>
      <div class="lista-categorias">
        <span v-for="categoria in CATEGORIAS_PADRAO" :key="categoria.nome" class="chip">
          <span class="ponto-cor" :style="{ background: categoria.cor }" />
          {{ categoria.nome }}
        </span>
      </div>
    </section>

    <div class="acoes">
      <Button type="submit" form="form-conta" label="Criar conta" size="large" :loading="enviando" class="largura-total" />
    </div>
  </main>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Password from 'primevue/password'
import { useToast } from 'primevue/usetoast'

import { api } from '../servicos/api'
import { entrar } from '../servicos/sessao'

// Mesmas categorias e cores que a classe Usuario cria no cadastro.
const CATEGORIAS_PADRAO = [
  { nome: 'Trabalho', cor: '#FF5733' },
  { nome: 'Estudos', cor: '#33FF57' },
  { nome: 'Pessoal', cor: '#3357FF' },
  { nome: 'Familia', cor: '#FF33A1' },
  { nome: 'Relacionamentos', cor: '#33FFF5' },
]

const router = useRouter()
const toast = useToast()

const nome = ref('')
const email = ref('')
const senha = ref('')
const senhaConfirmacao = ref('')
const erro = ref('')
const enviando = ref(false)

function validarEmail(event) {
  const input = event.currentTarget
  input.setCustomValidity(
    input.validity.typeMismatch
      ? 'O e-mail cadastrado deve ser válido e incluir "@" (ex.: nome@dominio.com).'
      : '',
  )
}

function limparValidacaoEmail(event) {
  event.currentTarget.setCustomValidity('')
}

async function enviar() {
  erro.value = ''

  if (!senhaConfirmacao.value) {
    erro.value = 'Confirme sua senha.'
    return
  }
  if (senha.value !== senhaConfirmacao.value) {
    erro.value = 'As senhas não coincidem.'
    return
  }

  enviando.value = true
  try {
    await api('/usuarios', {
      metodo: 'POST',
      corpo: { nome: nome.value, email: email.value, senha: senha.value },
    })
    // Conta criada: já entra direto, sem pedir o login de novo.
    const sessao = await api('/login', { metodo: 'POST', corpo: { email: email.value, senha: senha.value } })
    entrar(sessao)
    toast.add({ severity: 'success', summary: 'Conta criada', detail: `Bem-vindo, ${sessao.usuario.nome}!`, life: 3000 })
    router.push({ name: 'tarefas' })
  } catch (e) {
    erro.value = e.message
  } finally {
    enviando.value = false
  }
}
</script>

<style scoped>
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
  font-size: 20px;
}

h1 {
  margin: 12px 0 6px;
  font-size: 26px;
}

.subtitulo {
  margin: 0;
  font-size: 15px;
  line-height: 1.45;
  color: var(--sgt-texto-suave);
}

.formulario {
  display: flex;
  flex-direction: column;
  gap: 16px;
  margin-top: 28px;
}

.categorias {
  margin-top: 24px;
  background: #fff;
  border: 1px solid var(--sgt-borda);
  border-radius: 12px;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.titulo-categorias {
  font-size: 14px;
  font-weight: 600;
}

.lista-categorias {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.chip {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  padding: 5px 10px;
  border-radius: 999px;
  background: var(--sgt-fundo);
}

.acoes {
  margin-top: auto;
  padding-top: 24px;
}
</style>
