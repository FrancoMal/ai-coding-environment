#!/bin/bash
# Entrypoint: instala herramientas AI en segundo plano y arranca ttyd

MARKER="/home/developer/.ai-tools-installed"

# Si es la primera vez, instalar herramientas AI en segundo plano
if [ ! -f "$MARKER" ]; then
    echo "[workspace] Primera ejecucion: instalando herramientas AI en segundo plano..."
    (
        install-ai-tools
    ) &
fi

# Arrancar ttyd (terminal web)
exec ttyd --writable --port 7681 --base-path /terminal bash --login
