# --- Build stage ---
FROM node:18-alpine AS build

# Environment: 'dev' or 'prod' (controls theming)
ARG VITE_ENV=prod
ENV VITE_ENV=$VITE_ENV

# Game server WebSocket URL (Colyseus)
ARG VITE_GAME_SERVER_URL
ENV VITE_GAME_SERVER_URL=$VITE_GAME_SERVER_URL

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# --- Production stage ---
FROM nginx:alpine

# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html

# Copy Unity build if present
COPY --from=build /app/public/unity-build /usr/share/nginx/html/unity-build

# Nginx config for SPA + gzip precompressed Unity files
RUN cat > /etc/nginx/conf.d/default.conf << 'EOF'
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml text/javascript application/wasm;

    location /unity-build/Build/ {
        types {
            application/javascript  js;
            application/wasm        wasm;
            application/octet-stream data;
        }
        add_header Cache-Control "no-cache, must-revalidate";
        gzip on;
        gzip_min_length 1000;
        gzip_types application/javascript application/wasm application/octet-stream;
    }

    # Aggressive cache for hashed Vite assets (NOT Unity build files)
    location ~* ^(?!/unity-build/).*\.(js|css|png|jpg|jpeg|gif|ico|svg|woff2?)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    location / {
        try_files $uri $uri/ /index.html;
    }
}
EOF

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]