# Histórias

Sprint 3: US01, US02 e US03. Sprint 4: US04 a US07. Sprint 5: US08 a US10.

## Quem fez o quê

- **Domain** (classes `Usuario`, `Categoria` e `Tarefa`, os enums e os métodos de subtarefa): Luan.
- **Banco de dados** das três histórias, **endpoints** das US01, US02 e US03 e **front-end**: João Vitor, em cima das classes do Domain.

O endpoint de vincular subtarefa (`POST /api/tarefas/{idPai}/subtarefas/{idFilha}`) e a migration `PersistirSubtarefas` são do Luan. Na US06 o endpoint passou a considerar as subtarefas que já existem na soma.

## US01 - Cadastrar conta

`POST /api/usuarios` e tela "Criar conta".

| Critério | Situação |
|---|---|
| Nome, e-mail e senha | Pronto. Nome com 3 ou mais letras e e-mail com @ (regras da classe `Usuario`); senha com 6 ou mais caracteres (eram 8 na Sprint 3) |
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

## US04 - Classificar por categoria (Sprint 4)

Feita por João Vitor: endpoints, tela "Categorias" e os métodos novos no Domain (`Categoria.AlterarCor`, `Tarefa.TrocarCategoria`).

| Critério | Situação |
|---|---|
| Cinco categorias padrão criadas no cadastro da conta | Pronto desde a US01 |
| Criar, renomear e excluir categorias próprias | Pronto. `POST`, `PUT` e `DELETE /api/categorias`; nome sem repetir e cor `#RRGGBB`. As padrão ficam travadas |
| Categoria com tarefas não é excluída sem realocação | Pronto. Responde 409 até receber `?moverPara=`; a tela pede para escolher a categoria de destino |

## US05 - Editar e excluir tarefa (Sprint 4)

Feita por João Vitor: endpoints, modo edição da tela de tarefa e os métodos novos no Domain (`Tarefa.Editar` e o construtor vazio para o EF).

| Critério | Situação |
|---|---|
| Editar a tarefa | Pronto. `PUT /api/tarefas/{id}`, com as mesmas regras do cadastro; tarefa atrasada pode ser editada sem mudar o prazo |
| Exclusão pede confirmação | Pronto. A tela abre uma confirmação; se houver subtarefas, avisa que elas vão junto |
| Mudanças registradas no histórico | Fica para a US12, como combinado no planejamento da Sprint 4 |

## US06 - Dividir tarefa em subtarefas (Sprint 4)

Feita por João Vitor: criação de subtarefa pela API e pela tela, quadro de subtarefas e correção da soma no endpoint de vincular. Os métodos de subtarefa do Domain (`AdicionarSubtarefa`, `SomarEstimativasSubtarefas`, `PodeConcluir`) são do Luan.

| Critério | Situação |
|---|---|
| Quebrar uma tarefa em subtarefas | Pronto. "Adicionar subtarefa" na tela da tarefa; na API, `tarefaPaiId` no `POST /api/tarefas` |
| Tempo estimado da tarefa-mãe exibe a soma das subtarefas | Pronto. A tela mostra "As subtarefas somam X de Y estimados"; a soma não pode passar da estimativa |
| Tarefa-mãe só é concluída com todas as subtarefas concluídas | Pronto. Regra da classe `Tarefa` (`PodeConcluir`), aplicada ao concluir pela US07 |

## US07 - Atualizar status (Sprint 4)

Feita por João Vitor: `PATCH /api/tarefas/{id}/status`, quadro de status, filtro na lista e o método `Tarefa.Concluir`.

| Critério | Situação |
|---|---|
| Marcar como Pendente, Em andamento, Concluída ou Cancelada | Pronto. Quadro "Status" no topo da tela da tarefa |
| Ao concluir, grava data e hora da conclusão | Pronto (`concluidoEm`) |
| Ao concluir, grava o tempo real gasto, informado pelo usuário | Pronto. A tela pergunta o tempo real, já sugerindo a estimativa (`tempoRealMin`) |

## US08 - Informar disponibilidade (Sprint 5)

| Critério | Situação |
|---|---|
| Cadastrar blocos de tempo livre por dia da semana, com início e fim | Pronto. Tela "Disponibilidade" e endpoints para listar, cadastrar, editar e excluir blocos próprios |
| Hora de fim maior que hora de início | Pronto. Validado na tela, na API, no Domain e por CHECK no banco |
| Blocos do mesmo dia não podem se sobrepor | Pronto. A API rejeita interseções no mesmo dia; blocos adjacentes são permitidos |

## US09 - Ver alerta de sobrecarga (Sprint 5)

| Critério | Situação |
|---|---|
| Comparar a soma estimada das tarefas abertas com os blocos livres de um período | Pronto. Compara tarefas abertas com prazo na semana atual (segunda a domingo) com os blocos semanais da US08 |
| Exibir alerta quando tarefas passam o tempo livre | Pronto. O alerta aparece no topo de "Minhas tarefas" somente quando há sobrecarga |
| Informar quantas horas passam do tempo livre | Pronto. Mostra o excesso em horas e minutos, além dos totais estimado e livre |

## US10 - Receber sugestão de ordem de execução (Sprint 5)

| Critério | Situação |
|---|---|
| Ordenar as tarefas por pontuação calculada com prazo, criticidade, prioridade e complexidade | Pronto. Soma os pontos dos quatro critérios, com máximo de 100 pontos |
| Mostrar a pontuação de cada tarefa | Pronto. Cada cartão exibe a pontuação sugerida |
| Tarefas vencidas aparecem no topo e destacadas | Pronto. Tarefas abertas atrasadas ficam antes das demais, com etiqueta e cartão em vermelho |
| Permitir que o usuário mude a ordem manualmente | Pronto. Modo manual com controles para mover tarefas para cima ou para baixo; a ordem fica salva no navegador por usuário |

### Pontuação da US10

- **Prazo (0 a 40)**: atrasada = 40; vence hoje = 35; amanhã = 30; em até 3 dias = 20; em até 7 dias = 10; depois disso = 0.
- **Criticidade (0 a 30)**: Baixa = 0, Média = 10, Alta = 20, Crítica = 30.
- **Prioridade (0 a 20)**: nem urgente nem importante = 0; urgente não importante = 10; importante não urgente = 15; urgente e importante = 20.
- **Complexidade (0 a 10)**: Baixa = 0, Média = 5, Alta = 10.

Tarefas atrasadas permanecem no topo também no modo manual; a ordem manual pode ser alterada dentro dos grupos de atrasadas e não atrasadas.
