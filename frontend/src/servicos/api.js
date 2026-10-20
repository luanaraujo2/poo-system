import { sessao } from './sessao'

export class ErroApi extends Error {
  constructor(mensagem, status) {
    super(mensagem)
    this.status = status
  }
}

let quandoExpirar = () => {}

export function aoSessaoExpirar(callback) {
  quandoExpirar = callback
}

const SEM_CONEXAO = 'Não foi possível falar com o servidor. Confira se a API está rodando.'

// Chama a API .NET. Os erros de regra voltam como { mensagem }, que vira o texto do ErroApi.
export async function api(caminho, { metodo = 'GET', corpo } = {}) {
  const headers = {}
  if (corpo !== undefined) headers['Content-Type'] = 'application/json'
  if (sessao.token) headers.Authorization = `Bearer ${sessao.token}`

  let resposta
  try {
    resposta = await fetch(`/api${caminho}`, {
      method: metodo,
      headers,
      body: corpo !== undefined ? JSON.stringify(corpo) : undefined,
    })
  } catch {
    throw new ErroApi(SEM_CONEXAO, 0)
  }

  const texto = await resposta.text()
  let dados = null
  try {
    dados = texto ? JSON.parse(texto) : null
  } catch {
    dados = null
  }

  if (!resposta.ok) {
    if (resposta.status === 401 && sessao.token) {
      quandoExpirar()
      throw new ErroApi('Sua sessão expirou. Entre de novo.', 401)
    }
    if (dados?.mensagem) throw new ErroApi(dados.mensagem, resposta.status)
    if (resposta.status >= 500) throw new ErroApi(SEM_CONEXAO, resposta.status)
    throw new ErroApi('Confira os campos e tente de novo.', resposta.status)
  }

  return dados
}
