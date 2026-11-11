# Pinned by digest (CKV_DOCKER_7), like the other services. 3.14-slim was
# 3.14.8 when pinned; Dependabot's docker ecosystem moves it forward.
FROM python:3.14-slim@sha256:9aaa474097d094040649f2162a627c0049b408f52138d980916788d80f04802f

# apt-get upgrade: the base ships apt and zlib1g, and Debian's fixes for
# them (CVE-2026-109518, CVE-2026-85091) were in trixie-security before the
# upstream image was rebuilt. Release builds run on fresh runners with no
# layer cache, so every release picks up whatever Debian has fixed since.
RUN apt-get update \
    && apt-get upgrade -y \
    && rm -rf /var/lib/apt/lists/*

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