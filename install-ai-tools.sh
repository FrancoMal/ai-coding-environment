#!/bin/bash
# Install AI coding tools
# Run this script to install/update AI tools: install-ai-tools

MARKER="$HOME/.ai-tools-installed"

echo ""
echo "  Instalando herramientas AI..."
echo ""

install_tool() {
    local name="$1"
    local package="$2"
    echo -n "  [$name] Instalando... "
    if npm install -g "$package" > /dev/null 2>&1; then
        echo "OK"
    else
        echo "FALLO (podes intentar despues con: npm install -g $package)"
    fi
}

install_tool "Claude Code" "@anthropic-ai/claude-code"
install_tool "Codex CLI"   "@openai/codex"
install_tool "Gemini CLI"  "@google/gemini-cli"

touch "$MARKER"
echo ""
echo "  Listo! Herramientas instaladas."
echo "  Para reinstalar: install-ai-tools"
echo ""
