FROM node:26-slim
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
ENV PORT=3000
EXPOSE 3000
CMD ["node", "src/index.js"]
