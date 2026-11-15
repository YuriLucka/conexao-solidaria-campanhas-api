# Conexão Solidária - Campanhas API

Microsserviço de usuários, campanhas e doações da plataforma Conexão Solidária (Hackathon FIAP Pós Tech 11NETT, Grupo 31).
Instruções para subir a solução inteira: repositório [conexao-solidaria-orchestration](https://github.com/YuriLucka/conexao-solidaria-orchestration).

## O que faz

| Método | Rota | Acesso |
|---|---|---|
| POST | `/api/auth/login` | público (JWT de 15 min no corpo; refresh token em cookie HttpOnly) |
| POST | `/api/auth/refresh` | cookie do refresh token (troca o token a cada uso) |
| POST | `/api/auth/logout` | cookie do refresh token (revoga a sessão) |
| POST | `/api/doadores` | público |
| GET | `/api/campanhas/ativas` | público (painel de transparência) |
| GET | `/api/campanhas/{id}` | público |
| GET | `/api/campanhas/{id}/doacoes-recentes` | público |
| GET | `/api/campanhas` | GestorONG |
| POST | `/api/campanhas` | GestorONG |
| PUT | `/api/campanhas/{id}` | GestorONG |
| POST | `/api/doacoes` | Doador |
| GET | `/api/doacoes/minhas` | Doador (paginado, com resumo) |
| GET | `/api/doacoes/{id}` | Doador dono da doação |
| GET | `/api/doacoes` | GestorONG (paginado, filtros, totais) |
| GET | `/api/gestao/resumo` | GestorONG |
| GET | `/api/gestao/doadores` | GestorONG (paginado, busca, indicadores) |
| GET | `/health`, `/health/live`, `/metrics` | infraestrutura |

Documentação interativa (Scalar) em `/scalar`; documento OpenAPI em `/openapi/v1.json`.

**Doação assíncrona.** `POST /api/doacoes` grava a doação como `Pendente` e publica `DoacaoRecebidaEvent` pelo outbox transacional do MassTransit (doação e evento na mesma transação do PostgreSQL). Quem soma o valor na campanha é o [doacoes-worker](https://github.com/YuriLucka/conexao-solidaria-doacoes-worker). O mapeamento do EF Core ignora `valor_arrecadado` ao salvar: a API não consegue alterar esse valor.

## Arquitetura

```
src/Campanhas.Domain          entidades e regras (Campanha, Usuario, Doacao, Cpf, Email)
src/Campanhas.Application     casos de uso e DTOs
src/Campanhas.Infrastructure  EF Core + PostgreSQL, outbox MassTransit, Redis, JWT, BCrypt, seed
src/Campanhas.API             controllers, Scalar, autenticação, health, métricas
building-blocks/              evento DoacaoRecebidaEvent e configuração do MassTransit (iguais no worker)
tests/Campanhas.Tests         xUnit: domínio, aplicação e infraestrutura
```

## Rodar só este serviço

Pré-requisitos: .NET SDK 10 e a infraestrutura do Compose (`docker compose up -d postgres rabbitmq rabbitmq-init redis` no repositório de orquestração).

```bash
dotnet run --project src/Campanhas.API --launch-profile http   # http://localhost:5080/scalar
dotnet test Campanhas.slnx
```

Em `Development` o seed de demonstração liga sozinho: gestora `helena@esperancasolidaria.demo` / `Gestao@2026` e doadora `mariana@email.demo` / `Doar@2026`.

## Configuração

| Chave | Exemplo |
|---|---|
| `ConnectionStrings__Postgres` | `Host=postgres;Database=conexao_solidaria;Username=...;Password=...` |
| `ConnectionStrings__Redis` | `redis:6379` |
| `RabbitMq__Host`, `RabbitMq__User`, `RabbitMq__Pass` | `rabbitmq`, ... |
| `JwtSettings__SecretKey` | mínimo 32 caracteres (Secret) |
| `Seed__Enabled` | `true` para criar os dados de demonstração |

## CI

`.github/workflows/ci.yml`, a cada push na `main`: restore, build, testes e imagem Docker publicada em `ghcr.io/yurilucka/conexao-solidaria-campanhas-api` (tags `sha-<commit>` e `latest`).
