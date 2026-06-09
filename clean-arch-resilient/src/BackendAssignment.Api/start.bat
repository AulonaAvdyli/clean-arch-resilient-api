@echo off

:: Run docker compose
docker-compose up -d

:: Check if container is ready
set index=1
:While
if %index% gtr 50 goto EndWhile
    docker exec src_postgres_1 pg_isready -q
    if NOT ERRORLEVEL 1 goto EndWhile
    
    timeout /t 2
    set /A index+=1
    goto While
:EndWhile

echo Postgres is ready

:: Check if Redis is ready
set indexRedis=1
:WhileRedis
if %indexRedis% gtr 50 goto EndRedisWhile
    docker exec src_redis_1 redis-cli ping
    if NOT ERRORLEVEL 1 goto EndRedisWhile
    
    timeout /t 2
    set /A indexRedis+=1
    goto WhileRedis
:EndRedisWhile


echo Redis is ready
