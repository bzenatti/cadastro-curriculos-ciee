# Cadastro de Currículos

Aplicação web para cadastrar e consultar candidatos. O cadastro pode ser manual ou a partir de um currículo em PDF: o backend lê o arquivo, tenta achar nome, e-mail e telefone e preenche o formulário, que a pessoa corrige e completa antes de salvar. O PDF é opcional, e se a leitura falhar o cadastro manual continua funcionando.

Os dois caminhos usam o mesmo formulário e as mesmas validações:

| Campo | Obrigatório |
| --- | --- |
| Nome completo | Sim |
| E-mail | Sim (com validação de formato) |
| Telefone | Não |
| Área ou cargo de interesse | Não |
| Resumo profissional | Não |

Depois de salvo, o candidato aparece na listagem (paginada) e tem uma tela de detalhes.

Decisões, uso de IA e limitações estão no [DESENVOLVIMENTO.md](DESENVOLVIMENTO.md).

## Tecnologias

| Camada | Tecnologia |
| --- | --- |
| Frontend | React 19, Vite 8, TypeScript 6, React Router 7 |
| Backend | ASP.NET Core 10 (.NET 10), Entity Framework Core 10, PdfPig 0.1.16 (leitura do PDF) |
| Banco | SQL Server 2022 (imagem Docker) |
| Testes | xUnit e Testcontainers (backend), Vitest e Testing Library (frontend) |
| Execução | Docker Compose; o nginx serve o front e repassa `/api` para a API |

## Como executar

