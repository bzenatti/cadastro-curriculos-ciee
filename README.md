# Desafio Técnico — Cadastro de Currículos

> Este README descreve o desafio, o contexto e o que se espera da documentação.
> Itens marcados com **(a preencher)** serão completados conforme o projeto for construído.

## Contexto

A equipe de recrutamento precisa **cadastrar e consultar candidatos**. A aplicação oferece duas formas de cadastro:

- **Cadastro manual:** a pessoa preenche o formulário e salva os dados, sem precisar enviar um documento.
- **Cadastro com PDF:** a pessoa envia um currículo; o backend extrai o texto e tenta identificar **nome, e-mail e telefone**. As informações encontradas preenchem o formulário e podem ser corrigidas ou complementadas antes de salvar.

Regras centrais:

- Os dois caminhos usam **o mesmo formulário e as mesmas regras de validação**.
- Depois de salvar, o candidato aparece em uma **listagem**, com acesso a uma **tela de detalhes**.
- O PDF é **opcional**: a ausência do arquivo ou uma falha na leitura **não pode impedir** o cadastro manual.

## Dados do cadastro

| Campo | Obrigatório |
| --- | --- |
| Nome completo | Sim |
| E-mail | Sim (com validação de formato) |
| Telefone | Não |
| Área ou cargo de interesse | Não |
| Resumo profissional | Não |

## Tecnologias

| Camada | Exigência do desafio | Escolha |
| --- | --- | --- |
| Frontend | Angular ou React | React (Vite + TypeScript) |
| Backend | ASP.NET Core (.NET) ou Node.js | ASP.NET Core (.NET 10) |
| Banco de dados | SQL Server | SQL Server (via Docker) |

As demais bibliotecas ficam a critério de quem desenvolve. **Versões utilizadas: (a preencher).**

## Requisitos da aplicação

- Interface simples e funcional, integrada ao backend.
- Leitura do PDF realizada **pelo backend**.
- Cadastro e consulta dos dados pelo backend, com persistência no SQL Server.
- Scripts ou migrations para criar a estrutura do banco.
- Validação dos campos obrigatórios e do formato do e-mail.
- Validação do arquivo enviado: apenas **PDF de até 5 MB**.
- Mensagens claras para arquivo inválido, falha na leitura e cadastro salvo.

A extração **não precisa funcionar com qualquer currículo**. Quando uma informação não for identificada, o formulário deve permitir o preenchimento manual. As **limitações** da solução devem ser documentadas.

## API

Todas as rotas ficam sob `/api`, em JSON (campos em `camelCase`, datas em ISO 8601 UTC). O frontend chama sempre caminhos relativos (`/api/...`): em desenvolvimento o Vite repassa ao backend (proxy) e, no Docker, o nginx faz o mesmo.

| Rota | Envia | Sucesso | Erros |
| --- | --- | --- | --- |
| `POST /api/candidates` | JSON `{ name, email, phone, position, summary }` | `201` com o candidato criado | `400` validação (com `errors`); `409` e-mail já cadastrado |
| `GET /api/candidates?page=1&pageSize=10` | `page` (padrão 1) e `pageSize` (padrão 10, máximo 50) | `200` `{ items, page, pageSize, totalCount }` | |
| `GET /api/candidates/{id}` | | `200` com o candidato | `404` |
| `POST /api/resumes/parse` | `multipart/form-data`, parte `file` | `200` `{ name, email, phone }`, com `null` no que não foi achado | `400` arquivo ausente, não é PDF ou passa de 5 MB; `422` PDF ilegível (corrompido, protegido por senha ou sem texto) |

**Formatos.** Item da lista: `{ id, name, email, position, createdAt }`. Candidato (detalhe e resposta do cadastro): `{ id, name, email, phone, position, summary, createdAt }`. Campos opcionais vazios: a requisição aceita `""` ou `null`, e a resposta devolve `null`. A lista vem dos mais recentes para os mais antigos (`createdAt` e `id` decrescentes). Páginas ou tamanhos fora do intervalo são ajustados, sem erro.

**Erros.** Todo erro segue o `ProblemDetails` (RFC 9457): `{ type, title, status, detail, traceId }`, sem stack trace. O `detail` é uma mensagem em português pronta para mostrar. O `400` de validação traz também `errors`, no formato `{ campo: ["mensagem"] }`, com os mesmos nomes do formulário (`name`, `email`, ...).

