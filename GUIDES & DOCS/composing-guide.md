# "I am new here"
First, create .env in your local project. Transfer the contents from .envTemplate to the newly created .env. 
Change the password.


```dockerfile
# Full build and run
docker-compose up --build

# Or step-by-step for debugging
docker-compose up database --build    # database first
docker-compose up backend --build     # then backend 
docker-compose up frontend --build    # then frontend

# Check logs
docker-compose logs backend
docker-compose logs frontend
```

# Something went wrong
```dockerfile
# View logs of a specific service
docker compose logs backend
docker compose logs frontend  
docker compose logs database

# Rebuild
docker compose down
docker compose up --build --force-recreate
```

Container Management
```dockerfile
# Run only specific service
docker compose up backend database --build   # without frontend
docker compose up database --build           # database only

# Stop without removing
docker compose stop

# Restart service
docker compose restart backend
```
Working with Database
```dockerfile
# Connect to MySQL
docker compose exec database mysql -uroot -p {password is specified in .env. You will need to
 enter it interactively. If you want it automatically, remove the space between -p and password}

# Execute SQL command
docker compose exec database mysql -uroot -p CloudCoreDB -e "SELECT * FROM users;"

# View database logs
docker compose logs database --follow

# Recreate database
docker compose down --volumes
    # Bring up the entire application
    docker compose up --build
    
MySQL will automatically go through files in ../database/init/ sequentially, but only if the volume is empty. 
That's why we delete the volume contents to apply updates to the database.

``` 

# Working with storage
```dockerfile
We have volume mapping, so storage contents can be viewed locally
through file explorer


If we didn't have it, it would be:
docker compose exec backend ls -la /app/storage
```

# System Cleanup
```dockerfile
# Full project cleanup
docker compose down --volumes --rmi all
docker system prune -f

# Cleanup only volumes
docker compose down --volumes

# Cleanup Docker cache
docker builder prune -f

# Cleanup unused images
docker image prune -a
```

# Adding https
```https setup
# Install mkcert for local self-signed certificate
sudo pacman -Syu mkcert nss
mkcert -install

# Create local certificates
mkcert -cert-file /frontend/certs/localhost.pem -key-file /frontend/certs/localhost-key.pem localhost 127.0.0.1 ::1

# Should be in /frontend/certs
```