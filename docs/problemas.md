# Problemas comuns

## "Relação já existe" ou usuário de teste não entra

O banco ficou com um esquema antigo. Pare a API e apague o banco (senha `postgres`):

```
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -c "DROP DATABASE sgt WITH (FORCE);"
```

Suba a API de novo: ela recria o banco, as tabelas e o usuário de teste.

## A API não conecta no banco

Confira se o PostgreSQL está rodando em `localhost:5432` com banco `sgt`, usuário `postgres` e senha `postgres`. Sem PostgreSQL instalado, use `docker compose up -d`.

## "Não foi possível falar com o servidor"

A API não está rodando, ou não está na porta 5224. Suba a API antes do front.

## O VS Code tenta depurar o `.csproj` ou o `.http`

Não rode com esses arquivos abertos. Abra um arquivo `.cs` e use **Executar > Executar sem Depuração**, perfil `C#: GestaoTempo.Api [http]`.

## O REST Client não conecta em `localhost`

Em alguns PCs com Windows o `localhost` não conecta pelo REST Client nem pelo Node. Troque `localhost` por `127.0.0.1` no `@host` do `GestaoTempo.Api.http` só na sua máquina, e não envie essa troca no commit. O proxy do front já usa `127.0.0.1`.

## Todos os testes de tarefa do `.http` dão 400

Os exemplos usam prazo em `2026-12-31`. Depois dessa data, a regra do prazo recusa o cadastro. Troque por uma data futura.

## Erro de migration ao dar `git pull`

Duas pessoas criaram migrations ao mesmo tempo e o `AppDbContextModelSnapshot.cs` conflitou. Combine antes com o grupo e refaça a migration mais nova depois do `git pull`.
