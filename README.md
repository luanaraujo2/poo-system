# SGT - Sistema de Gestão do Tempo

Projeto FullStack para um melhor aproveitamento do seu tempo.

**Integrantes:** Luan Araujo de Oliveira, Murilo Cipriano Carvalho, João Vitor de Carvalho Loureiro e Henrique de Paula Balbino.

## Escopo da Sprint 3

| US | História | Situação |
|---|---|---|
| US01 | Cadastrar conta (nome, e-mail, senha e confirmação de senha) | Pronta: API e tela "Criar conta"; confirmação validada no front-end |
| US02 | Entrar e sair do sistema | Pronta: login com token, tela "Entrar" e botão "Sair" |
| US03 | Cadastrar tarefa | Pronta: API e tela "Nova tarefa" (regra do prazo ainda a combinar, ver [decisões](docs/decisoes.md)) |

## Documentação

| Arquivo | Conteúdo |
|---|---|
| [docs/historias.md](docs/historias.md) | Critérios de aceite das US01, US02 e US03 e quem fez cada parte |
| [docs/api.md](docs/api.md) | Endpoints, exemplos de requisição e resposta, regras e erros |
| [docs/banco.md](docs/banco.md) | Tabelas, decisões do banco e migrations |
| [docs/banco.sql](docs/banco.sql) | Script SQL do esquema, para entrega e consulta |
| [docs/front.md](docs/front.md) | Telas, rotas, sessão e como o front chama a API |
| [docs/decisoes.md](docs/decisoes.md) | Decisões tomadas e pendências a combinar com o grupo |
| [docs/problemas.md](docs/problemas.md) | Problemas comuns ao rodar e como resolver |

## Tecnologias

- C# / .NET 10, ASP.NET Core Web API com controllers
- Entity Framework Core 10 + Npgsql
- PostgreSQL 16
- Vue 3 + Vue Router + PrimeVue 4 + Vite (front-end)
- Token JWT e BCrypt (login)

## Estrutura

```
SGT.slnx
GestaoTempo.Domain/            regras de negócio, sem dependência de banco
  Entities/                    Usuario, Categoria, Tarefa
  Enums/                       StatusTarefa, Criticidade, Complexidade, Prioridade
GestaoTempo.Api/               API web
  Controllers/                 Usuarios, Autenticacao, Tarefas, Categorias
  Data/                        AppDbContext, DadosDeTeste, Migrations/
  Dtos/                        objetos de entrada e saída da API
  Services/                    UsuarioAtual, GeradorDeToken
  GestaoTempo.Api.http         testes manuais (extensão REST Client)
frontend/                      telas em Vue + PrimeVue
  src/views/                   EntrarView, CriarContaView, TarefasView, NovaTarefaView
  src/servicos/                api.js (chamadas à API) e sessao.js (token do login)
docs/                          documentação e script SQL
docker-compose.yml             PostgreSQL local (opcional)
```

## Como rodar

### Pré-requisitos

- .NET SDK 10
- Node.js 20.19 ou mais novo
- PostgreSQL 16 em `localhost:5432`, banco `sgt`, usuário `postgres` e senha `postgres`
  (valores em `GestaoTempo.Api/appsettings.Development.json`)

Sem PostgreSQL instalado, dá para subir um pelo Docker:

```
docker compose up -d
```

### API

```
dotnet run --project GestaoTempo.Api --launch-profile http
```

A API fica em `http://localhost:5224`. Em Development, ao iniciar ela aplica as migrations e, se o banco estiver sem usuários, cria o usuário de teste `teste@sgt.local` (senha `12345678`) com as 5 categorias padrão.

No VS Code: abra um arquivo `.cs` e use **Executar > Executar sem Depuração**, perfil `C#: GestaoTempo.Api [http]`. Não rode com o `.csproj` ou o `.http` aberto, porque o VS Code tenta depurar esses arquivos.

### Front-end

Com a API rodando, em outro terminal:

```
cd frontend
npm install
npm run dev
```

O `npm install` só é preciso na primeira vez. Abra `http://localhost:5173` e entre com `teste@sgt.local` / `12345678` ou crie uma conta.

### Rodar pelo VS Code, sem digitar comandos

Pré-requisitos instalados e PostgreSQL ligado. Na primeira vez, rode `npm install` uma vez dentro da pasta `frontend` (ver acima); depois não precisa mais.

API:

1. Na barra lateral, abra `GestaoTempo.Api` e clique em `Program.cs` para deixá-lo aberto.
2. No menu de cima, clique em **Executar > Executar sem Depuração**.
3. Se perguntar o depurador, escolha **C#** e o perfil `C#: GestaoTempo.Api [http]`.
4. Espere aparecer `Now listening on: http://localhost:5224` no painel de baixo.

Front-end:

1. Na barra lateral, abra a pasta `frontend` e clique em `package.json`.
2. Passe o mouse em cima da palavra `dev`, na linha `"dev": "vite",` dentro de `"scripts"`.
3. Na caixinha que aparece, clique em **Executar Script**. Se a caixinha não aparecer, clique em **Depurar**, escrito acima da linha `"scripts"`, e escolha `dev`.
4. No terminal que abrir, segure **Ctrl** e clique em `http://localhost:5173/`.

Para parar: o quadrado vermelho da barra de depuração encerra a API; a lixeira do terminal encerra o front-end.

### Testar a API

Abra `GestaoTempo.Api/GestaoTempo.Api.http` no VS Code (extensão REST Client) e clique em **Send Request** acima de cada teste. O resultado esperado está no título de cada um: A a G cobrem cadastro, login e token (US01 e US02), 1 a 12 cobrem as tarefas (US03) e 13 a 16 cobrem as subtarefas. Os prazos são relativos à data de hoje.

Algo não funcionou? Veja [docs/problemas.md](docs/problemas.md).

## Configuração

| Chave | Arquivo | Para que serve |
|---|---|---|
| `ConnectionStrings:Sgt` | `appsettings.Development.json` | Conexão com o PostgreSQL |
| `Jwt:Chave` | `appsettings.Development.json` | Chave de assinatura do token, com pelo menos 32 caracteres. Em produção precisa ser trocada |
| `Jwt:Emissor` | `appsettings.json` | Emissor e audiência do token |
| `Jwt:ValidadeHoras` | `appsettings.json` | Validade do token (8 horas) |

## Fluxo de trabalho do grupo

- `master`: base do grupo. Não se faz push direto nela.
- Cada tarefa: criar uma branch a partir da `master` atualizada, fazer os commits, enviar e abrir um pull request para a `master`.
- Antes de criar uma migration, faça `git pull` (ver [docs/banco.md](docs/banco.md#migrations)).
