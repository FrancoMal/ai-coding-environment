#!/bin/bash
# Instala herramientas AI en el workspace
# Se ejecuta automaticamente la primera vez, o manualmente con: install-ai-tools

MARKER="/home/developer/.ai-tools-installed"

echo ""
echo "  ========================================="
echo "  Instalando herramientas AI..."
echo "  ========================================="
echo ""

install_tool() {
    local name="$1"
    local package="$2"
    echo -n "  [$name] Instalando... "
    if sudo npm install -g "$package" > /dev/null 2>&1; then
        echo "OK"
    else
        echo "FALLO (podes intentar despues con: sudo npm install -g $package)"
    fi
}

install_tool "Claude Code" "@anthropic-ai/claude-code"
install_tool "OpenCode"    "opencode-ai"
install_tool "Codex CLI"   "@openai/codex"
install_tool "Gemini CLI"  "@google/gemini-cli"

touch "$MARKER"

echo ""
echo "  ========================================="
echo "  Herramientas AI instaladas."
echo "  Para reinstalar: install-ai-tools"
echo "  ========================================="
echo ""
