# Multi-stage Dockerfile with flexible build directory
# Supports: build/, dist/, public/ output directories

# Build stage
FROM node:20-alpine AS builder

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source code
COPY . .

# Build the Svelte app
RUN npm run build

# Find the build directory and copy to a known location
RUN if [ -d "build" ]; then cp -r build /tmp/output; \
    elif [ -d "dist" ]; then cp -r dist /tmp/output; \
    elif [ -d "public/build" ]; then cp -r public/build /tmp/output; \
    elif [ -d ".svelte-kit/output/client" ]; then cp -r .svelte-kit/output/client /tmp/output; \
    else echo "ERROR: No build output found!" && exit 1; fi

# Production stage
FROM nginx:alpine

# Copy from the standardized location
COPY --from=builder /tmp/output /usr/share/nginx/html/

# Ensure assets directory exists
RUN mkdir -p /usr/share/nginx/html/assets

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Expose ports
EXPOSE 80 443

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
