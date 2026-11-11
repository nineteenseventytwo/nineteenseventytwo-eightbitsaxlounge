FROM node:26-slim@sha256:5adad64f9a1fca2d51972caa65983634c8a774eb300d72b20f7f2a0f60368072
# apt-get upgrade: the base ships apt and zlib1g, and Debian's fixes for
# them (CVE-2026-109518, CVE-2026-85091) were in trixie-security before the
# upstream image was rebuilt. Release builds run on fresh runners with no
# layer cache, so every release picks up whatever Debian has fixed since.
RUN apt-get update \
    && apt-get upgrade -y \
    && rm -rf /var/lib/apt/lists/*
# The base image already ships a `node` user at uid/gid 1000 for exactly
# this — confirmed live rather than assumed, since a naive `useradd -u 1000`
# collides with it (exit 4, UID already in use).
WORKDIR /app
COPY package.json package-lock.json* ./
# Use npm ci when a lockfile exists for deterministic installs; otherwise fall back to npm install
# npm is only needed to build the image (CMD runs node directly), so it goes
# once the install is done. The base image's npm bundles its own undici and
# brace-expansion, which carried six fixable highs on 2026-11-11. Same fix as
# chat's pip uninstall.
RUN if [ -f package-lock.json ]; then \
      npm ci --omit=dev --no-audit --no-fund; \
    else \
      npm install --omit=dev --no-audit --no-fund; \
    fi \
    && rm -rf /usr/local/lib/node_modules/npm /usr/local/bin/npm /usr/local/bin/npx /root/.npm
COPY src/ ./src/
COPY public/ ./public/
COPY scripts/ ./scripts/
# CKV_DOCKER_3 — chown after every COPY lands so `app` owns what it runs,
# then drop out of root for good.
RUN chown -R node:node /app
USER node
ENV PORT=3000
EXPOSE 3000
CMD ["node", "src/index.js"]
