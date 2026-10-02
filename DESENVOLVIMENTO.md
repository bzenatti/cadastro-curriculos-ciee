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
- **Uso de TDD:** na etapa de realizar o parser do currículo, pedi à IA gerar o código dos testes com os cenários possíveis de texto, considerando o texto que poderia aparecer em um currículo.
- **Geração de Currículos Falsos:** os currículos falsos foram gerados com o uso de IA para teste e validação.

### Exemplos de prompts

- "Que que isso faz?" (colando o comando `docker compose exec db ... sqlcmd ...`)
- "Gere testes para o componente Button"
- "Quero fazer a primeira etapa do Parser do resumo com TDD, entao quero q vc faça todos os testes, com os cenarios possiveis, pra eu rodar e eles falharem."

## Limitações do parser de currículo

O `ResumeParser` é uma heurística sobre o texto extraído do PDF: ele sugere nome, e-mail e telefone, e a pessoa revisa tudo antes de salvar. Nada é gravado na extração, e o cadastro valida os campos de novo. O que ele não resolve:

- **Nome:** vale a primeira linha, entre as 10 primeiras, com 2 a 6 palavras iniciadas em maiúscula. Um cargo antes do nome é tomado como nome (`samples/05-cargo-no-topo.pdf`). Linha com rótulo (`Nome: Fulana`) ou com nome e cargo juntos não é reconhecida e o campo fica vazio (`samples/04-rotulos.pdf`). A lista de títulos de seção que ele descarta é em português (mais "Curriculum Vitae"). Abreviações com ponto (`J. Silva`) são recusadas. Só palavras em CAIXA ALTA são convertidas para "Maria da Silva"; as demais mantêm a caixa original.
- **E-mail:** vale o primeiro do texto. Um trecho que casa com o regex mas passa de 254 caracteres não é e-mail (é o limite do cadastro): ele é descartado e o parser tenta a próxima ocorrência. O regex é só uma busca e não valida o domínio (aceita `a..b.com`); um e-mail colado na palavra seguinte pelo PDF (`x@y.comTelefone`) sai errado.
- **Telefone:** só formato brasileiro (DDD + 8 ou 9 dígitos, com ou sem +55); número sem DDD ou estrangeiro não é reconhecido, e vale o primeiro do texto. A linha que cita "CPF" é ignorada inteira, então um telefone na mesma linha se perde; já um CPF só com dígitos e sem a palavra "CPF" pode ser tomado por celular.
- **Ordem do texto:** o parser depende da ordem em que o PDF entrega as linhas; em layouts de duas colunas ela pode vir misturada (`samples/02-duas-colunas.pdf`).
