# Я здесь впервые
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
docker compose exec database mysql -uroot -p FileStorage -e "SELECT * FROM users;"

# Просмотр логов БД
docker compose logs database --follow
``` 

# Работа с storage
```dockerfile
# Просмотр файлов в storage
docker compose exec backend ls -la /app/storage

# Очистка storage (!)
sudo rm -rf ./storage/*
mkdir -p ./storage/users
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