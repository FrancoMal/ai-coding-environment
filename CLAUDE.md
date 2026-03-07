# AGENTS.md - Reglas para Agentes de IA

Este archivo contiene las instrucciones que SIEMPRE debe seguir cualquier agente de IA (Claude Code, OpenCode, o cualquier otro) cuando trabaje en este proyecto.

Lee este archivo completo antes de hacer cualquier cosa.

---

## Quien es el usuario

El usuario NO es programador ni tiene conocimientos de IT. Habla en lenguaje cotidiano, no tecnico. Tu trabajo es:

1. Escuchar lo que dice, aunque sea vago o impreciso
2. Interpretar que es lo que realmente necesita
3. Transformar su pedido en tareas concretas y ejecutarlas
4. Explicarle lo que hiciste en palabras simples, sin jerga tecnica

Cuando el usuario diga algo como "quiero que se vea mas lindo" o "hace que funcione eso", no le pidas que sea mas especifico con terminos tecnicos. Vos tenes que deducir que quiere y proponer opciones claras.

### Ejemplos

- Usuario dice: "quiero guardar cosas" -> Vos entendes: necesita una tabla en la base de datos + formulario + listado
- Usuario dice: "que se pueda entrar con clave" -> Vos entendes: necesita autenticacion/login
- Usuario dice: "no me anda" -> Vos entendes: hay que revisar los logs, el estado de los containers, y debuggear

---

## Reglas obligatorias

### 1. Siempre hacer commits

Cada vez que termines un cambio funcional, hace un commit con un mensaje claro en espanol:

```bash
git add -A
git commit -m "Agregar formulario de contacto en el dashboard"
```

No acumules muchos cambios en un solo commit. Un commit por funcionalidad o arreglo. Esto permite deshacer cosas si algo sale mal.

### 2. Probar antes de decir que esta listo

No digas "listo, funciona" sin haber verificado. Siempre:
- Si tocaste el backend: verifica que compila (`dotnet build`)
- Si tocaste el frontend: recarga el browser y verifica visualmente
- Si tocaste Docker: hace `docker compose up --build -d` y verifica que los containers esten corriendo

### 3. No romper lo que ya funciona

Antes de modificar un archivo, leelo primero. Entende que hace antes de cambiarlo. Si tu cambio puede afectar otras partes, revisalas tambien.

### 4. Explicar lo que hiciste

Despues de cada tarea, explica brevemente al usuario:
- Que cambiaste (en palabras simples)
- Por que lo hiciste asi
- Como lo puede ver o probar

Ejemplo: "Agregue una seccion de contacto en la pagina principal. Ahora cuando entres al dashboard vas a ver un formulario donde podes escribir un mensaje. Los mensajes se guardan en la base de datos."

### 5. Usar subagentes para tareas grandes

Si el usuario pide algo complejo (mas de 3 archivos o mas de una funcionalidad), dividilo en partes y usa subagentes en paralelo:
- Un agente para el backend (API, base de datos)
- Un agente para el frontend (paginas, estilos)
- Un agente para infraestructura (Docker, nginx) si hace falta

Esto es mas rapido y reduce errores.

### 6. No inventar funcionalidades extra

Hace SOLO lo que el usuario pidio. No agregues cosas "por las dudas" o "porque seria buena idea". Si crees que algo seria util, proponelo al usuario primero.

---

## Arquitectura del proyecto

```
ai-coding-environment/
|
|-- docker-compose.yml          <- Levanta la app (DB + API + Frontend)
|-- setup.sh                    <- Instalador: prepara la maquina y levanta todo
|-- .env.example                <- Variables de entorno (API keys)
|
|-- src/Api/                  <- Backend (API REST)
|   |-- Program.cs            <- Punto de entrada de la API
|   |-- Controllers/          <- Endpoints (AuthController, DashboardController)
|   |-- Models/               <- Modelos de datos (User.cs)
|   |-- DTOs/                 <- Objetos de transferencia (AuthDtos.cs)
|   |-- Services/             <- Logica de negocio (AuthService.cs)
|   |-- Data/                 <- Base de datos (AppDbContext.cs)
|   |-- Dockerfile            <- Como se construye el container de la API
|   |-- appsettings.json      <- Configuracion (JWT, connection string)
|   '-- Api.csproj            <- Dependencias del proyecto
|
|-- src/Web/                  <- Frontend (Blazor WebAssembly)
|   |-- Program.cs            <- Punto de entrada, registro de servicios
|   |-- App.razor             <- Router y autenticacion
|   |-- _Imports.razor        <- Usings globales
|   |-- Web.csproj            <- Dependencias del proyecto Blazor
|   |-- Dockerfile            <- Build multi-stage (SDK + nginx)
|   |-- Pages/                <- Paginas de la app
|   |   |-- Login.razor       <- Pagina de login
|   |   |-- Dashboard.razor   <- Pagina principal del dashboard
|   |   '-- Config.razor      <- Pagina de configuracion
|   |-- Layout/               <- Layouts de la app
|   |   |-- MainLayout.razor  <- Layout principal (sidebar + topbar)
|   |   '-- LoginLayout.razor <- Layout de login
|   |-- Shared/               <- Componentes reutilizables
|   |   |-- NavItem.razor     <- Item de navegacion del sidebar
|   |   |-- StatCard.razor    <- Tarjeta de estadistica
|   |   |-- ToastContainer.razor <- Notificaciones toast
|   |   |-- SvgIcons.razor    <- Iconos SVG centralizados
|   |   '-- RedirectToLogin.razor <- Redireccion a login
|   |-- Models/               <- Modelos de datos del frontend
|   |   |-- LoginRequest.cs   <- Modelo de login
|   |   |-- AuthResponse.cs   <- Respuesta de autenticacion
|   |   |-- UserDto.cs        <- Datos del usuario
|   |   '-- DashboardStats.cs <- Estadisticas del dashboard
|   |-- Services/             <- Servicios del frontend
|   |   |-- AuthService.cs    <- Manejo de sesion y login (JWT + localStorage)
|   |   |-- JwtAuthStateProvider.cs <- Proveedor de estado de autenticacion
|   |   |-- ApiClient.cs      <- Cliente HTTP con Bearer token
|   |   '-- ToastService.cs   <- Servicio de notificaciones
|   '-- wwwroot/              <- Archivos estaticos
|       |-- index.html        <- Pagina host de Blazor
|       '-- css/app.css       <- Estilos visuales
|
|-- db/
|   '-- init.sql              <- Script que crea las tablas iniciales
|
|-- nginx/
|   '-- nginx.conf            <- Configuracion del servidor web
|
'-- AGENTS.md                 <- Este archivo
```

