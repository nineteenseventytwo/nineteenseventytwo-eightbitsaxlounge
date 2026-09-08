# Build stage
FROM golang:1.27-alpine@sha256:cf6fca6641884b8433441b2b0652976f975e1d0fdd26d177eaaf8596087f3125 AS builder
WORKDIR /app
COPY . .
RUN go build -o api

# Runtime stage
# Pinned to a digest, not `latest` — CKV_DOCKER_7. A floating tag means the
# image this Dockerfile produces today is not the image it produces next
# month, and Dependabot (docker ecosystem, .github/dependabot.yml) is what
# now moves this pin forward deliberately instead of silently.
FROM alpine:3.22@sha256:14358309a308569c32bdc37e2e0e9694be33a9d99e68afb0f5ff33cc1f695dce
RUN addgroup -g 1000 app && adduser -D -u 1000 -G app app
WORKDIR /app
COPY --from=builder /app/api .
# CKV_DOCKER_3. The prod tenant namespace enforces Pod Security Standards
# `restricted`, which requires runAsNonRoot — this is that, at the image
# rather than the manifest.
USER app
EXPOSE 8080
CMD ["./api"]
