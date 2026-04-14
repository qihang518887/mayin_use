# -------- 构建阶段 --------
FROM node:lts AS builder

WORKDIR /app
COPY . .

# 安装 pnpm（轻量）
RUN npm install -g pnpm tsx
# 安装依赖
RUN pnpm install --frozen-lockfile

# 复制项目源码并构建
RUN pnpm build

# -------- 运行阶段 --------
FROM node:lts-alpine AS runner

WORKDIR /app

COPY --from=builder /app/dist ./

EXPOSE 8080
EXPOSE 8089

CMD ["node", "noname-server.cjs"]