**Validação.** Backend e frontend usam as mesmas regras (o backend é a fonte da verdade): nome e e-mail obrigatórios; e-mail com `^[^@\s]+@[^@\s]+\.[^@\s]+$`; telefone opcional, só números, com `^[0-9]{10,11}$`; tamanhos máximos nome 150, e-mail 254, telefone 20, cargo 100 e resumo 2000. O e-mail é único por índice no banco, sem diferenciar maiúsculas de minúsculas.

**PDF.** O `parse` não grava nada, não guarda o arquivo e não registra o texto extraído. Só o `POST /api/candidates` grava.

## Como executar

Requisito: [Docker](https://docs.docker.com/get-docker/) com Docker Compose. Não precisa instalar .NET nem Node.

```bash
cp .env.example .env           # senha de exemplo, só para o container local
docker compose up -d --build   # a primeira vez demora: baixa imagens e pacotes
```

Abra **http://localhost:8080**. A API responde no mesmo endereço, em `/api` (ex.: `http://localhost:8080/api/candidates`).

- Na primeira subida a API cria o banco `CandidatesDb` e a tabela sozinha (migrations); não há script para rodar.
- Depois de alterar o código, rode `docker compose up -d --build` de novo.
- `docker compose down` para tudo e mantém os dados; `docker compose down -v` também apaga o banco.
- Se a porta 8080 (`web`) ou a 1434 (`db`) estiver ocupada, troque o número da esquerda em `ports`, no `docker-compose.yml`.

### Possível problema: o build falha ao baixar pacotes (rede só com IPv6)

**Sintoma:** `docker compose up --build` falha no `dotnet restore` (`NU1301 ... Resource temporarily unavailable`) ou no `npm ci`, embora o navegador acesse a internet.

**Causa:** a sua rede tem só IPv6 (sem IPv4) e o Docker cria redes IPv4, então o build não sai para a internet. Para confirmar, o primeiro comando falha e o segundo responde:

```bash
curl -4 -sI https://api.nuget.org/v3/index.json | head -1
curl -6 -sI https://api.nuget.org/v3/index.json | head -1
```

**Solução:** crie `docker-compose.override.yml` na raiz do projeto (o Compose o lê sozinho; vale só para a sua máquina, não o comite) para que o build use a rede do computador:

```yaml
services:
  api:
    build:
      network: host
  web:
    build:
      network: host
```

Rode `docker compose up -d --build` de novo. Em execução os containers não precisam de internet. Testado no Linux. Não configure DNS IPv4 (como `8.8.8.8`) em `/etc/docker/daemon.json`: sem IPv4 ele não é alcançado e o build continua falhando.

## Desenvolvimento local

Para alterar o código com resposta rápida, suba só o banco no Docker e rode a API e o front na sua máquina (precisa do .NET 10 SDK e do Node 22). A API lê a connection string da configuração `ConnectionStrings:Default`; no Docker completo, quem a define é o `docker-compose.yml`.

**1. Subir o banco**

```bash
cp .env.example .env       # senha de exemplo, só para o container local (pule se já fez em "Como executar")
docker compose up -d db    # SQL Server na porta 1434 do seu computador
docker compose ps          # aguarde o status "healthy"
```

**2. Informar a connection string à API**

Para rodar a API fora do Docker (`dotnet run`, `dotnet ef`), guarde a string no `user-secrets`, que fica fora do repositório:

```bash
. ./.env
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1434;Database=CandidatesDb;User Id=sa;Password=$DB_PASSWORD;TrustServerCertificate=True" --project backend/src/Candidates.Api
```

O `user-secrets` vale só para o computador onde o comando foi executado. Em outra máquina, repita o passo 2.

Formato da string, sem credencial real:

```text
Server=localhost,1434;Database=CandidatesDb;User Id=sa;Password=<senha do .env>;TrustServerCertificate=True
```

- `Server=localhost,1434`: endereço e porta (depois da **vírgula**) publicada pelo Docker.
- `Database=CandidatesDb`: criado pelas migrations.
- `TrustServerCertificate=True`: o container usa certificado autoassinado.
- A senha do `sa` precisa ter 8+ caracteres, com maiúscula, minúscula e número ou símbolo (regra do SQL Server), e não pode conter `;`.
- Se a porta 1434 estiver ocupada, troque o número da esquerda em `docker-compose.yml` e na string.

**3. Criar a estrutura do banco**

A tabela `Candidates` vem de uma migration (`backend/src/Candidates.Api/Data/Migrations`). Há dois jeitos de aplicá-la; use um só:

- **Automático:** ao subir a API, ela cria o banco `CandidatesDb` (se não existir) e aplica as migrations pendentes. Isso é controlado por `Database:MigrateOnStartup`, ligado em `appsettings.Development.json` (`dotnet run`) e no `docker-compose.yml`. Não precisa de mais nada:

  ```bash
  dotnet run --project backend/src/Candidates.Api
  ```

- **Pelo `dotnet ef`:**

  ```bash
  cd backend
  dotnet tool restore                  # instala o dotnet-ef (versão fixada em backend/dotnet-tools.json)
  dotnet restore src/Candidates.Api    # sem isso, o dotnet ef falha com NETSDK1004 em um clone novo
  dotnet ef database update --project src/Candidates.Api
  ```

**Prefere o SQL?** O repositório não traz um `.sql` pronto: as migrations são a fonte da verdade e um script versionado ficaria desatualizado a cada migration nova. Gere um na hora, depois do passo 2 e dos dois comandos de restore acima, a partir de `backend/`:

```bash
dotnet ef migrations script --idempotent --project src/Candidates.Api --output schema.sql
```

O `--idempotent` faz o script verificar quais migrations já foram aplicadas, então ele pode ser executado mais de uma vez. O script não cria o banco: crie o `CandidatesDb` antes e execute o `schema.sql` nele, em um cliente SQL conectado em `localhost,1434`, com o usuário `sa` e a senha do `.env`.

**4. Rodar a API e o front**

Em dois terminais, a partir da raiz do projeto:

```bash
dotnet run --project backend/src/Candidates.Api    # API em http://localhost:5206
cd frontend && npm install && npm run dev          # front em http://localhost:5173
```

Abra **http://localhost:5173**: o Vite repassa `/api` para a API na porta 5206. Se usou o jeito automático do passo 3, a API já está rodando.


## O que este README deve conter

- [ ] Tecnologias e **versões** utilizadas
- [ ] Requisitos para executar (SDK, Node, Docker etc.)
- [ ] Como **configurar a conexão** com o SQL Server (exemplo sem credenciais reais)
- [ ] Como **criar a estrutura do banco** (migrations ou scripts)
- [ ] Como **executar** a aplicação (backend e frontend)
- [ ] Como **rodar os testes**
- [ ] **Limitações** da extração de dados do PDF

## O que o repositório deve conter

- [ ] Código-fonte do frontend e do backend
- [ ] `README.md` (este arquivo)
- [ ] `DESENVOLVIMENTO.md` com o relato do desenvolvimento
- [ ] Exemplos de configuração sem credenciais reais
- [ ] Scripts ou migrations do banco de dados
- [ ] Um currículo fictício em PDF para testar a importação
- [ ] Histórico de commits que acompanhe a evolução do trabalho

## O que o DESENVOLVIMENTO.md deve explicar

- Como o trabalho foi organizado e executado
- Principais decisões técnicas e seus motivos
- Ferramentas de IA e modelos utilizados
- Em quais etapas a IA ajudou, com exemplos de pedidos e como as respostas foram aproveitadas
- O que foi corrigido, adaptado ou descartado
- Como a solução foi verificada
- Tempo aproximado dedicado ao desafio
- Dificuldades, limitações e melhorias com mais tempo

## Critérios de avaliação

1. Funcionamento dos cadastros manual e com PDF, da listagem e da consulta de detalhes
2. Integração entre frontend, backend e SQL Server
3. Clareza e organização do código
4. Validações e tratamento de erros
5. Relevância dos testes
6. Facilidade para configurar e executar o projeto
7. Clareza na documentação e capacidade de explicar as decisões

> Diretriz: preferir uma solução **simples, funcional** e que possa ser compreendida e evoluída.
