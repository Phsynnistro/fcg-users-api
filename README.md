# FCG — Users API

Microsserviço de **identidade** da FIAP Cloud Games: cadastro de usuários, autenticação com emissão de **JWT RS256** e autorização por papel (`User`/`Admin`). É o único serviço que possui a chave privada de assinatura; publica a chave pública via **JWKS** para os demais validarem os tokens.

> Parte da arquitetura de microsserviços da FIAP Cloud Games. Visão geral, diagramas, decisões (ADRs) e matriz de
> requisitos no repositório [fcg-orchestration](https://github.com/Phsynnistro/fcg-orchestration).

**Stack:** .NET 10 · ASP.NET Core Minimal APIs · EF Core 10 + PostgreSQL 17 · MassTransit 8.5 + RabbitMQ (Outbox/Inbox) · OpenTelemetry · xUnit

## Responsabilidades

- Cadastrar usuários validando e-mail e a política de senha da FCG (mín. 8 caracteres, letras, números e caractere especial).
- Autenticar e emitir access token (RS256, claims `sub`, `email`, `name`, `role`).
- Publicar `UserCreatedEvent` de forma atômica com o cadastro (Transactional Outbox).
- Expor `/.well-known/openid-configuration` e `/.well-known/jwks.json`.
- Criar o administrador inicial a partir de credenciais em Secret (seed idempotente).

## Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `POST` | `/api/users` | Anônimo · rate limit | Cadastra usuário → 201 + publica `UserCreatedEvent` |
| `POST` | `/api/auth/login` | Anônimo · rate limit | Autentica → `{ accessToken, expiresIn, ... }` |
| `GET` | `/api/users/me` | Autenticado | Dados do usuário do token |
| `GET` | `/api/users/{id}` | Admin | Consulta usuário |
| `GET` | `/api/users?page=&pageSize=` | Admin | Lista paginada |
| `GET` | `/.well-known/openid-configuration` | Anônimo | Discovery (issuer + jwks_uri) |
| `GET` | `/.well-known/jwks.json` | Anônimo | Chave pública de validação |

Todos os serviços expõem `/health/live` (liveness), `/health/ready` (readiness) e `/health` (diagnóstico em JSON).
Erros seguem **Problem Details (RFC 9457)** com `traceId`.

Documentação interativa: http://localhost:5101/scalar (com `dotnet run`) ou http://localhost:5001/scalar (compose).

## Eventos

| Direção | Evento | Exchange | Fila deste serviço | Campos principais |
|---|---|---|---|---|
| Publica | `UserCreatedEvent` | `fcg.user-created` | — | `UserId`, `Name`, `Email`, `CreatedAt` |

Contratos em `src/Fcg.Users.Api/Contracts/Events`, namespace **`Fcg.Contracts.Events`** — idêntico em todos os
repositórios (o MassTransit identifica a mensagem por namespace + nome). Altere-os apenas de forma **aditiva**;
o `ContractTests` falha se a identidade do contrato mudar.

## Variáveis de ambiente

Configuração .NET via variáveis de ambiente: `__` (dois underscores) equivale a `:` — `RabbitMq__Host` → `RabbitMq:Host`.

| Variável | Exemplo | Origem no K8s | Descrição |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | ConfigMap | Ambiente do ASP.NET Core. |
| `RabbitMq__Host` | `rabbitmq` | ConfigMap | Host do broker (nome do Service no cluster). |
| `RabbitMq__Port` | `5672` | ConfigMap | Porta AMQP. |
| `RabbitMq__VirtualHost` | `/` | ConfigMap | Virtual host do RabbitMQ. |
| `RabbitMq__Username` | `fcg` | **Secret** | Usuário do broker. |
| `RabbitMq__Password` | `••••` | **Secret** | Senha do broker. |
| `ConnectionStrings__Database` | `Host=users-db;Port=5432;Database=fcg_users;Username=fcg_users;Password=••••` | **Secret** | Connection string do PostgreSQL exclusivo do serviço. |
| `ApiDocs__Enabled` | `true` | ConfigMap | Expõe `/openapi/v1.json` e a UI Scalar em `/scalar`. |
| `Jwt__Issuer` | `fcg-users-api` | ConfigMap | Valor da claim `iss`. |
| `Jwt__Audience` | `fcg` | ConfigMap | Valor da claim `aud`. |
| `Jwt__AccessTokenLifetimeMinutes` | `60` | ConfigMap | Validade do token (1–1440). |
| `Jwt__PrivateKey` | `MIIEvQIBADAN…` | **Secret** | Chave privada RSA, PKCS#8 DER em Base64 (uma linha). Gerar com `fcg-orchestration/scripts/generate-jwt-keys.sh`. |
| `Seed__AdminName` | `Administrador FCG` | ConfigMap | Nome do administrador inicial. |
| `Seed__AdminEmail` | `admin@fcg.local` | ConfigMap | E-mail do administrador inicial. |
| `Seed__AdminPassword` | `••••` | **Secret** | Senha do administrador inicial. |
| `RateLimiting__Authentication__PermitLimit` | `20` | ConfigMap | Tentativas de login/cadastro por IP por janela. |
| `RateLimiting__Authentication__WindowSeconds` | `60` | ConfigMap | Tamanho da janela (s). |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://jaeger:4317` | ConfigMap | Coletor OTLP (Jaeger). Ausente ⇒ tracing não é exportado. |

Valores sensíveis (**Secret**) nunca entram no `appsettings.json`. O `appsettings.Development.json` contém valores
**apenas de desenvolvimento local**.

## Executar localmente

```bash
# 1) infraestrutura (a partir do repositório de orquestração)
cd ../fcg-orchestration && docker compose up -d rabbitmq jaeger users-db

# 2) o serviço
cd ../fcg-users-api
dotnet run --project src/Fcg.Users.Api        # http://localhost:5101
```

## Testes

```bash
dotnet test
```

Testes de domínio (regras e invariantes) e de **contrato** (identidade dos eventos).

## Docker

```bash
docker build -t fcg/users-api:1.0.0 .            # imagem final: runtime chiseled, não-root, porta 8080
docker build --target test .                    # (opcional) roda os testes dentro do build
```

Dockerfile **multi-stage**: `build` (SDK) → `test` (opcional) → `final` (`aspnet:10.0-noble-chiseled`).
O CI analisa a imagem com **Trivy** e falha em vulnerabilidade CRITICAL/HIGH corrigível.

## Kubernetes

| Arquivo em [`k8s/`](k8s) | Conteúdo |
|---|---|
| `00-namespace.yaml` | Namespace `fcg` (permite aplicar este repositório isoladamente) |
| `01-configmap.yaml` | ConfigMap: filas, URLs de serviços, limites |
| `02-secrets.yaml` | Secrets: credenciais, connection string e senha do banco |
| `03-database.yaml` | PostgreSQL: PersistentVolumeClaim + Deployment (`Recreate`) + Service |
| `04-deployment.yaml` | Deployment da API: probes, recursos, securityContext, anotações |
| `05-service.yaml` | Service ClusterIP `users-api:80` |
| `06-hpa.yaml` | HorizontalPodAutoscaler: 2–5 réplicas por CPU |

```bash
# pré-requisito: RabbitMQ e Jaeger do repositório fcg-orchestration (ou o ambiente completo dele)
cd k8s && kubectl apply -f .
kubectl -n fcg get pods -l app.kubernetes.io/name=users-api
```

Este `/k8s` é a **fonte da verdade** do serviço. O repositório de orquestração mantém uma cópia sincronizada
(`scripts/k8s-sync-manifests.sh`) para subir o ambiente inteiro com um único `kubectl apply -f .` (ADR-0013).

## Estrutura

```
src/Fcg.Users.Api/
├── Contracts/Events/                UserCreatedEvent.cs
├── Domain/Users/                    User, EmailAddress, PasswordPolicy
├── Application/Users/               RegisterUserHandler, LoginHandler, UserQueries, DTOs
├── Application/Abstractions/        ITokenService (porta)
├── Infrastructure/Persistence/      UsersDbContext, mapeamentos, seed do admin, init do banco
├── Infrastructure/Security/         JwtSigningKeys, JwtTokenService (RS256)
├── Infrastructure/Observability/    OpenTelemetry
└── Presentation/                    endpoints, rate limiting, health, ProblemDetails, OpenAPI
tests/Fcg.Users.UnitTests/
k8s/                                 manifestos Kubernetes (00 a 06)
Dockerfile · Fcg.Users.sln · Directory.Build.props · Directory.Packages.props (versões centralizadas) · global.json
```

## Decisões de design

- **Outbox**: `RegisterUserHandler` publica o evento antes do `SaveChanges`; o MassTransit grava a mensagem na tabela `OutboxMessage` na mesma transação do `INSERT` (ADR-0003).
- **Unicidade de e-mail garantida pelo banco** (índice único), não só pelo `AnyAsync` — dois cadastros simultâneos não passam.
- **Login sem enumeração de usuários**: mensagem genérica e verificação de hash fictícia quando o e-mail não existe (equaliza o tempo de resposta).
- **Rate limiting** (janela fixa por IP) no login e no cadastro → 429 (Aula 07).
- **PBKDF2 do ASP.NET Core Identity** com rehash transparente quando os parâmetros evoluem.
- **RS256 + JWKS** em vez de HS256 com segredo compartilhado: quem valida não consegue emitir (ADR-0005).
- **Tracing** com OpenTelemetry (HTTP, banco, publicação no RabbitMQ) exportado ao Jaeger (ADR-0011).
