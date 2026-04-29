# Stage 1: Deps
FROM node:20-alpine AS deps
RUN apk add --no-cache libc6-compat
WORKDIR /app
RUN corepack enable
COPY ./package.json ./yarn.lock ./.yarnrc.yml ./
RUN yarn install --immutable

# Stage 2: Build
FROM node:20-alpine AS builder
WORKDIR /app
RUN corepack enable
ARG NEXT_PUBLIC_ENABLE_IDEM_FORM=false
ARG NEXT_PUBLIC_SHOW_SEEDED_IDEMS=false
ENV NEXT_PUBLIC_ENABLE_IDEM_FORM=$NEXT_PUBLIC_ENABLE_IDEM_FORM
ENV NEXT_PUBLIC_SHOW_SEEDED_IDEMS=$NEXT_PUBLIC_SHOW_SEEDED_IDEMS
COPY --from=deps /app/node_modules ./node_modules
COPY --from=deps /app/yarn.lock ./yarn.lock
COPY . .
RUN yarn build

# Stage 3: Run
FROM node:20-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
RUN addgroup -S --gid 1001 nodejs && adduser -S nextjs -G nodejs -u 1001

COPY --from=builder --chown=nextjs:nodejs /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs
EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"
CMD ["node", "server.js"]