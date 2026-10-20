# Banco de dados

PostgreSQL 16, feito com EF Core (code-first). As classes do Domain são mapeadas em `AppDbContext` (`GestaoTempo.Api/Data`) e as tabelas são criadas pelas migrations, aplicadas sozinhas quando a API sobe em Development. Tabelas e colunas seguem o DER, em `snake_case`.

O script [banco.sql](banco.sql) é uma cópia legível do esquema, para entrega e consulta.

## Tabelas

| Tabela | Colunas |
|---|---|
| `usuario` | `id_usuario`, `nome`, `email` (único, `citext`), `senha_hash`, `criado_em` |
| `categoria` | `id_categoria`, `id_usuario`, `nome`, `cor`, `padrao` |
| `tarefa` | `id_tarefa`, `id_usuario`, `id_categoria`, `id_tarefa_pai`, `titulo`, `descricao`, `prazo`, `estimativa_min`, `tempo_real_min`, `criticidade`, `complexidade`, `prioridade`, `status`, `criada_em`, `concluida_em` |

Relações:

- `categoria.id_usuario` e `tarefa.id_usuario` apontam para `usuario` (apagar o usuário apaga categorias e tarefas).
- `tarefa.id_categoria` aponta para `categoria` (não dá para apagar uma categoria em uso).
- `tarefa.id_tarefa_pai` aponta para outra tarefa (subtarefa). A chave estrangeira é composta, `(id_tarefa_pai, id_usuario)`, o que impede que pai e filha sejam de usuários diferentes.

Os enums são gravados como texto.

## O que as migrations criam

- as três tabelas, com chaves primárias, chaves estrangeiras e índices;
- a extensão `citext`, usada em `usuario.email` para o e-mail único e o login ignorarem maiúsculas e minúsculas;
- CHECKs em `tarefa`, para o banco recusar dados fora das regras;
- índice único de nome de categoria por usuário e índices para listar tarefas por status e por prazo;
- as subtarefas (`id_tarefa_pai`, a chave `AK_tarefa_id_tarefa_id_usuario`, o CHECK que impede uma tarefa de ser pai de si mesma e a FK composta).

| Migration | Autor | O que faz |
|---|---|---|
| `CriacaoInicial` | João Vitor | `usuario`, `categoria` e `tarefa` |
| `PersistirSubtarefas` | Luan | coluna e constraints de subtarefa |

## Decisões

- **Senha**: gravada só como hash BCrypt em `senha_hash`. Nunca em texto puro.
- **E-mail único sem caixa**: `Joao@x.com` e `joao@x.com` contam como o mesmo e-mail. A comparação `u.Email == email` em consultas já ignora a caixa.
- **Categorias**: pertencem ao usuário (`categoria.id_usuario`) e o nome é único por usuário. As 5 padrão são criadas pelo construtor de `Usuario` e salvas junto com ele.
- **Tarefas**: pertencem ao usuário (`tarefa.id_usuario`). Todas as consultas filtram por ele.
- **Regras no banco**: `estimativa_min > 0`, título preenchido, `tempo_real_min >= 0` e valores válidos de status, criticidade, complexidade e prioridade. Ao criar um valor novo em algum enum, atualize o CHECK correspondente em `AppDbContext`.
- **Datas**: as classes usam `DateTime.Now`. O `AppDbContext` converte para UTC ao gravar, porque o PostgreSQL (`timestamptz`) só aceita UTC. A API devolve as datas em UTC.
- **Sessão**: não há tabela de sessão. O login usa token, então "sair" é só descartar o token no front.
- **Dados de teste**: só em Development, a API cria `teste@sgt.local` (senha `12345678`) com as 5 categorias, se não houver nenhum usuário.

## Migrations

Para criar uma migration nova (precisa da ferramenta `dotnet-ef`):

```
dotnet tool install --global dotnet-ef --version 10.0.13
dotnet ef migrations add NomeDaMigration --project GestaoTempo.Api --output-dir Data/Migrations
```

Combinação do grupo: sempre faça `git pull` antes de criar uma migration. Duas migrations criadas em paralelo conflitam no arquivo `AppDbContextModelSnapshot.cs`.

Depois de uma migration nova, atualize o `docs/banco.sql`.

## Recriar o banco

Se o banco ficou com um esquema antigo, veja [problemas.md](problemas.md#relação-já-existe-ou-usuário-de-teste-não-entra).
