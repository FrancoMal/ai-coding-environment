# AI Coding Environment

Un template completo para crear ambientes de desarrollo con Docker + herramientas AI. Un solo comando instala todo y levanta el proyecto.

## Que incluye

- **Dashboard web** con login y gestion de usuarios (Blazor WebAssembly)
- **API REST** con autenticacion JWT (.NET 8)
- **Base de datos** SQL Server Express
- **Herramientas AI**: Claude Code, OpenCode, Codex CLI, Gemini CLI
- **Nginx** como reverse proxy
- **100% Docker** - un solo comando para levantar todo

## Stack

| Componente | Tecnologia |
|------------|------------|
| Backend | .NET 8 + C# + Entity Framework Core |
| Base de datos | SQL Server 2022 Express |
| Frontend | Blazor WebAssembly (.NET 8) + CSS |
| Servidor web | Nginx |
| Agentes AI | Claude Code, OpenCode, Codex CLI, Gemini CLI |
| Contenedores | Docker + Docker Compose |

## Instalacion rapida

### Requisito previo

- **Linux (Ubuntu/Debian)**: El script instala todo automaticamente
- **macOS**: Necesitas [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado
- **Windows**: Usa WSL2 y seguí las instrucciones de Linux

### Instalar todo

```bash
git clone https://github.com/FrancoMal/ai-coding-environment.git
cd ai-coding-environment
chmod +x setup.sh
./setup.sh
```

El script `setup.sh` instala automaticamente:
1. Herramientas base (git, curl, etc.)
2. Node.js 20
3. Python 3
4. Docker + Docker Compose
5. Herramientas AI (Claude Code, OpenCode, Codex CLI, Gemini CLI)
6. Levanta el proyecto (DB + API + Frontend)

### Acceder

| Que | Direccion |
|-----|-----------|
| Dashboard | http://localhost:3000 |

**Login:** admin / admin123

## Instalacion manual

Si preferis instalar paso a paso:

```bash
# 1. Configurar variables de entorno
cp .env.example .env

# 2. (Opcional) Editar .env con tus API keys

# 3. Levantar la app
docker compose up --build -d

# 4. Abrir http://localhost:3000
```

## Herramientas AI

Las herramientas se instalan en tu maquina con el script `setup.sh`. Despues las podes usar directo desde la terminal:

| Herramienta | Comando | API Key necesaria |
|-------------|---------|-------------------|
| Claude Code | `claude` | ANTHROPIC_API_KEY |
| OpenCode | `opencode` | OPENAI_API_KEY |
| Codex CLI | `codex` | OPENAI_API_KEY |
| Gemini CLI | `gemini` | GEMINI_API_KEY |

Configura las API keys en el archivo `.env`.

## Estructura del proyecto

```
ai-coding-environment/
├── docker-compose.yml          # Levanta la app (DB + API + Frontend)
├── setup.sh                    # Instalador automatico
├── .env.example                # Variables de entorno (API keys)
├── src/Api/                    # Backend .NET 8
│   ├── Controllers/            # Endpoints de la API
│   ├── Models/                 # Modelos de datos
│   ├── Services/               # Logica de negocio
│   ├── Data/                   # Entity Framework (base de datos)
│   └── Dockerfile              # Build del backend
├── src/Web/                    # Frontend Blazor WebAssembly
│   ├── Pages/                  # Paginas (Login, Dashboard, Config)
│   ├── Layout/                 # Layouts (MainLayout, LoginLayout)
│   ├── Shared/                 # Componentes reutilizables
│   ├── Services/               # Servicios (Auth, API, Toast)
│   ├── wwwroot/                # Archivos estaticos (HTML, CSS)
│   └── Dockerfile              # Build del frontend
├── db/init.sql                 # Creacion de tablas
└── nginx/nginx.conf            # Reverse proxy + SPA routing
```

## Servicios Docker

| Servicio | Que hace | Puerto |
|----------|----------|--------|
| sqlserver | Base de datos SQL Server Express | 1433 (interno) |
| api | Backend .NET 8 con autenticacion JWT | 80 (interno) |
| web | Blazor WASM + Nginx | 3000 |

## Credenciales por defecto

| Servicio | Usuario | Contrasena |
|----------|---------|------------|
| Dashboard | admin | admin123 |
| SQL Server | sa | YourStrong@Passw0rd |

**Importante:** Cambiar las contrasenas en produccion.

## Comandos utiles

```bash
# Levantar todo
docker compose up --build -d

# Ver logs
docker compose logs -f

# Parar todo
docker compose down

# Reinstalar herramientas AI
./setup.sh
```

## Para agentes de IA

Este proyecto incluye `CLAUDE.md` con instrucciones detalladas para que cualquier agente de IA pueda trabajar en el automaticamente.

## Expansion

- **Nueva pagina:** crear `.razor` en `src/Web/Pages/` y agregar navegacion en `MainLayout.razor`
- **Nueva tabla:** crear modelo en `src/Api/Models/` + agregar en `db/init.sql`
- **Nuevo endpoint:** crear controller en `src/Api/Controllers/`
- **Nuevo servicio Docker:** agregar en `docker-compose.yml`

Ver `CLAUDE.md` para instrucciones detalladas.

## Licencia

MIT
