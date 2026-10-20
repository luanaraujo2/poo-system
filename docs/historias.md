# Histórias da Sprint 3

## Quem fez o quê

- **Domain** (classes `Usuario`, `Categoria` e `Tarefa`, os enums e os métodos de subtarefa): Luan.
- **Banco de dados** das três histórias, **endpoints** das US01, US02 e US03 e **front-end**: João Vitor, em cima das classes do Domain.

O endpoint de subtarefas (`POST /api/tarefas/{idPai}/subtarefas/{idFilha}`) e a migration `PersistirSubtarefas` são do Luan e não fazem parte das três histórias.

## US01 - Cadastrar conta

`POST /api/usuarios` e tela "Criar conta".

| Critério | Situação |
|---|---|
| Nome, e-mail e senha | Pronto. Nome com 3 ou mais letras e e-mail com @ (regras da classe `Usuario`); senha com 8 ou mais caracteres |
| Não aceita e-mail repetido | Pronto. 409 "Este e-mail já está cadastrado.", sem diferenciar maiúsculas (índice único `IX_usuario_email`) |
| Senha salva com hash | Pronto (BCrypt) |
| Conta começa com as 5 categorias | Pronto. O construtor de `Usuario` cria as categorias e elas são salvas junto |

Depois de criar a conta, o front já faz o login sozinho.

## US02 - Entrar e sair do sistema

`POST /api/login` e tela "Entrar".

| Critério | Situação |
|---|---|
| Login com e-mail e senha | Pronto. Devolve um token JWT (8 horas) com o id do usuário na claim `NameIdentifier` |
| Erro sem dizer qual campo está errado | Pronto. 401 "E-mail ou senha inválidos." para e-mail inexistente e para senha errada |
| Cada usuário só vê as próprias tarefas | Pronto. `UsuarioAtual` lê o id do token e os endpoints filtram por ele |
| Sair | Pronto. O botão "Sair" da tela de tarefas descarta o token |

Não há tabela de sessão: "sair" é só o front apagar o token. Se o token vencer no meio do uso, o front volta para a tela "Entrar".

## US03 - Cadastrar tarefa

`POST /api/tarefas` e tela "Nova tarefa".

| Critério | Situação |
|---|---|
| Campos: título, descrição, prazo, tempo estimado, categoria, criticidade, complexidade e prioridade | Pronto |
| Categoria entre as 5 padrão | Pronto. `GET /api/categorias`; a categoria precisa ser do usuário |
| Título e tempo estimado obrigatórios | Pronto (400 com mensagem) |
| Prazo não pode ser data que já passou | Pronto, com a regra da classe `Tarefa` (prazo depois do momento do cadastro). Falta combinar se deve valer o dia inteiro, ver [decisões](decisoes.md) |
| Tarefa nova começa como Pendente | Pronto |
| Tela "Nova tarefa" | Pronto. Depois de salvar, volta para "Minhas tarefas" |

As regras de validação estão em [api.md](api.md#regras-de-validação).
