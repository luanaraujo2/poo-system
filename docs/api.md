# API

Base: `http://localhost:5224`. Tudo em JSON, com campos em camelCase. Enums trafegam como texto. Datas voltam em UTC (terminam em `Z`).

| Método | Rota | Descrição | Respostas |
|---|---|---|---|
| POST | `/api/usuarios` | Cria conta (US01) | 201, 400, 409 |
| POST | `/api/login` | Entra e devolve o token (US02) | 200, 400, 401 |
| GET | `/api/usuarios/eu` | Dados do usuário logado | 200, 401 |
| GET | `/api/categorias` | Categorias do usuário, com quantas tarefas cada uma tem | 200, 401 |
| POST | `/api/categorias` | Cria categoria própria (US04) | 201, 400, 401, 409 |
| PUT | `/api/categorias/{id}` | Renomeia e troca a cor de categoria própria (US04) | 200, 400, 401, 404, 409 |
| DELETE | `/api/categorias/{id}?moverPara={id}` | Exclui categoria própria, movendo as tarefas dela (US04) | 204, 400, 401, 404, 409 |
| POST | `/api/tarefas` | Cadastra tarefa (US03) | 201, 400, 401 |
| GET | `/api/tarefas` | Lista as tarefas do usuário, por prazo | 200, 401 |
| GET | `/api/tarefas/{id}` | Obtém uma tarefa do usuário | 200, 401, 404 |
| PUT | `/api/tarefas/{id}` | Edita a tarefa (US05) | 200, 400, 401, 404 |
| DELETE | `/api/tarefas/{id}` | Exclui a tarefa e as subtarefas dela (US05) | 204, 401, 404 |
| POST | `/api/tarefas/{idPai}/subtarefas/{idFilha}` | Vincula uma subtarefa (do Luan, fora das três histórias) | 204, 400, 401, 404 |

Com exceção de criar conta e entrar, todas as rotas pedem `Authorization: Bearer <token>`. Só em Development, os testes do `.http` aceitam o cabeçalho `X-Usuario-Id: 1` no lugar do token.

Erros de regra voltam com `{ "mensagem": "..." }`. A exceção é o 404 de `GET /api/tarefas/{id}`, que volta sem corpo.

## Criar conta

`POST /api/usuarios`

```json
{ "nome": "Ana Clara", "email": "ana.clara@email.com", "senha": "12345678" }
```

Resposta 201:

```json
{ "id": 2, "nome": "Ana Clara", "email": "ana.clara@email.com" }
```

## Entrar

`POST /api/login`

```json
{ "email": "ana.clara@email.com", "senha": "12345678" }
```

Resposta 200:

```json
{
  "token": "eyJhbGciOi...",
  "expiraEm": "2026-10-20T20:00:00Z",
  "usuario": { "id": 2, "nome": "Ana Clara", "email": "ana.clara@email.com" }
}
```

## Categorias

`GET /api/categorias`

```json
[
  { "id": 1, "nome": "Trabalho", "cor": "#FF5733", "padrao": true, "tarefas": 3 },
  { "id": 6, "nome": "Academia", "cor": "#7C3AED", "padrao": false, "tarefas": 0 }
]
```

`tarefas` é quantas tarefas usam a categoria.

### Criar e renomear (US04)

`POST /api/categorias` e `PUT /api/categorias/{id}`:

```json
{ "nome": "Academia", "cor": "#7C3AED" }
```

A cor é opcional: sem ela, a categoria nova fica cinza (`#9AA6B2`) e a editada mantém a que tinha. Resposta: a categoria, no mesmo formato da lista.

### Excluir (US04)

`DELETE /api/categorias/{id}` responde 204 se a categoria não tiver tarefas. Se tiver, responde 409 até receber para onde elas vão: `DELETE /api/categorias/{id}?moverPara=3`. As tarefas mudam de categoria e a categoria é excluída na mesma gravação.

## Cadastrar tarefa

