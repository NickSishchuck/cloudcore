# "Я здесь впервые"
Сначала создай .env в своём локальном проекте. Перенеси содержимое из .envTemplate
в новосозданный .env. Поменяй пароль.


```dockerfile
# Полная сборка и запуск
docker-compose up --build

# Или пошагово для отладки
docker-compose up database --build    # сначала БД
docker-compose up backend --build     # потом бекенд  
docker-compose up frontend --build    # потом фронтенд

# Проверка логов
docker-compose logs backend
docker-compose logs frontend
```

# Что-то пошло не так
```dockerfile
# Посмотри логи конкретного сервиса
docker compose logs backend
docker compose logs frontend  
docker compose logs database

# Пересборка
docker compose down
docker compose up --build --force-recreate
```

# Управление контейнерами
```dockerfile
# Запуск только определённого сервиса
docker compose up backend database --build   # без фронтенда
docker compose up database --build           # только БД

# Остановка без удаления
docker compose stop

# Перезапуск сервиса
docker compose restart backend
```
# Работа с БД
```dockerfile
# Подключение к MySQL
docker compose exec database mysql -uroot -p {пароль указан в .env. Ты должен будешь
 ввести его интерактивно. Если хочешь автоматически, убери пробел между -p и паролем}

# Выполнение SQL команды
docker compose exec database mysql -uroot -p CloudCoreDB -e "SELECT * FROM users;"

# Просмотр логов БД
docker compose logs database --follow

# Пересоздание БД
docker compose down --volumes
    #Поднятие всего приложения
    docker compose up --build
    
MySQL автоматически по-очереди пройдётся по файлам в ../database/init/, но только если в вольюме будет пусто. 
По-этому мы удаляем содержимое вольюма, чтобы внести обновления в бд.

``` 

# Работа с storage
```dockerfile
У нас volume mapping, по-этому содержимое storage можно просматривать прям локально
через проводник


Если бы у нас его не было, было бы:
docker compose exec backend ls -la /app/storage
```

# Очистка системы
```dockerfile
# Полная очистка проекта
docker compose down --volumes --rmi all
docker system prune -f

# Очистка только volumes
docker compose down --volumes

# Очистка Docker кэша
docker builder prune -f

# Очистка неиспользуемых образов
docker image prune -a
```

# Добавление https
```https setup
# Установка mkcert для локального самоподписаного сертификата
sudo pacman -Syu mkcert nss
mkcert -install

# Создание локальных сертификатов
mkcert -cert-file /frontend/certs/localhost.pem -key-file /frontend/certs/localhost-key.pem localhost 127.0.0.1 ::1

# Должны быть в /frontend/certs
```