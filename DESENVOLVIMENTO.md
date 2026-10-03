# Desenvolvimento

Como fiz o desafio: organização, decisões, uso de IA, verificação e limites.

## Como organizei o trabalho

1. **Planejamento antes do código.** Listei os requisitos e escrevi o contrato da API: rotas, formatos, erros, validações e o que o front faz com cada resposta. Com o contrato fixo, backend e frontend andaram separados.
2. **Backend em fatias completas:** banco, cadastro com validação, listagem e detalhe, tratamento de erros, Docker, parser do currículo, leitura do PDF e endpoint de importação.
3. **Frontend:** componentes pequenos e testados, formulário, listagem, detalhes e importação.
4. **Testes de integração** com SQL Server de verdade e, por último, a documentação.

Cada etapa já saiu na forma final, sem protótipo para refatorar depois.

**Versionamento.** Como trabalhei sozinho, usei só a `main`, com commits pequenos (uma mudança lógica cada) no padrão Conventional Commits. Em equipe, usaria `main` protegida, branches curtas por funcionalidade e Pull Request com revisão e CI verde. Dividiria o trabalho por funcionalidade, não por camada, combinando o contrato da API antes.

## Principais decisões técnicas

- **Stack simples:** Organizei o backend em um único projeto de API, dividido em pastas, e um projeto de testes. Descartei dividir em vários projetos (Clean Architecture), MediatR e AutoMapper: para 5 campos e 4 endpoints, o custo de interfaces e mapeamentos seria maior que o ganho.
- **Docker Compose** com banco, API e front: `docker compose up -d --build` sobe tudo, sem instalar .NET nem Node. O SQL Server usa a porta 1434, para não colidir com uma instância local.
- **Banco só por migrations do EF Core,** aplicadas ao subir a API quando `Database:MigrateOnStartup` é `true`. É um artefato só, sem `.sql` para manter. Limite: quem só tem um cliente SQL gera o script com `dotnet ef` (o README explica).
- **Extrair não é cadastrar.** `POST /api/resumes/parse` só devolve nome, e-mail e telefone; só `POST /api/candidates` grava. Assim o PDF é opcional, uma falha na leitura não bloqueia o cadastro manual, e os dois caminhos usam o mesmo formulário e a mesma validação.
- **O PDF só preenche campos vazios,** para a heurística não apagar o que a pessoa digitou. Limite: um segundo PDF não troca o que o primeiro preencheu.
- **PDF para texto (PdfPig) separado de texto para campos (`ResumeParser`).** O parser é uma função pura, testada com strings (CPF, CEP e datas não podem virar telefone). Usei PdfPig e não iText, por causa da licença AGPL. Sem LLM: exigiria chave de API, enviaria dado pessoal a terceiros e deixaria os testes não determinísticos.
- **O PDF não é guardado e o texto não vai para o log** (LGPD).
- **A mesma validação nos dois lados, com o backend como fonte da verdade.** O limite de cada campo é igual no DTO, na validação e na coluna; sem isso um resumo grande vira erro 500. Descartei o `[EmailAddress]` do .NET, que aceita quase tudo com `@`.
- **E-mail único por índice no banco, com resposta 409.** Um `if` antes do insert deixa passar duas requisições simultâneas; um teste cobre 10 cadastros ao mesmo tempo.
- **Erros em `ProblemDetails`, por um tratador global:** mensagem em português, `traceId` e nunca stack trace. Erro inesperado vira 500 genérico e vai para o log. PDF inválido ou grande demais é 400; PDF ilegível (senha, corrompido, escaneado) é 422.
- **Upload em camadas.** O front valida tipo e tamanho; o back confere o tamanho e a assinatura `%PDF-` (extensão e `Content-Type` quem escolhe é o cliente) e lê só as 5 primeiras páginas. Acima de 6 MB o servidor recusa o envio antes de ler.
- **Sem CORS:** o front chama caminhos relativos (`/api/...`) e o Vite (desenvolvimento) ou o nginx (Docker) repassa para a API. Para o navegador há uma origem só.
- **Texto do usuário nunca vira HTML:** o React escapa, e as quebras de linha do resumo usam `white-space: pre-wrap`. A connection string fica fora do repositório (user-secrets e variáveis de ambiente), com um `.env.example`.
- **Paginação no servidor,** com tamanho máximo e ordem fixa; a lista devolve só o que a tela mostra.
- **O estado do formulário fica na página,** porque a importação precisa ler o que já foi digitado. Se a importação falha, o formulário continua como está.
- **Testes do que quebraria se um requisito parasse:** parser com casos negativos, validação, integração com SQL Server de verdade (Testcontainers) e front com Vitest. Descartei o EF InMemory, que não aplica índice único nem tamanho de coluna: o teste de e-mail duplicado passaria com o código errado. Não há testes de autenticação nem de limite de requisições, porque a API não tem nenhum dos dois.

