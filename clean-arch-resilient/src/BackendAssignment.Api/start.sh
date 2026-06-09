#!/bin/bash

# Check if Docker Engine is running
if ! docker info > /dev/null 2>&1; then
    echo " Docker Engine is not running. Please start Docker Desktop and try again."
    exit 1
fi

# Number of attempts
WAIT=10
# To keep count of how many attempts have passed
count=0

# Rebuild Docker images with no cache
docker compose build --no-cache

docker compose up -d

while ! docker exec backend-assignment-postgres-1 pg_isready -q  > /dev/null 2> /dev/null; do
    sleep 2

    if [ $count -gt $WAIT ]; then
        docker compose down
        docker compose up &
        count=0
    fi
    ((count++))
done

echo Postgres is ready

# Start Flyway migration
echo "Running Flyway migrations..."
docker compose run --rm flyway migrate

echo "Flyway migrations applied successfully."

redisCount=0
while ! docker exec backend-assignment-redis-1 redis-cli ping > /dev/null 2> /dev/null; do
    sleep 2

    if [ $redisCount -gt $WAIT ]; then
        docker compose down
        docker compose up &
        redisCount=0
    fi
    ((redisCount++))
done

echo Redis is ready