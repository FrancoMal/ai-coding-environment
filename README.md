# AI Coding Environment

Un template completo para crear ambientes de desarrollo con Docker. Incluye dashboard web, API, base de datos y un workspace con herramientas AI preconfiguradas.

## Que incluye

- **Dashboard web** con login y gestion de usuarios (Blazor WebAssembly)
- **API REST** con autenticacion JWT (.NET 8)
- **Base de datos** SQL Server Express
- **Workspace** con herramientas AI: Claude Code, OpenCode, Codex CLI, Gemini CLI
- **Nginx** como reverse proxy
- **100% Docker** - un solo comando para levantar todo

## Stack

| Componente | Tecnologia |
|------------|------------|
| Backend | .NET 8 + C# + Entity Framework Core |
| Base de datos | SQL Server 2022 Express |
| Frontend | Blazor WebAssembly (.NET 8) + CSS |
| Servidor web | Nginx |
| Workspace | Ubuntu 22.04 + Node.js 20 + Python 3 |
| Agentes AI | Claude Code, OpenCode, Codex CLI, Gemini CLI |
| Contenedores | Docker + Docker Compose |

## Instalacion

### Requisito previo

Tener [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado y corriendo.

### Paso 1 - Descargar el proyecto

```bash
git clone https://github.com/FrancoMal/ai-coding-environment.git
cd ai-coding-environment
```

### Paso 2 - Configurar variables de entorno

```bash
cp .env.example .env
```

Si tenes API keys de los agentes AI, edita el archivo `.env` y pegalas ahi. Si no, dejalo vacio y todo funciona igual (solo que no vas a poder usar los agentes AI).

### Paso 3 - Levantar todo

```bash
docker compose up --build -d
```

Esto arranca automaticamente:
- Base de datos SQL Server
- Backend API (.NET 8)
- Frontend Blazor + Nginx
- Workspace con herramientas AI (se instalan en segundo plano la primera vez)

### Paso 4 - Abrir en el navegador

| Que | Direccion |
|-----|-----------|
| Dashboard | http://localhost:3000 |

**Login:** admin / admin123

### Paso 5 (Opcional) - Usar el workspace

```bash
docker exec -it aicoding-workspace bash
```

Dentro del workspace tenes disponible:
- **Claude Code** - `claude` (necesita ANTHROPIC_API_KEY)
- **OpenCode** - `opencode` (necesita OPENAI_API_KEY)
- **Codex CLI** - `codex` (necesita OPENAI_API_KEY)
- **Gemini CLI** - `gemini` (necesita GEMINI_API_KEY)
- **Node.js**, **Python**, **Git**, **Docker CLI**

Las herramientas AI se instalan automaticamente la primera vez que arranca el container. Si necesitas reinstalarlas: `install-ai-tools`

## Estructura del proyecto

```
ai-coding-environment/
├── docker-compose.yml          # Levanta todo (DB, API, frontend, workspace)
├── .env.example                # Variables de entorno
├── install-ai-tools.sh         # Script de instalacion de herramientas AI
├── entrypoint.sh               # Script de inicio del workspace
├── Dockerfile.workspace        # Container de trabajo (Ubuntu + herramientas)
├── src/Api/                    # Backend .NET 8
│   ├── Controllers/            # Endpoints de la API
│   ├── Models/                 # Modelos de datos
│   ├── Services/               # Logica de negocio
│   ├── Data/                   # Entity Framework (base de datos)
│   ├── DTOs/                   # Objetos de transferencia
│   └── Dockerfile              # Build del backend
├── src/Web/                    # Frontend Blazor WebAssembly
│   ├── Pages/                  # Paginas (Login, Dashboard, Config)
│   ├── Layout/                 # Layouts (MainLayout, LoginLayout)
│   ├── Shared/                 # Componentes reutilizables
│   ├── Models/                 # Modelos del frontend
│   ├── Services/               # Servicios (Auth, API, Toast)
│   ├── wwwroot/                # Archivos estaticos (HTML, CSS)
│   └── Dockerfile              # Build del frontend
├── db/                         # Scripts de base de datos
│   └── init.sql                # Creacion de tablas
└── nginx/                      # Configuracion del servidor web
    └── nginx.conf              # Reverse proxy + SPA routing
```

## Servicios Docker

| Servicio | Que hace | Puerto |
|----------|----------|--------|
| sqlserver | Base de datos SQL Server Express | 1433 (interno) |
| api | Backend .NET 8 con autenticacion JWT | 80 (interno) |
| web | Blazor WASM + Nginx | 3000 |
| workspace | Ubuntu + herramientas AI | - |

## Como se conectan

```
Browser -> localhost:3000 -> Nginx
                              |-- /          -> Frontend (Blazor WASM)
                              |-- /api/      -> Backend (.NET 8)
                              '-- /swagger   -> Documentacion API

Workspace (docker exec -it aicoding-workspace bash):
  Claude Code, OpenCode, Codex CLI, Gemini CLI
  Node.js v20, Python 3, Git, Docker CLI
```

## Credenciales por defecto

| Servicio | Usuario | Contrasena |
|----------|---------|------------|
| Dashboard | admin | admin123 |
| SQL Server | sa | YourStrong@Passw0rd |

**Importante:** Cambiar las contrasenas en produccion.

## Para agentes de IA

Este proyecto incluye `CLAUDE.md` con instrucciones detalladas para que cualquier agente de IA (Claude Code, OpenCode, Codex, Gemini, etc.) pueda trabajar en el automaticamente.

## Expansion

El template esta disenado para crecer facilmente:

- **Nueva pagina:** crear `.razor` en `src/Web/Pages/` y agregar navegacion en `MainLayout.razor`
- **Nueva tabla:** crear modelo en `src/Api/Models/` + agregar en `db/init.sql`
- **Nuevo endpoint:** crear controller en `src/Api/Controllers/`
- **Nuevo servicio Docker:** agregar en `docker-compose.yml`

Ver `CLAUDE.md` para instrucciones detalladas de cada tipo de expansion.

## Licencia

MIT