## Como usei a IA

### Abordagem

Usei a IA como assistente, não como agente. Ela explicava, sugeria e escrevia trechos; eu decidia, colocava no projeto, rodava e revisava. O objetivo era aprender (Entity Framework Core e Testcontainers eram novos para mim, e eu nunca tinha configurado o Docker do zero) e entregar um software em que eu soubesse explicar cada decisão.

### Regras que segui

1. **Só entra o que eu consigo explicar.** Cada trecho vinha com o que faz, por que existe e o ponto de atenção.
2. **Eu coloco o código no projeto,** em passos pequenos (até 100 linhas), revisando antes de seguir.
3. **Planejar antes,** seguindo o Pragmatic Programmer: contrato da API e riscos de segurança primeiro, depois fatias de ponta a ponta, cada uma na versão final.
4. **Testes antes do código** quando o comportamento está claro, como no parser (TDD).
5. **Questionar e verificar.** Eu perguntava o porquê de cada escolha e se havia outro jeito, e conferia pacotes e versões na fonte.
6. **Registrar cada decisão** com o motivo, a alternativa descartada e o limite.

### Ferramentas e modelos

Claude Code no VS Code, com Claude Opus 5.5 e Sonnet 5.5. Nos testes do backend, o Claude Haiku 4.5 rodou `dotnet test` e devolveu só o resultado.

### Em que a IA ajudou (exemplos)

| Etapa | O que pedi | Como usei |
| --- | --- | --- |
| Docker e base do projeto | Explicações do que cada parte do `docker-compose.yml` e do comando `sqlcmd` faz | Rodei os comandos e li cada parte até entender. |
| Decisões | O porquê de algumas escolhas e se havia outro jeito | Comparei as alternativas e registrei o motivo, o que descartei e o limite. |
| Parser | Os testes antes do código, com os cenários possíveis, para ver falhar primeiro | Mais de 80 casos escritos antes do parser; implementei até ficarem verdes. |
| Frontend | CSS e testes de componente | Tornei globais os valores repetidos e removi testes de classe CSS. |
| Testes do backend | Cenários pensando como QA, com foco em segurança | Escolhi o que entrava (ambiente `Testing`, cadastros simultâneos, nada de autenticação). |
| Currículos de exemplo | PDFs fictícios com armadilhas | Viraram entrada dos testes e documentam as limitações do parser. |
| Dados de teste | Um script para popular o banco com candidatos fictícios | Usei para fechar os testes manuais de listagem, paginação e detalhes. |

Nos testes do backend a IA foi além do trecho: ajudou a elaborar os cenários e escreveu a implementação. Eu defini o que entrava e revisei.

### O que corrigi ou descartei

- **E-mail duplicado:** a IA sugeriu tratar no controller; fiz um tratador global.
- **Pacote do PdfPig:** a IA indicou um ID antigo no NuGet. O `dotnet add` não achou versão estável, conferi na fonte e usei o ID oficial, `PdfPig`.
- **Limite de envio:** a IA recomendou só o validador. Mantive também o corte antes da leitura e registrei o custo: mais peças no código.
- **Testes de componente:** removi os que olhavam classes CSS e detalhes de implementação.

## Como verifiquei

- **`dotnet test`:** 224 testes, 161 unitários e 63 de integração (sobem um SQL Server em container e precisam do Docker). O front tem testes com Vitest.
- **À mão,** com `curl` e o arquivo `.http`: os PDFs de `samples/` (inclusive sem texto, corrompido e com senha) e os erros (sem arquivo, arquivo de texto, PDF acima de 5 MB).
- **Dados de exemplo:** `scripts/seed-candidates.sh` cria candidatos fictícios pela API (nomes de personagens de Game of Thrones e Harry Potter), com combinações de campos, tamanhos máximos, acentos, emoji, símbolos e mais de 50 registros para a paginação. O nome termina com as iniciais dos campos preenchidos (N nome, E e-mail, T telefone, C cargo, R resumo): `NER` tem nome, e-mail e resumo; `NETCR` tem todos. Ao abrir a listagem e os detalhes, sei o que esperar de cada um. Pedi à IA para gerar o script e o usei para fechar os testes manuais.
- **Docker:** `docker compose down -v` seguido de `up`; a API criou o banco e a tabela sozinha.
- **Teste de mutação:** quebrei oito regras do código de propósito (como o `Trim`, a assinatura `%PDF-` e o limite de 5 MB) e confirmei que os testes certos falhavam. Depois restaurei.

