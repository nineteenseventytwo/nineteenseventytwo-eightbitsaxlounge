FROM node:26-slim@sha256:ec7758ee051e457b468b32bde57b0879010b325bb9862718e9615225ce4aaae1
# The base image already ships a `node` user at uid/gid 1000 for exactly
# this — confirmed live rather than assumed, since a naive `useradd -u 1000`
# collides with it (exit 4, UID already in use).
WORKDIR /app
COPY package.json package-lock.json* ./
# Use npm ci when a lockfile exists for deterministic installs; otherwise fall back to npm install
RUN if [ -f package-lock.json ]; then \
      npm ci --omit=dev --no-audit --no-fund; \
    else \
      npm install --omit=dev --no-audit --no-fund; \
    fi
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
