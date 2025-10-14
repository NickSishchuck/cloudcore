FROM nginx:alpine

COPY . /usr/share/nginx/html/

RUN mkdir -p /usr/share/nginx/html/assets
COPY certs/localhost.pem /etc/ssl/certs/localhost.pem
COPY certs/localhost-key.pem /etc/ssl/private/localhost-key.pem
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80 443

CMD ["nginx", "-g", "daemon off;"]