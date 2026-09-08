FROM python:3.14.7-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY src/ ./src/

RUN useradd --create-home --shell /bin/bash --uid 1000 app \
    && chown -R app:app /app \
    && mkdir -p /app/tokens \
    && chown -R app:app /app/tokens
USER app

ENV PYTHONPATH=/app/src

EXPOSE 8080

CMD ["python", "-m", "main"]