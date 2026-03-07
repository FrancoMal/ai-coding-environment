#!/bin/bash
# ===========================================================
# AI Coding Environment - Instalador
# ===========================================================
# Instala todo lo necesario y levanta el proyecto.
# Uso: chmod +x setup.sh && ./setup.sh
# ===========================================================

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

ok()   { echo -e "  ${GREEN}[OK]${NC} $1"; }
skip() { echo -e "  ${YELLOW}[YA INSTALADO]${NC} $1"; }
fail() { echo -e "  ${RED}[ERROR]${NC} $1"; }
info() { echo -e "\n${YELLOW}>>>${NC} $1\n"; }

# --- Detectar OS ---
detect_os() {
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        OS=$ID
    elif command -v sw_vers &>/dev/null; then
        OS="macos"
    else
        OS="unknown"
    fi
    echo "$OS"
}

OS=$(detect_os)

echo ""
echo "  ==========================================="
echo "  AI Coding Environment - Instalador"
echo "  ==========================================="
echo "  Sistema detectado: $OS"
echo "  ==========================================="

# --- Funciones de instalacion por OS ---
install_apt() {
    sudo apt-get update -qq
    sudo apt-get install -y -qq "$@" > /dev/null 2>&1
}

# ===========================================================
# 1. Herramientas base (git, curl, build tools)
# ===========================================================
info "1/6 - Herramientas base"

if [ "$OS" = "ubuntu" ] || [ "$OS" = "debian" ]; then
    NEEDED=""
    for pkg in git curl wget build-essential ca-certificates gnupg; do
        if ! dpkg -s "$pkg" &>/dev/null; then
            NEEDED="$NEEDED $pkg"
        fi
    done
    if [ -n "$NEEDED" ]; then
        install_apt $NEEDED
        ok "Herramientas base instaladas:$NEEDED"
    else
        skip "Herramientas base"
    fi
elif [ "$OS" = "macos" ]; then
    if ! command -v brew &>/dev/null; then
        /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
        ok "Homebrew instalado"
    fi
    skip "Herramientas base (macOS incluye lo necesario)"
else
    echo "  Sistema no soportado automaticamente. Instala manualmente: git, curl, Node.js, Docker."
    exit 1
fi

# ===========================================================
# 2. Node.js 20
# ===========================================================
info "2/6 - Node.js"

if command -v node &>/dev/null; then
    NODE_VERSION=$(node --version)
    skip "Node.js $NODE_VERSION"
else
    if [ "$OS" = "ubuntu" ] || [ "$OS" = "debian" ]; then
        curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash - > /dev/null 2>&1
        install_apt nodejs
        ok "Node.js $(node --version) instalado"
    elif [ "$OS" = "macos" ]; then
        brew install node@20
        ok "Node.js $(node --version) instalado"
    fi
fi

# ===========================================================
# 3. Python 3
# ===========================================================
info "3/6 - Python"

if command -v python3 &>/dev/null; then
    PYTHON_VERSION=$(python3 --version)
    skip "$PYTHON_VERSION"
else
    if [ "$OS" = "ubuntu" ] || [ "$OS" = "debian" ]; then
        install_apt python3 python3-pip python3-venv
        ok "$(python3 --version) instalado"
    elif [ "$OS" = "macos" ]; then
        brew install python@3
        ok "$(python3 --version) instalado"
    fi
fi

# ===========================================================
# 4. Docker + Docker Compose
# ===========================================================
info "4/6 - Docker"

if command -v docker &>/dev/null; then
    DOCKER_VERSION=$(docker --version 2>/dev/null | cut -d' ' -f3 | tr -d ',')
    skip "Docker $DOCKER_VERSION"
else
    if [ "$OS" = "ubuntu" ] || [ "$OS" = "debian" ]; then
        # Instalar Docker oficial
        sudo install -m 0755 -d /etc/apt/keyrings
        curl -fsSL https://download.docker.com/linux/$OS/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
        sudo chmod a+r /etc/apt/keyrings/docker.gpg
        echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/$OS $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
        install_apt docker-ce docker-ce-cli containerd.io docker-compose-plugin
        sudo usermod -aG docker "$USER"
        ok "Docker $(docker --version | cut -d' ' -f3 | tr -d ',') instalado"
        echo -e "  ${YELLOW}NOTA: Cerra sesion y volve a entrar para usar Docker sin sudo${NC}"
    elif [ "$OS" = "macos" ]; then
        echo -e "  ${YELLOW}Instala Docker Desktop desde: https://www.docker.com/products/docker-desktop/${NC}"
        echo "  Despues de instalarlo, volve a correr este script."
        exit 1
    fi
fi

# Verificar que Docker este corriendo
if ! docker info &>/dev/null; then
    echo -e "  ${YELLOW}Docker no esta corriendo. Intentando iniciar...${NC}"
    sudo systemctl start docker 2>/dev/null || true
    sleep 2
    if ! docker info &>/dev/null; then
        fail "Docker no esta corriendo. Inicialo manualmente y volve a correr este script."
        exit 1
    fi
fi

# ===========================================================
# 5. Herramientas AI
# ===========================================================
info "5/6 - Herramientas AI"

install_npm_tool() {
    local name="$1"
    local package="$2"
    local cmd="$3"
    if command -v "$cmd" &>/dev/null; then
        local ver=$($cmd --version 2>/dev/null | head -1)
        skip "$name ($ver)"
    else
        echo -n "  Instalando $name... "
        if sudo npm install -g "$package" > /dev/null 2>&1; then
            ok "$name instalado"
        else
            fail "$name (podes intentar despues con: npm install -g $package)"
        fi
    fi
}

install_npm_tool "Claude Code"  "@anthropic-ai/claude-code"  "claude"
install_npm_tool "OpenCode"     "opencode-ai"                "opencode"
install_npm_tool "Codex CLI"    "@openai/codex"              "codex"
install_npm_tool "Gemini CLI"   "@google/gemini-cli"         "gemini"

# ===========================================================
# 6. Levantar el proyecto
# ===========================================================
info "6/6 - Proyecto"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# Crear .env si no existe
if [ ! -f .env ]; then
    cp .env.example .env
    ok "Archivo .env creado (edita las API keys si queres)"
else
    skip "Archivo .env"
fi

# Levantar servicios
echo "  Levantando servicios con Docker Compose..."
if docker compose up --build -d 2>&1 | tail -5; then
    ok "Servicios levantados"
else
    fail "Error levantando servicios. Revisa los logs con: docker compose logs"
    exit 1
fi

# ===========================================================
# Resumen final
# ===========================================================
echo ""
echo "  ==========================================="
echo "  Instalacion completa!"
echo "  ==========================================="
echo ""
echo "  Dashboard:   http://localhost:3000"
echo "  Login:       admin / admin123"
echo ""
echo "  Herramientas AI disponibles:"
command -v claude   &>/dev/null && echo "    - claude    (Claude Code)"
command -v opencode &>/dev/null && echo "    - opencode  (OpenCode)"
command -v codex    &>/dev/null && echo "    - codex     (Codex CLI)"
command -v gemini   &>/dev/null && echo "    - gemini    (Gemini CLI)"
echo ""
echo "  Para configurar las API keys, edita .env"
echo "  Para parar todo: docker compose down"
echo "  Para volver a levantar: docker compose up -d"
echo "  ==========================================="
echo ""
