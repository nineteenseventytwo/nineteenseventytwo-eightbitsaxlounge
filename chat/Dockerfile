# Pinned by digest (CKV_DOCKER_7), like the other services. 3.14-slim was
# 3.14.8 when pinned; Dependabot's docker ecosystem moves it forward.
FROM python:3.14-slim@sha256:a2b82f3c48559aa0a8446d9af49826b6e2b2016f4cd2afabfe6013ec53729170

WORKDIR /app

COPY requirements.txt .
# pip is only needed to build the image. The base image's pip vendors
# certifi 2026.6.17 (GHSA-6q48-6c7w-9gw2), which blocked the 7.0.5 release.
RUN pip install --no-cache-dir -r requirements.txt \
    && pip uninstall -y pip

COPY src/ ./src/

RUN useradd --create-home --shell /bin/bash --uid 1000 app \
    && chown -R app:app /app \
    && mkdir -p /app/tokens \
    && chown -R app:app /app/tokens
USER app

ENV PYTHONPATH=/app/src

EXPOSE 8080

CMD ["python", "-m", "main"]