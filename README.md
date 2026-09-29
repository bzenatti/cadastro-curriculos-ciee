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
