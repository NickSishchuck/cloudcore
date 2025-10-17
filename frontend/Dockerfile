FROM nginx:alpine

COPY . /usr/share/nginx/html/

RUN mkdir -p /usr/share/nginx/html/assets

COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80 443

CMD ["nginx", "-g", "daemon off;"]