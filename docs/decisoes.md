# Decisões e pendências

## Decisões tomadas

- **Leitura por projeção.** As classes do Domain não têm construtor vazio, então o EF Core usa o construtor com parâmetros ao ler do banco. Em `Usuario`, isso cria 5 categorias novas na memória a cada leitura. Em `Tarefa`, a validação de prazo roda na leitura e dá erro em tarefa atrasada. Por isso a API lê usuários, tarefas e categorias com `Select`, sem montar as entidades.
- **DatePicker do PrimeVue não foi usado.** No modo 24h ele não aceita hora digitada (problema no `populateTime` da versão 4.5.5). Por isso o prazo usa o campo de data do navegador e um campo de hora próprio.
- **Cartões da lista não abrem nada.** Detalhe, edição e mudança de status não são escopo da Sprint 3.
- **Sem tabela de sessão.** O token JWT vale 8 horas e "sair" é o front apagar o token. O token fica no `localStorage`, o que é aceitável para o projeto da faculdade.
- **Chave do token no repositório.** `Jwt:Chave` e a senha do banco ficam em `appsettings.Development.json`, só para desenvolvimento. Fora do Development a API não sobe sem uma chave configurada com 32 caracteres ou mais.
- **Mesma mensagem no login.** "E-mail ou senha inválidos." para e-mail inexistente e senha errada, para não revelar quais e-mails têm conta.
- **Cabeçalho `X-Usuario-Id`.** Só funciona em Development, para os testes do `.http` sem precisar do token.

## Pendências

### A combinar com o Luan (Domain)

- **Construtor vazio.** Adicionar `private Usuario() { }`, e o mesmo em `Tarefa` e `Categoria`. Com isso o EF Core deixaria de usar os construtores com validação ao ler do banco.
- **Regra do prazo.** Hoje o prazo precisa ser depois do momento do cadastro (regra da classe `Tarefa`). Falta decidir se uma tarefa com prazo no dia de hoje deve valer até o fim do dia.
- **Namespaces.** `Categoria` e os enums estão sem `namespace` (ficam no global).
- **Endpoint de subtarefas** (`POST /api/tarefas/{idPai}/subtarefas/{idFilha}`):
  - não carrega as subtarefas existentes antes de somar, então a soma pode passar da estimativa do pai;
  - dá erro 500 quando a tarefa-pai está atrasada, pelo mesmo motivo do construtor.

### Fora do escopo desta sprint

- Tela de detalhe, edição e mudança de status de tarefa.
- Subtarefas no front-end.
- Projeto de testes automatizados (hoje os testes são manuais, pelo `.http`).
