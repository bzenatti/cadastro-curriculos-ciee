# Desenvolvimento

## Versionamento

Como trabalhei sozinho, usei apenas a branch `main`, com commits pequenos (uma mudança lógica cada) e mensagens no padrão Conventional Commits (`feat:`, `fix:`, `refactor:`...), para o histórico contar a evolução do projeto.

Em equipe eu usaria: `main` protegida, branches curtas por funcionalidade (`feat/importar-pdf`, `fix/validacao-email`) e Pull Request com revisão e CI (build + testes) verde antes do merge. Dividiria o trabalho por funcionalidade, não por camada: frontend e backend já ficam em pastas separadas do monorepo, e o que permite trabalharem em paralelo é combinar antes o contrato da API.

## Uso de IA

Usei o **Claude Code** (modelos Claude Opus 5.5 e Sonnet 5.5) como apoio ao longo do desafio.

Até aqui, a IA me ajudou em:

- **Setup dos arquivos e projetos base:** pedi os comandos para criar a API ASP.NET Core, o projeto de testes, o frontend (Vite + React + TypeScript) e o `docker-compose.yml` com o SQL Server. Executei os comandos por conta própria e revisei o resultado.
- **Básico de Docker:** como sou faz tempo que usei Docker da última vez, pedi explicações do que cada parte do `docker-compose.yml` faz (imagem, variáveis de ambiente, portas, volume, healthcheck) e do comando `sqlcmd` usado para testar a conexão com o banco.
- **Esqueleto do README:** pedi uma estrutura inicial para o `README.md`, que vou preencher e ajustar conforme o projeto avançar.
- **Geração de testes:** pedi à IA testes de unidade para cada componente; revisei e removi os exagerados (ex.: testes de classes CSS e de detalhes de implementação), mantendo só os que validam comportamento.
- **Geração de código:** pedi o código em etapas pequenas. No componente de upload de currículo (arrastar ou clicar), pedi primeiro a lógica comum aos dois modos (ex.: validação do PDF) e depois cada interação separada, revisando cada parte antes de seguir.
- **Estilização do Frontend:** a IA gerou o código do css, tive que pedir pra alterar algumas coisas, tornar alguns valores globais para não serem inconsistentes nos componentes
- **Tratamento de erros:** a IA sugeriu tratar o e-mail duplicado no `CandidateService`, com a exceção capturada no controller. Decidi fazer um middleware global de exceções, que converte o e-mail duplicado em 409, devolve 500 sem stack trace e registra no log os erros inesperados.


### Exemplos de prompts

- "Que que isso faz?" (colando o comando `docker compose exec db ... sqlcmd ...`)
- "Gere testes para o componente Button"
