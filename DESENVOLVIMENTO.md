# Desenvolvimento

## Uso de IA

Usei o **Claude Code** (modelos Claude Opus 5.5 e Sonnet 5.5) como apoio ao longo do desafio.

Até aqui, a IA me ajudou em:

- **Setup dos arquivos e projetos base:** pedi os comandos para criar a API ASP.NET Core, o projeto de testes, o frontend (Vite + React + TypeScript) e o `docker-compose.yml` com o SQL Server. Executei os comandos por conta própria e revisei o resultado.
- **Básico de Docker:** como sou faz tempo que usei Docker da última vez, pedi explicações do que cada parte do `docker-compose.yml` faz (imagem, variáveis de ambiente, portas, volume, healthcheck) e do comando `sqlcmd` usado para testar a conexão com o banco.
- **Esqueleto do README:** pedi uma estrutura inicial para o `README.md`, que vou preencher e ajustar conforme o projeto avançar.

### Exemplos de prompts

- "Que que isso faz?" (colando o comando `docker compose exec db ... sqlcmd ...`)