## Tempo dedicado

Cerca de 15-20 horas, sem contar o tempo de estudo do que eu nunca tinha usado (Entity Framework Core e Testcontainers) e da configuração do Docker do zero, e a revisão de React.

## Dificuldades

- **Entity Framework Core, Testcontainers e o Docker configurado do zero eram novos:** estudei cada parte enquanto implementava.
- **Envio de arquivo grande:** com `IFormFile` como parâmetro, o MVC lê o formulário antes da action e devolve o próprio 400, em inglês. Passei a ler o formulário na action e a traduzir as exceções no tratador global.
- **PDF com senha para teste:** sem `qpdf` na máquina, gerei o arquivo com `pdftk`.

## Limitações do parser de currículo

O `ResumeParser` é uma heurística sobre o texto do PDF: sugere nome, e-mail e telefone, e a pessoa revisa tudo antes de salvar.

- **Nome:** a primeira linha, entre as 10 primeiras, com 2 a 6 palavras iniciadas em maiúscula. Um cargo antes do nome é tomado como nome (`samples/05-cargo-no-topo.pdf`). Rótulos como `Nome: Fulana` (`samples/04-rotulos.pdf`) e abreviações com ponto (`J. Silva`) não são reconhecidos. Só nomes em CAIXA ALTA são convertidos para "Maria da Silva". A lista de títulos de seção que ele ignora é em português.
- **E-mail:** vale o primeiro do texto; um trecho acima de 254 caracteres é descartado. O regex não valida o domínio (aceita `a..b.com`), e um e-mail colado na palavra seguinte pelo PDF (`x@y.comTelefone`) sai errado.
- **Telefone:** só formato brasileiro (DDD + 8 ou 9 dígitos, com ou sem +55), o primeiro do texto. A linha que cita "CPF" é ignorada inteira, e um CPF só com dígitos e sem a palavra pode ser tomado por celular.
- **Ordem do texto:** em PDFs de duas colunas ela pode vir misturada (`samples/02-duas-colunas.pdf`).

## Melhorias futuras

A API é aberta de propósito: o projeto roda só em desenvolvimento e não tem usuários. Se for acoplado a um sistema real:

- **Usuários e perfis:** listagem e detalhe só para administradores ou recrutadores autenticados; cadastro e importação abertos a quem se candidata. Hoje qualquer pessoa que alcance a API lê os dados de todos, e os ids sequenciais permitem percorrer um a um (dados pessoais, LGPD).
- **Limite de requisições** nos endpoints abertos, principalmente `POST /api/resumes/parse`, que lê o PDF: `AddRateLimiter` do ASP.NET Core e CAPTCHA se o cadastro continuar público. Evita cadastros em massa e negação de serviço.
- **Dados pessoais:** id não sequencial, consentimento, prazo de retenção, exclusão a pedido e registro de acessos.
- **Infraestrutura:** HTTPS e limite de tempo e memória no container para o PDF (uma bomba de descompressão pode consumir memória mesmo lendo só 5 páginas).
- **Importação:** OCR para PDF escaneado e reconhecimento de rótulos e de duas colunas. Um LLM seria uma opção, ao custo de enviar dado pessoal a terceiros.
- **Edição e exclusão:** hoje só se cadastra e consulta. Antes de existirem, é preciso ter autenticação (a API é aberta, e qualquer pessoa apagaria os dados de todos). A edição também precisa tratar o e-mail único (409) quando o e-mail muda.
- **Busca e filtros** (nome, e-mail, cargo): entram na listagem paginada, com o filtro aplicado antes da contagem e da página. Um `LIKE '%texto%'` não usa índice, então com muitos registros pediria um índice próprio ou a busca de texto completo do SQL Server.
- **Processo:** CI (build e testes a cada push) e um teste de ponta a ponta no navegador.

Quando autenticação e limite de requisições existirem, os primeiros testes seriam: sem login, a listagem responde 401; quem não é recrutador recebe 403 no detalhe; a importação acima do limite responde 429.
