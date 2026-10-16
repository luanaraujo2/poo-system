# SGT - Sistema de Gestão do Tempo

Projeto FullStack para um melhor aproveitamento do seu tempo.

**Integrantes:** Luan Araujo de Oliveira, Murilo Cipriano Carvalho, João Vitor de Carvalho Loureiro e Henrique de Paula Balbino.

## Escopo da Sprint 3

| US | História | Situação |
|---|---|---|
| US01 | Cadastrar conta (nome, e-mail, senha) | Classe `Usuario` e banco prontos. Falta o endpoint de cadastro |
| US02 | Entrar e sair do sistema | Banco pronto. Falta o endpoint de login |
| US03 | Cadastrar tarefa | API pronta e testada. Falta a tela |

Detalhes de cada história, e como o banco foi feito, na seção [O que foi feito, por história](#o-que-foi-feito-por-história).

## O que foi feito, por história

- **Domain** (classes `Usuario`, `Categoria` e `Tarefa` e os enums): Luan.
- **Banco de dados** das três histórias e **US03** inteira (API): João Vitor, em cima das classes do Domain.

### Banco de dados (base das US01, US02 e US03)

O banco foi feito com EF Core (code-first): as classes do Domain são mapeadas em `AppDbContext` e as tabelas são criadas pela migration `CriacaoInicial`, aplicada sozinha quando a API sobe em Development. Tabelas e colunas seguem o DER, em `snake_case`.

A migration cria:

- as tabelas `usuario`, `categoria` e `tarefa`, com chaves primárias, chaves estrangeiras e índices;
- a extensão `citext`, usada no `usuario.email` para o e-mail único e o login ignorarem maiúsculas e minúsculas;
- CHECKs em `tarefa`, para o banco recusar dados fora das regras;
- índice único de nome de categoria por usuário e índices para listar tarefas por status e por prazo.

Decisões que valem para o grupo:

- **Senha**: `senha_hash` aceita até 255 caracteres, o suficiente para BCrypt ou PBKDF2. A senha nunca é gravada em texto puro.
- **E-mail único sem caixa**: `Joao@x.com` e `joao@x.com` contam como o mesmo e-mail. A comparação `u.Email == email` em consultas já ignora a caixa.
- **Categorias**: pertencem ao usuário (`categoria.id_usuario`) e o nome é único por usuário. As 5 padrão são criadas pelo construtor de `Usuario`; ao salvar o usuário, as categorias são salvas junto.
- **Regras no banco**: `estimativa_min > 0`, título preenchido, `tempo_real_min >= 0` e valores válidos de status, criticidade, complexidade e prioridade. Ao criar um valor novo em algum enum, atualize o CHECK correspondente em `AppDbContext`.
- **Datas**: as classes usam `DateTime.Now`. O `AppDbContext` converte para UTC ao gravar, porque o PostgreSQL (`timestamptz`) só aceita UTC. A API devolve as datas em UTC (terminadas em `Z`).
- **Tarefas**: pertencem ao usuário (`tarefa.id_usuario`). Todas as consultas filtram por ele.
- **Sessão**: não há tabela de sessão. O login usa token, então "sair" é só descartar o token no front.
- **Dados de teste**: só em Development, a API cria `teste@sgt.local` (id 1) com as 5 categorias.

Pontos para combinar com quem mexer no Domain:

- `Usuario`, `Categoria` e `Tarefa` não têm construtor vazio, então o EF Core usa o construtor com parâmetros quando lê do banco. Em `Usuario`, isso cria 5 categorias novas na memória a cada leitura, então a lista `Categorias` vem com categorias que não existem no banco. Em `Tarefa`, a validação de prazo roda na leitura e dá erro em tarefa atrasada. Por isso a API da US03 lê tarefas e categorias por projeção (`Select`), sem montar as entidades. Para a US01/US02, vale adicionar um construtor privado vazio (`private Usuario() { }`, e o mesmo em `Tarefa` e `Categoria`), que o EF Core passa a usar.
- `Categoria` e os enums estão sem `namespace` (ficam no namespace global).

### US01 - Cadastrar conta

| Critério | Situação |
|---|---|
| Nome, e-mail e senha | Classe `Usuario` (nome com 3 ou mais letras, e-mail com @) e colunas em `usuario` prontas |
| Não aceita e-mail repetido | Pronto no banco (índice único `IX_usuario_email`, sem diferenciar caixa) |
| Senha salva com hash | Coluna `senha_hash` pronta |
| Conta começa com as 5 categorias | Pronto: o construtor de `Usuario` cria as categorias |
| Endpoint de cadastro | **Falta**: gerar o hash, salvar o usuário (as categorias vão junto) e tratar e-mail repetido (409 ou 400) |

### US02 - Entrar e sair do sistema

| Critério | Situação |
|---|---|
| Login com e-mail e senha | Consulta por `usuario.email` pronta. **Falta** o endpoint |
| Erro sem dizer qual campo está errado | **Falta**: responder a mesma mensagem para e-mail inexistente e senha errada |
| Cada usuário só vê as próprias tarefas | Pronto: `UsuarioAtual` lê o id do token (claim `NameIdentifier`) e os endpoints filtram por ele |
| Sair | Descartar o token no front |

Integração com a US03: o login precisa gerar um token que traga o id do usuário na claim `NameIdentifier`. Com isso, o cabeçalho `X-Usuario-Id` (só Development) deixa de ser necessário.

### US03 - Cadastrar tarefa

Pronta no back-end, testada pelo `GestaoTempo.Api.http`.

| Critério | Situação |
|---|---|
| Campos: título, descrição, prazo, tempo estimado, categoria, criticidade, complexidade e prioridade | Pronto |
| Categoria entre as 5 padrão | Pronto (`GET /api/categorias`; a categoria precisa ser do usuário) |
| Título e tempo estimado obrigatórios | Pronto (400 com mensagem) |
| Prazo não pode ser data que já passou | Pronto (regra da classe `Tarefa`: o prazo precisa ser depois do momento do cadastro) |
| Tarefa nova começa como Pendente | Pronto |
| Tela "Nova tarefa" (Vue.js) | **Falta** |

### Ainda falta no projeto

- Endpoints de cadastro (US01) e de login (US02).
- Front-end em Vue.js: tela "Nova tarefa", cadastro e login.

## Tecnologias

- C# / .NET 10, ASP.NET Core Web API com controllers
- Entity Framework Core 10 + Npgsql
- PostgreSQL 16
- Vue.js (front-end, ainda não iniciado; o CORS já libera `http://localhost:5173`)

## Estrutura

```
SGT.slnx
GestaoTempo.Domain/            regras de negócio, sem dependência de banco
  Entities/                    Usuario, Categoria, Tarefa
  Enums/                       StatusTarefa, Criticidade, Complexidade, Prioridade
GestaoTempo.Api/               API web
  Controllers/                 TarefasController, CategoriasController
  Data/                        AppDbContext, DadosDeTeste, Migrations/
  Dtos/                        objetos de entrada e saída da API
  Services/                    UsuarioAtual
  GestaoTempo.Api.http         testes manuais (extensão REST Client)
docker-compose.yml             PostgreSQL local (opcional)
docs/banco.sql                 script SQL do esquema do banco (para entrega e consulta)
```

## Como rodar

### Pré-requisitos

- .NET SDK 10
- PostgreSQL 16 em `localhost:5432` com banco `sgt`, usuário `postgres` e senha `postgres`
  (valores em `GestaoTempo.Api/appsettings.Development.json`)

Sem PostgreSQL instalado, dá para subir um pelo Docker:

```
docker compose up -d
```

### Subir a API

```
dotnet run --project GestaoTempo.Api --launch-profile http
```

A API fica em `http://localhost:5224`. Em ambiente Development, ao iniciar ela:

1. aplica as migrations pendentes (cria o banco, as tabelas e a extensão `citext`);
2. cria o usuário de teste `teste@sgt.local` (id 1) com as 5 categorias padrão, se o banco estiver sem usuários.

O usuário que faz a requisição é identificado pela claim `NameIdentifier` do token. Enquanto o login (US02) não existe, em Development vale o cabeçalho `X-Usuario-Id: 1`.

### Testar

Abra `GestaoTempo.Api/GestaoTempo.Api.http` no VS Code (extensão REST Client) e clique em **Send Request** acima de cada teste. O resultado esperado está no título de cada um.

## API

Todos os endpoints respondem em JSON. Enums trafegam como texto.

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/categorias` | Categorias do usuário |
| POST | `/api/tarefas` | Cadastra tarefa (201 ou 400) |
| GET | `/api/tarefas` | Lista as tarefas do usuário, por prazo |
| GET | `/api/tarefas/{id}` | Obtém uma tarefa do usuário (404 se não for dele) |

### Cadastrar tarefa

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

Valores aceitos:

| Campo | Valores |
|---|---|
| `criticidade` | `Baixa`, `Media`, `Alta`, `Critica` |
| `complexidade` | `Baixa`, `Media`, `Alta` |
| `prioridade` | `Urgente_Importante`, `Importante_NaoUrgente`, `Urgente_NaoImportante`, `NaoUrgente_NaoImportante` |

Erros de regra de negócio voltam com status 400 e `{ "mensagem": "..." }`.

### Regras da US03

As regras de título, tempo estimado e prazo ficam no construtor da classe `Tarefa`; a API confere o restante.

- Título obrigatório, com pelo menos 3 e no máximo 150 caracteres.
- Tempo estimado maior que zero.
- Prazo obrigatório e depois do momento do cadastro.
- Criticidade, complexidade e prioridade obrigatórias.
- Toda tarefa nova começa como `Pendente`.
- A categoria precisa existir e pertencer ao usuário (as 5 padrão: Trabalho, Estudos, Pessoal, Familia e Relacionamentos).
- Cada usuário só enxerga as próprias tarefas.

## Banco de dados

| Tabela | Colunas principais |
|---|---|
| `usuario` | `id_usuario`, `nome`, `email` (único, `citext`), `senha_hash`, `criado_em` |
| `categoria` | `id_categoria`, `id_usuario`, `nome`, `cor`, `padrao` |
| `tarefa` | `id_tarefa`, `id_usuario`, `id_categoria`, `titulo`, `descricao`, `prazo`, `estimativa_min`, `tempo_real_min`, `criticidade`, `complexidade`, `prioridade`, `status`, `criada_em`, `concluida_em` |

Os enums são gravados como texto. A coluna de subtarefa (`id_tarefa_pai`) do DER entra quando a classe `Tarefa` tiver subtarefas.

### Migrations

Para criar uma migration nova (precisa da ferramenta `dotnet-ef`):

```
dotnet tool install --global dotnet-ef --version 10.0.13
dotnet ef migrations add NomeDaMigration --project GestaoTempo.Api --output-dir Data/Migrations
```

Combinação do grupo: sempre faça `git pull` antes de criar uma migration. Duas migrations criadas em paralelo conflitam no arquivo `AppDbContextModelSnapshot.cs`.

## Branches

- `master`: base do grupo. Não se faz push direto nela.
- `us01-us02-us03-banco-de-dados-e-cadastrar-tarefa`: banco das US01, US02 e US03 e a API da US03, em cima do Domain da `master`.
