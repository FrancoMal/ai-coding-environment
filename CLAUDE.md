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
- Si tocaste Docker: hace `docker-compose up --build -d` y verifica que los containers esten corriendo

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
|-- docker-compose.yml        <- Orquesta todos los servicios
|-- .env.example              <- Variables de entorno (copiar a .env)
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
|-- db/
|   '-- init.sql              <- Script que crea las tablas iniciales
|
|-- web/                      <- Frontend (lo que ve el usuario en el browser)
|   |-- index.html            <- Pagina principal del dashboard
|   |-- login.html            <- Pagina de login
|   |-- css/dashboard.css     <- Estilos visuales
|   '-- js/
|       |-- auth.js           <- Manejo de sesion y login
|       |-- api.js            <- Conexion con la API
|       '-- dashboard.js      <- Logica del dashboard (Dashboard + Configuracion)
|
|-- workspace/
|   '-- Dockerfile            <- Container con herramientas de desarrollo
|
|-- nginx/
|   '-- nginx.conf            <- Configuracion del servidor web
|
'-- AGENTS.md                 <- Este archivo
```

### Servicios Docker

| Servicio | Que hace | Puerto |
|----------|----------|--------|
| sqlserver | Base de datos SQL Server Express | 1433 |
| api | Backend .NET 8 con autenticacion JWT | interno |
| web | Nginx - sirve el frontend y hace de intermediario | 8080 |
| workspace | Ubuntu con herramientas (git, node, python, claude code). Accesible via terminal web en /terminal | interno |

### Como se conectan

```
Usuario (browser)
    |
    v
  Nginx (:8080)
    |-- /            -> Archivos del frontend (web/)
    |-- /api/        -> Backend .NET (api:80)
    '-- /terminal    -> Terminal web del workspace (workspace:7681)
                        (no visible en el dashboard, acceso directo por URL)
```

### Tecnologias

- **Backend**: .NET 8 + C# + Entity Framework Core
- **Base de datos**: SQL Server 2022 Express
- **Frontend**: HTML + CSS + JavaScript puro (sin frameworks)
- **Servidor web**: Nginx
- **Workspace**: Ubuntu 22.04, Node.js 20, Python 3, Git, FFmpeg, Claude Code
- **Terminal web**: ttyd
- **Autenticacion**: JWT (JSON Web Tokens)
- **Contenedores**: Docker + Docker Compose

---

## Como expandir el proyecto

### Agregar una nueva pagina al dashboard

1. En `web/js/dashboard.js`:
   - Agregar un icono nuevo en el objeto `Icons`
   - Agregar un item de navegacion en `renderShell()`
   - Agregar un case en `navigateTo()`
   - Crear la funcion `renderNuevaPagina()`

2. Si necesita datos del backend, crear el endpoint en la API:
   - Crear el modelo en `src/Api/Models/`
   - Agregar la tabla en `db/init.sql`
   - Agregar el DbSet en `src/Api/Data/AppDbContext.cs`
   - Crear el controller en `src/Api/Controllers/`
   - Agregar la funcion en `web/js/api.js`

3. Si necesita estilos nuevos, agregarlos en `web/css/dashboard.css`

### Agregar una nueva tabla a la base de datos

1. Crear el modelo C# en `src/Api/Models/NuevoModelo.cs`
2. Agregar `DbSet<NuevoModelo>` en `AppDbContext.cs`
3. Agregar `CREATE TABLE` en `db/init.sql`
4. Crear el controller con los endpoints CRUD

### Cambiar el nombre de la marca

Buscar "Tu Marca" en estos archivos y reemplazar:
- `web/index.html` (titulo)
- `web/login.html` (titulo y encabezado)
- `web/js/dashboard.js` (sidebar y topbar)

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

```bash
# 1. Copiar variables de entorno
cp .env.example .env

# 2. (Opcional) Poner tu API key de Anthropic en .env
#    Esto habilita Claude Code en la terminal

# 3. Levantar todo
docker-compose up --build -d

# 4. Abrir en el browser
# http://localhost:8080
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