Precisa só do [Docker](https://docs.docker.com/get-docker/) com Docker Compose (testado com Docker 29 e Compose v5). Não precisa instalar .NET nem Node.

Baixe o projeto (ou o ZIP pelo botão **Code** do GitHub, descompactando e entrando na pasta) e suba tudo:

```bash
git clone <URL do repositório>
cd <pasta criada pelo clone>
cp .env.example .env           # senha de exemplo, só para o container local
docker compose up -d --build   # na primeira vez baixa imagens e pacotes, então demora
```

Abra **http://localhost:8080**. A API responde no mesmo endereço, em `/api` (ex.: `http://localhost:8080/api/candidates`).

- Na primeira subida a API cria o banco `CandidatesDb` e a tabela sozinha, por migrations. Não há script para rodar.
- Depois de mudar o código, rode `docker compose up -d --build` de novo.
- Para parar ou apagar tudo, veja [Parar e apagar tudo](#parar-e-apagar-tudo).
- Se a porta 8080 (`web`) ou a 1434 (`db`) estiver ocupada, troque o número da esquerda em `ports`, no `docker-compose.yml`.

### Parar e apagar tudo

Na pasta do projeto, em bash (Linux, macOS ou WSL):

```bash
docker compose down                  # para os containers e mantém o banco
docker compose down -v --rmi local   # apaga também o banco e as imagens da API e do front
```

Sobra a imagem do SQL Server (cerca de 2 GB, usada também nos testes): `docker rmi mcr.microsoft.com/mssql/server:2022-latest`.

### Possível problema: o build falha ao baixar pacotes (rede só com IPv6)

Se o `docker compose up --build` falhar no `dotnet restore` (`NU1301`) ou no `npm ci` com a internet funcionando, a rede provavelmente tem só IPv6 e o build do Docker não sai para a internet. Crie o `docker-compose.override.yml` na raiz (vale só para a sua máquina, não faça commit) e rode `docker compose up -d --build` de novo:

```yaml
services:
  api:
    build:
      network: host
  web:
    build:
      network: host
```

Testado no Linux.

## Desenvolvimento local

Para mudar o código com resposta rápida, suba só o banco no Docker e rode a API e o front na sua máquina. Precisa do .NET 10 SDK e do Node 22.

### 1. Subir o banco

```bash
cp .env.example .env       # pule se já fez em "Como executar"
docker compose up -d db    # SQL Server na porta 1434 do seu computador
docker compose ps          # espere o status "healthy"
```

### 2. Informar a connection string à API

A API lê a string de `ConnectionStrings:Default`. Fora do Docker (`dotnet run`, `dotnet ef`), guarde-a no `user-secrets`, que fica fora do repositório:

```bash
. ./.env
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1434;Database=CandidatesDb;User Id=sa;Password=$DB_PASSWORD;TrustServerCertificate=True" --project backend/src/Candidates.Api
```

O `user-secrets` vale só para a máquina onde o comando rodou. Em outra, repita este passo. No Docker completo, quem define a string é o `docker-compose.yml`.

Formato, sem credencial real:

```text
Server=localhost,1434;Database=CandidatesDb;User Id=sa;Password=<senha do .env>;TrustServerCertificate=True
```

- `Server=localhost,1434`: endereço e porta (depois da **vírgula**) publicada pelo Docker.
- `Database=CandidatesDb`: criado pelas migrations.
- `TrustServerCertificate=True`: o container usa certificado autoassinado.
- A senha do `sa` precisa ter 8+ caracteres, com maiúscula, minúscula e número ou símbolo (regra do SQL Server), e não pode ter `;`.
- Se a porta 1434 estiver ocupada, troque-a no `docker-compose.yml` e na string.

### 3. Criar a estrutura do banco

A tabela `Candidates` vem de uma migration (`backend/src/Candidates.Api/Data/Migrations`). Use um destes jeitos:

- **Automático:** ao subir a API, ela cria o banco `CandidatesDb` (se não existir) e aplica as migrations pendentes. Isso depende de `Database:MigrateOnStartup`, ligado em `appsettings.Development.json` (para o `dotnet run`) e no `docker-compose.yml`. Não precisa de mais nada:

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

**Prefere o SQL?** Não há `.sql` versionado: as migrations são a fonte da verdade. Para gerar um, rode a partir de `backend/`, depois do passo 2 e dos dois restores acima:

```bash
dotnet ef migrations script --idempotent --project src/Candidates.Api --output schema.sql
```

Crie o banco `CandidatesDb` e execute o `schema.sql` nele, em `localhost,1434`, com o usuário `sa` e a senha do `.env`.

### 4. Rodar a API e o front

Em dois terminais, a partir da raiz:

```bash
dotnet run --project backend/src/Candidates.Api    # API em http://localhost:5206
```

```bash
cd frontend && npm install && npm run dev          # front em http://localhost:5173
```

Abra **http://localhost:5173**: o Vite repassa `/api` para a API na porta 5206. Se você usou o jeito automático do passo 3, a API já está rodando.

## Testes

Backend. Os testes de integração sobem um SQL Server num container e precisam do Docker rodando:

```bash
dotnet test backend/Candidates.slnx
dotnet test backend/Candidates.slnx --filter "Category!=Integration"   # só os unitários, sem Docker
```

Eles rodam no ambiente `Testing`, com um banco próprio (`CandidatesTests`) criado pelas migrations, sem tocar no de desenvolvimento.

| Requisito | Testes |
| --- | --- |
| Validação dos campos | `Dtos/CreateCandidateRequestTests`, `Integration/CandidatesApiTests` |
| Cadastro, listagem, detalhe, e-mail único (409, inclusive simultâneo) e 404 | `Integration/CandidatesApiTests` |
| Validação do arquivo (PDF até 5 MB) e mensagens de erro | `Services/PdfFileValidatorTests`, `Integration/ResumesApiTests` |
| Leitura do PDF e extração dos dados | `Services/PdfTextExtractorTests`, `Services/ResumeParserTests`, `Integration/ResumesApiTests` (PDFs de `samples/`) |
| Erros em `ProblemDetails`, sem detalhes internos | `Errors/GlobalExceptionHandlerTests` |

Não há testes de autenticação nem de limite de requisições, porque a API não tem nenhum dos dois ([Melhorias futuras](DESENVOLVIMENTO.md#melhorias-futuras)). O servidor de teste não aplica o limite de corpo do Kestrel; esse corte só é coberto no teste do tratador de erros.

Frontend:

```bash
cd frontend
npm install          # só na primeira vez
npm test -- --run    # sem o --run, o Vitest fica observando as mudanças
```

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

**Validação.** Backend e frontend usam as mesmas regras, e o backend é a fonte da verdade: nome e e-mail obrigatórios; e-mail com `^[^@\s]+@[^@\s]+\.[^@\s]+$`; telefone opcional, só números, com `^[0-9]{10,11}$`; tamanhos máximos: nome 150, e-mail 254, telefone 20, cargo 100 e resumo 2000. O e-mail é único por índice no banco, sem diferenciar maiúsculas de minúsculas.

## Importação de PDF

O backend lê as 5 primeiras páginas e procura nome, e-mail e telefone no texto. É uma heurística: ela sugere os dados e a pessoa confere tudo antes de salvar. A leitura não grava nada, e o arquivo não é guardado.

Limitações:

- **PDF sem texto** (escaneado, só imagem), corrompido ou protegido por senha não é lido: a API responde `422` e o cadastro manual segue disponível. Não há OCR.
- **Nome:** é a primeira linha, entre as 10 primeiras, que parece um nome (2 a 6 palavras com inicial maiúscula). Um cargo acima do nome pode ser tomado como nome, e rótulos como `Nome: Fulana` não são reconhecidos.
- **E-mail e telefone:** vale o primeiro de cada. O telefone só é reconhecido no formato brasileiro (DDD + 8 ou 9 dígitos, com ou sem +55).
- **Layout:** em currículos de duas colunas, a ordem do texto pode vir misturada.

Mais detalhes em [DESENVOLVIMENTO.md](DESENVOLVIMENTO.md#limitações-do-parser-de-currículo).

### Currículos de exemplo

A pasta `samples/` tem PDFs fictícios para testar a importação. O `curriculo-ficticio.pdf` é o caso comum. Os de `02` a `06` mostram as limitações do parser, e os de `07` a `09` são PDFs que a API não consegue ler.

| Arquivo | O que mostra |
| --- | --- |
| `curriculo-ficticio.pdf` | currículo comum: nome, e-mail e telefone encontrados |
| `02-duas-colunas.pdf` | duas colunas, em inglês |
| `03-caixa-alta.pdf` | nome em caixa alta (vira "Beltrano Sicrano de Tal") e telefone fixo |
| `04-rotulos.pdf` | nome com rótulo (`Nome:`): o nome não é encontrado |
| `05-cargo-no-topo.pdf` | cargo acima do nome: o cargo é tomado como nome |
| `06-sem-contato.pdf` | só o nome; não há e-mail nem telefone |
| `07-sem-texto.pdf` | página sem texto (como um PDF escaneado): `422`, preencha à mão |
| `08-corrompido.pdf` | cabeçalho de PDF, mas o arquivo está quebrado: `422` |
| `09-com-senha.pdf` | protegido por senha (`senha-de-teste`): `422` |
