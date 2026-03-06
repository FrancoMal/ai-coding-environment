FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive
ENV LANG=C.UTF-8
ENV LC_ALL=C.UTF-8

# Install base system tools
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    git \
    python3 \
    python3-pip \
    python3-venv \
    ffmpeg \
    build-essential \
    sudo \
    vim \
    nano \
    htop \
    unzip \
    jq \
    ca-certificates \
    gnupg \
    && rm -rf /var/lib/apt/lists/*

# Install Node.js 20 LTS
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Install Claude Code globally
RUN npm install -g @anthropic-ai/claude-code 2>/dev/null || echo "Claude Code will need to be installed with API key"

# Install ttyd (web terminal)
RUN wget -q https://github.com/tsl0922/ttyd/releases/download/1.7.7/ttyd.x86_64 -O /usr/local/bin/ttyd \
    && chmod +x /usr/local/bin/ttyd

# Create developer user with sudo
RUN useradd -m -s /bin/bash developer \
    && echo "developer ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers \
    && mkdir -p /home/developer/workspace \
    && chown -R developer:developer /home/developer

# Create a welcome script
RUN echo '#!/bin/bash\n\
echo ""\n\
echo "  ===================================="\n\
echo "  Welcome to AI Coding Environment"\n\
echo "  ===================================="\n\
echo ""\n\
echo "  Tools disponibles:"\n\
echo "    - git          $(git --version 2>/dev/null | cut -d\" \" -f3)"\n\
echo "    - node         $(node --version 2>/dev/null)"\n\
echo "    - npm          $(npm --version 2>/dev/null)"\n\
echo "    - python3      $(python3 --version 2>/dev/null | cut -d\" \" -f2)"\n\
echo "    - pip3         $(pip3 --version 2>/dev/null | cut -d\" \" -f2)"\n\
echo "    - ffmpeg       $(ffmpeg -version 2>/dev/null | head -1 | cut -d\" \" -f3)"\n\
echo "    - claude       (Claude Code CLI)"\n\
echo ""\n\
echo "  Para usar Claude Code:"\n\
echo "    export ANTHROPIC_API_KEY=tu-api-key"\n\
echo "    claude"\n\
echo ""\n\
echo "  Directorio de trabajo: ~/workspace"\n\
echo "  ===================================="\n\
echo ""\n\
' > /home/developer/.welcome.sh \
    && chmod +x /home/developer/.welcome.sh \
    && echo 'source ~/.welcome.sh' >> /home/developer/.bashrc

USER developer
WORKDIR /home/developer/workspace

EXPOSE 7681

# Start ttyd with bash
CMD ["ttyd", "--writable", "--port", "7681", "--base-path", "/terminal", "bash", "--login"]
