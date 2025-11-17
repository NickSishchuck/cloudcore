FROM caddy:alpine

# Copy your frontend files
COPY . /usr/share/caddy/

# Create assets directory
RUN mkdir -p /usr/share/caddy/assets

# Copy Caddy configuration
COPY Caddyfile /etc/caddy/Caddyfile

EXPOSE 80

CMD ["caddy", "run", "--config", "/etc/caddy/Caddyfile"]