`POST /api/tarefas`. Use um prazo no futuro.

```json
{
  "titulo": "Estudar para a prova de POO",
  "descricao": "Revisar herança e polimorfismo",
  "prazo": "2026-12-31T18:00:00-03:00",
  "estimativaMin": 90,
  "categoriaId": 2,
  "criticidade": "Alta",
  "complexidade": "Media",
  "prioridade": "Urgente_Importante"
}
```

Resposta 201 (a mesma forma vale para `GET /api/tarefas` e `GET /api/tarefas/{id}`):

```json
{
  "id": 1,
  "titulo": "Estudar para a prova de POO",
  "descricao": "Revisar herança e polimorfismo",
  "prazo": "2026-12-31T21:00:00Z",
  "estimativaMin": 90,
  "categoriaId": 2,
  "categoria": "Estudos",
  "criticidade": "Alta",
  "complexidade": "Media",
  "prioridade": "Urgente_Importante",
  "status": "Pendente",
  "criadoEm": "2026-10-20T14:00:00Z",
  "subtarefasIds": []
}
```

## Editar e excluir tarefa (US05)

`PUT /api/tarefas/{id}` recebe os mesmos campos do cadastro e devolve a tarefa atualizada. O status não muda aqui (fica para a US07).

`DELETE /api/tarefas/{id}` responde 204. As subtarefas dela, e as subtarefas dessas, são excluídas junto. A tela pede confirmação antes de chamar.

### Valores aceitos

| Campo | Valores |
|---|---|
| `criticidade` | `Baixa`, `Media`, `Alta`, `Critica` |
| `complexidade` | `Baixa`, `Media`, `Alta` |
| `prioridade` | `Urgente_Importante`, `Importante_NaoUrgente`, `Urgente_NaoImportante`, `NaoUrgente_NaoImportante` |
| `status` | `Pendente`, `EmAndamento`, `Concluida`, `Cancelada` |

## Regras de validação

As regras de nome, e-mail, título, tempo estimado e prazo ficam nos construtores das classes do Domain; a API confere o restante.

**Conta**
- Nome com pelo menos 3 e no máximo 100 caracteres.
- E-mail válido (com @), com no máximo 150 caracteres e ainda não cadastrado, sem diferenciar maiúsculas.
- Senha com pelo menos 6 caracteres.

**Tarefa**
- Título com pelo menos 3 e no máximo 150 caracteres.
- Tempo estimado maior que zero.
- Prazo obrigatório e depois do momento do cadastro.
- Criticidade, complexidade e prioridade obrigatórias.
- A categoria precisa existir e ser do usuário (as 5 padrão: Trabalho, Estudos, Pessoal, Familia e Relacionamentos).
- Toda tarefa nova começa como `Pendente`.
- Cada usuário só enxerga as próprias tarefas.

**Edição de tarefa (US05)**
- Mesmas regras do cadastro para título, tempo estimado, categoria e enums.
- O prazo só é conferido se mudar. Uma tarefa atrasada pode ser editada sem mudar o prazo; se mudar, o novo prazo precisa ser futuro.
- O tempo estimado não pode ficar menor que a soma das subtarefas.
- O registro das mudanças no histórico fica para a US12.

**Categoria (US04)**
- Nome com pelo menos 3 e no máximo 50 caracteres, sem repetir outra categoria do usuário (sem diferenciar maiúsculas).
- Cor no formato `#RRGGBB`.
- As 5 categorias padrão não podem ser renomeadas nem excluídas.
- Categoria com tarefas só é excluída informando para qual categoria elas vão (`moverPara`), que precisa ser outra categoria do usuário.

## Autenticação

O token é um JWT assinado com HMAC-SHA256, válido por 8 horas, com as claims `NameIdentifier` (id do usuário), `Name` e `Email`. A API lê o id da claim `NameIdentifier` em cada requisição (`Services/UsuarioAtual`). Token ausente, inválido ou vencido resulta em 401.
