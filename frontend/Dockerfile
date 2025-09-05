FROM nginx:alpine

COPY . /usr/share/nginx/html/

RUN mkdir -p /usr/share/nginx/html/assets

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]