# "First Time Here"

First, create a .env file in your local project. Transfer the contents from .envTemplate
to the newly created .env file. Change the password.

```dockerfile
# Full build and run
docker-compose up --build

# Or step by step for debugging
docker-compose up database --build    # first DB
docker-compose up backend --build     # then backend  
docker-compose up frontend --build    # then frontend

# Check logs
docker-compose logs backend
docker-compose logs frontend
```

# Running with tests

```dockerfile
RUN_TESTS=true docker-compose up --build
# Or add to .env file
echo "RUN_TESTS=true" >> .env
docker-compose up --build
# By default tests do NOT run
docker-compose up --build
# Only backend with tests
RUN_TESTS=true docker-compose up backend --build
```

# Something went wrong

```dockerfile
# Check logs of specific service
docker compose logs backend
docker compose logs frontend  
docker compose logs database

# Rebuild
docker compose down
docker compose up --build --force-recreate
# If tests fail during build
# Remove RUN_TESTS or set to false
RUN_TESTS=false docker compose up --build
```

# Container management

```dockerfile
# Run only specific service
docker compose up backend database --build   # without frontend
docker compose up database --build           # only DB

# Stop without removing
docker compose stop

# Restart service
docker compose restart backend
```

# Working with DB

```dockerfile
# Connect to MySQL
docker compose exec database mysql -uroot -p {password is specified in .env. You will need to
 enter it interactively. If you want it automatically, remove the space between -p and password}

# Execute SQL command
docker compose exec database mysql -uroot -p CloudCoreDB -e "SELECT * FROM users;"

# View DB logs
docker compose logs database --follow

# Recreate DB
docker compose down --volumes
    # Start the entire application
    docker compose up --build
    
MySQL will automatically go through files in ../database/init/ in sequence, but only if the volume is empty. 
That's why we delete the volume contents to apply updates to the database.
```

# Working with storage

```dockerfile
We have volume mapping, so storage contents can be viewed locally
through file explorer

If we didn't have it, it would be:
docker compose exec backend ls -la /app/storage
```

# Testing

```dockerfile
# Run tests locally (without Docker)
cd backend
dotnet test
# Run tests in Docker during build
RUN_TESTS=true docker-compose up backend --build
# Run specific test
dotnet test --filter "FullyQualifiedName~UnitTest1"
# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

# System cleanup

```dockerfile
# Complete project cleanup
docker compose down --volumes --rmi all
docker system prune -f

# Cleanup only volumes
docker compose down --volumes

# Cleanup Docker cache
docker builder prune -f

# Cleanup unused images
docker image prune -a
```

# Adding HTTPS

```https setup
# Install mkcert for local self-signed certificate
sudo pacman -Syu mkcert nss
mkcert -install

# Create local certificates
mkcert -cert-file /frontend/certs/localhost.pem -key-file /frontend/certs/localhost-key.pem localhost 127.0.0.1 ::1

# Should be in /frontend/certs
```
