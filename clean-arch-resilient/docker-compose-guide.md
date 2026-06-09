# Docker compose guide

For the exercise assigned to you, two docker images are provided from docker hub. 
The configuration for the images can be found in the folder
> src/Configs

with the following names
* Redis
    * redis.conf
* Postgresql
    * postgresql.conf
    * pg_hba.conf

---
## Running the containers
Make sure that you have docker-compose installed and running by using the following command.  
```sh
$ docker-compose --version
```

The result should look like this
```sh
docker-compose version 1.22.0, build f46880fe
```

In order to run the services use the scripts provided.  
You can find startup scripts for both windows and linux located at the `src` folder.

These scripts will start docker-compose and check that services are running.
```sh
$ start.bat
```
---
## Running the containers by yourself (Alternative)
If (for some reason) you need to run the containers by yourself, you can do that by following the steps bellow


#### Building and running the Redis Docker
```sh
# From within the root exercise folder
$ cd .\src
$ docker-compose up redis
$ docker-compose exec redis sh
```

#### Building and running the Postgres Docker
```sh
# From within the root exercise folder
$ cd .\src
$ docker-compose up postgres
$ docker-compose exec postgres sh
```

To find the ip address of a container you can run the following command
```sh
$ docker inspect <container name or id>
```

---
## Troubleshooting
If you encounter an error similar to this
```sh
failed to build: no matching manifest for unknown in the manifest list entries
```
It is most likely caused by running the command with wrong containers.  
Make sure that you are set to work with Linux Containers, this can be achieved from the Docker daemon.