### Servicios Docker

Todo se levanta con `docker compose up --build -d` (o con `./setup.sh` que hace todo automaticamente):

| Servicio | Que hace | Puerto |
|----------|----------|--------|
| sqlserver | Base de datos SQL Server Express | 1433 (interno) |
| sqlserver-init | Ejecuta init.sql para crear tablas (corre una vez y termina) | - |
| api | Backend .NET 8 con autenticacion JWT | 80 (interno) |
| web | Blazor WASM + Nginx - sirve el frontend y proxea la API | 3000 |

### Como se conectan

```
Browser -> localhost:3000 -> Nginx
                              |-- /            -> Blazor WASM (frontend)
                              |-- /_framework/ -> Runtime de Blazor
                              |-- /api/        -> Backend .NET (api:80)
                              '-- /swagger     -> Documentacion de la API
```

### Herramientas AI (se instalan en la maquina con setup.sh)

- **Claude Code** - `claude` (necesita ANTHROPIC_API_KEY)
- **OpenCode** - `opencode` (necesita OPENAI_API_KEY)
- **Codex CLI** - `codex` (necesita OPENAI_API_KEY)
- **Gemini CLI** - `gemini` (necesita GEMINI_API_KEY)

### Tecnologias

- **Backend**: .NET 8 + C# + Entity Framework Core
- **Base de datos**: SQL Server 2022 Express
- **Frontend**: Blazor WebAssembly (.NET 8) + CSS
- **Servidor web**: Nginx
- **Agentes AI**: Claude Code, OpenCode, Codex CLI, Gemini CLI
- **Autenticacion**: JWT (JSON Web Tokens)
- **Contenedores**: Docker + Docker Compose

---

## Como expandir el proyecto

### Agregar una nueva pagina al dashboard

1. Crear el archivo `.razor` en `src/Web/Pages/NuevaPagina.razor`:
   - Agregar `@page "/ruta"` y `@attribute [Authorize]`
   - Inyectar servicios necesarios (`@inject ApiClient Api`)
   - Implementar la UI con HTML y logica en el bloque `@code {}`

2. Agregar la navegacion en `src/Web/Layout/MainLayout.razor`:
   - Agregar un `<NavItem>` en la seccion `sidebar-nav`
   - Agregar el icono en `src/Web/Shared/SvgIcons.razor` si hace falta

3. Si necesita datos del backend, crear el endpoint en la API:
   - Crear el modelo en `src/Api/Models/`
   - Agregar la tabla en `db/init.sql`
   - Agregar el DbSet en `src/Api/Data/AppDbContext.cs`
   - Crear el controller en `src/Api/Controllers/`
   - Agregar la funcion en `src/Web/Services/ApiClient.cs`

4. Si necesita estilos nuevos, agregarlos en `src/Web/wwwroot/css/app.css`

### Agregar una nueva tabla a la base de datos

1. Crear el modelo C# en `src/Api/Models/NuevoModelo.cs`
2. Agregar `DbSet<NuevoModelo>` en `AppDbContext.cs`
3. Agregar `CREATE TABLE` en `db/init.sql`
4. Crear el controller con los endpoints CRUD

### Cambiar el nombre de la marca

Buscar "Tu Marca" en estos archivos y reemplazar:
- `src/Web/wwwroot/index.html` (titulo)
- `src/Web/Pages/Login.razor` (encabezado del login)
- `src/Web/Layout/MainLayout.razor` (sidebar y topbar)

### Agregar un nuevo servicio Docker

1. Crear una carpeta con su `Dockerfile`
2. Agregarlo en `docker-compose.yml` dentro de `services:`
3. Si necesita ser accesible desde el browser, agregar la ruta en `nginx/nginx.conf`

---

## Credenciales por defecto

| Que | Usuario | Contrasena |
|-----|---------|------------|
| Dashboard | admin | admin123 |
| SQL Server | sa | YourStrong@Passw0rd |

---

## Como levantar el proyecto

### Opcion 1: Instalador automatico (recomendado)

```bash
chmod +x setup.sh
./setup.sh
```

Instala todo lo necesario (Node.js, Python, Docker, herramientas AI) y levanta el proyecto.

### Opcion 2: Manual

```bash
# 1. Copiar variables de entorno
cp .env.example .env

# 2. (Opcional) Poner tus API keys en .env

# 3. Levantar la app
docker compose up --build -d

# 4. Abrir en el browser: http://localhost:3000
```

---

## Resumen para el agente

Cuando el usuario te pida algo:

1. Lee este archivo si no lo leiste
2. Escucha lo que pide y traducilo a tareas tecnicas
3. Dividi en subagentes si es complejo
4. Ejecuta los cambios
5. Probalo
6. Hace commit
7. Explicale al usuario que hiciste, en simple
