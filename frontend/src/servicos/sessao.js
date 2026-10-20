import { reactive } from 'vue'

// Guarda o token do login no navegador. "Sair" (US02) é apagar o token.
const CHAVE = 'sgt.sessao'

function lerSalvo() {
  try {
    const salvo = JSON.parse(localStorage.getItem(CHAVE) ?? 'null')
    if (salvo?.token && new Date(salvo.expiraEm) > new Date()) return salvo
  } catch {
    // sessão salva inválida: começa sem login
  }
  return { token: null, expiraEm: null, usuario: null }
}

export const sessao = reactive(lerSalvo())

export function entrar({ token, expiraEm, usuario }) {
  Object.assign(sessao, { token, expiraEm, usuario })
  localStorage.setItem(CHAVE, JSON.stringify({ token, expiraEm, usuario }))
}

export function sair() {
  Object.assign(sessao, { token: null, expiraEm: null, usuario: null })
  localStorage.removeItem(CHAVE)
}
