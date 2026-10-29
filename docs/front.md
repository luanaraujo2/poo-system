# Front-end

Vue 3 com PrimeVue 4 (tema Aura com o azul `#2457A6` do protótipo da Sprint 2), Vue Router 4 e Vite 7. Fica na pasta `frontend/`.

## Telas

| Tela | Rota | História | Arquivo |
|---|---|---|---|
| Entrar | `/entrar` | US02 | `views/EntrarView.vue` |
| Criar conta | `/criar-conta` | US01 | `views/CriarContaView.vue` |
| Minhas tarefas (lista, Categorias e Sair) | `/tarefas` | US02, US03 e US05 | `views/TarefasView.vue` |
| Nova tarefa | `/tarefas/nova` | US03 | `views/NovaTarefaView.vue` |
| Editar tarefa (com Excluir) | `/tarefas/{id}` | US05 | `views/NovaTarefaView.vue`, em modo edição |
| Categorias | `/categorias` | US04 | `views/CategoriasView.vue` |

`/` e qualquer rota desconhecida levam para `/tarefas`. As telas de tarefas exigem login; quem já está logado e abre `/entrar` ou `/criar-conta` é levado para `/tarefas`.

## Arquivos

| Arquivo | Função |
|---|---|
| `src/main.js` | Cria o app, registra PrimeVue, rotas e o aviso de sessão expirada |
| `src/router.js` | Rotas e proteção das telas que exigem login |
| `src/servicos/api.js` | Chamadas à API com o token. Em 401 com sessão ativa, avisa que a sessão expirou |
| `src/servicos/sessao.js` | Guarda o token, a validade e o usuário no `localStorage` (chave `sgt.sessao`). Sair é apagar |
| `src/constantes.js` | Textos dos enums e formatadores de duração e de prazo |
| `src/tema.js` | Preset do PrimeVue com o azul do protótipo |
| `src/estilo.css` | Cores, coluna central e classes usadas em mais de uma tela |

## Como chama a API

O front chama `/api/...` e o Vite repassa para `http://127.0.0.1:5224` (`vite.config.js`). Por isso não há problema de CORS no desenvolvimento. Usa-se `127.0.0.1` e não `localhost` porque em alguns PCs com Windows o `localhost` não conecta.

Os erros de regra voltam da API como `{ "mensagem": "..." }` e a tela mostra esse texto. Se a API estiver fora do ar, aparece "Não foi possível falar com o servidor".

## Tela "Nova tarefa"

- **Prazo**: campo de data do navegador, com atalhos Hoje, Amanhã e Em 1 semana.
- **Hora**: campo de texto opcional que aceita só números e coloca os dois-pontos sozinho (`1800` vira `18:00`). Em branco, vale 23:59.
- **Tempo estimado**: horas e minutos, com atalhos de 15min, 30min, 1h, 2h e 4h.
- **Categoria**: carregada de `GET /api/categorias`.
- O navegador confere o que dá para conferir; as regras da classe `Tarefa` quem responde é a API.

## Tela "Editar tarefa" (US05)

- Abre ao tocar num cartão de "Minhas tarefas". É a mesma tela do cadastro, já preenchida.
- Se o dia e a hora do prazo não forem mexidos, a tela manda o prazo gravado de volta. Assim uma tarefa atrasada pode ser editada sem trocar o prazo.
- **Excluir tarefa** abre uma confirmação. Se a tarefa tiver subtarefas, a confirmação avisa que elas também serão excluídas.

## Tela "Categorias" (US04)

- Abre pelo botão **Categorias** no topo de "Minhas tarefas".
- Mostra as categorias próprias (com lápis para renomear e lixeira para excluir) e as 5 padrão, que ficam travadas.
- **Nova categoria**: nome e uma de 8 cores.
- Excluir uma categoria com tarefas pede para escolher para qual categoria elas vão.

## Comandos

```
npm install      instala as dependências (só na primeira vez)
npm run dev      servidor de desenvolvimento em http://localhost:5173
npm run build    gera a versão final em dist/
npm run preview  abre a versão gerada
```
