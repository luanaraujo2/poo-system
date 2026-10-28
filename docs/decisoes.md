# Decisões e pendências

## Decisões tomadas

- **Leitura por projeção.** `Usuario` não tem construtor vazio, então o EF Core usa o construtor com parâmetros ao ler do banco, e isso cria 5 categorias novas na memória a cada leitura. Por isso a API lê usuários com `Select`, sem montar a entidade. As listagens de tarefas e categorias também usam `Select`.
- **Construtor vazio em `Tarefa` (Sprint 4).** A US05 precisa carregar a tarefa para editar. Sem construtor vazio, o EF chamava o construtor com a regra do prazo e dava erro 500 em tarefa atrasada. Foi criado `private Tarefa() { }`, usado só pelo EF. Isso também resolveu o erro 500 do endpoint de subtarefas com tarefa-pai atrasada.
- **Métodos novos no Domain (Sprint 4).** `Tarefa.Editar`, `Tarefa.TrocarCategoria`, `Categoria.AlterarCor` e `Categoria.CorValida`. Nenhum método que já existia mudou.
- **Categorias padrão travadas (US04).** As 5 categorias criadas com a conta não podem ser renomeadas nem excluídas; só as próprias.
- **Excluir tarefa leva as subtarefas junto (US05).** O banco não deixa apagar a tarefa-pai antes das filhas (`ON DELETE RESTRICT`), então a API apaga do último nível de subtarefa para o primeiro, numa transação.
- **Histórico fica para a US12.** Editar tarefa não grava histórico de mudanças nesta sprint.
- **DatePicker do PrimeVue não foi usado.** No modo 24h ele não aceita hora digitada (problema no `populateTime` da versão 4.5.5). Por isso o prazo usa o campo de data do navegador e um campo de hora próprio.
- **Cartões da lista abrem a edição (Sprint 4).** Na Sprint 3 eles não abriam nada; agora levam para "Editar tarefa".
- **Sem tabela de sessão.** O token JWT vale 8 horas e "sair" é o front apagar o token. O token fica no `localStorage`, o que é aceitável para o projeto da faculdade.
- **Chave do token no repositório.** `Jwt:Chave` e a senha do banco ficam em `appsettings.Development.json`, só para desenvolvimento. Fora do Development a API não sobe sem uma chave configurada com 32 caracteres ou mais.
- **Mesma mensagem no login.** "E-mail ou senha inválidos." para e-mail inexistente e senha errada, para não revelar quais e-mails têm conta.
- **Cabeçalho `X-Usuario-Id`.** Só funciona em Development, para os testes do `.http` sem precisar do token.

## Pendências

### A combinar com o Luan (Domain)

- **Construtor vazio em `Usuario`.** `Tarefa` já tem (Sprint 4). Falta `private Usuario() { }` para o EF parar de criar categorias na memória ao ler um usuário.
- **Regra do prazo.** Hoje o prazo precisa ser depois do momento do cadastro (regra da classe `Tarefa`). Falta decidir se uma tarefa com prazo no dia de hoje deve valer até o fim do dia.
- **Namespaces.** `Categoria` e os enums estão sem `namespace` (ficam no global).
- **Endpoint de subtarefas** (`POST /api/tarefas/{idPai}/subtarefas/{idFilha}`):
  - não carrega as subtarefas existentes antes de somar, então a soma pode passar da estimativa do pai.
  - O erro 500 com tarefa-pai atrasada foi resolvido pelo construtor vazio de `Tarefa`.

### Fora do escopo desta sprint

- Mudança de status de tarefa (US07).
- Subtarefas no front-end.
- Projeto de testes automatizados (hoje os testes são manuais, pelo `.http`).
