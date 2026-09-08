# Build stage
FROM golang:1.27-alpine AS builder
WORKDIR /app
COPY . .
RUN go build -o api

# Runtime stage
FROM alpine:latest
WORKDIR /app
COPY --from=builder /app/api .
EXPOSE 8080
CMD ["./api"]