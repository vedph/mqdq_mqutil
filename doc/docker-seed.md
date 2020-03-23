# Seeding MQDQ Database in Cadmus Docker Image

This procedure allows creating a modified `docker-compose` script which uses an *ad-hoc* Docker image to seed the default Cadmus database (named `cadmus`) for the Cadmus API layer.

This implies dumping the MQDQ database into a folder from our server, and copying the dump into a MongoDB-derived docker image, whose only purpose is executing `mongorestore` with that dump.

## Dockerfile

This is the `Dockerfile` to build the seeder image:

```yml
FROM mongo

COPY ./cadmus /
CMD [ "mongorestore", "cadmus" ]
```

In this example, we assume that it's located under a folder named `mqdq_seed`. To create the image (once you have placed the database dump in a subfolder of `mqdq_seed`; see below), enter this folder and execute this command:

```ps1
docker build . -t vedph2020/mqdq_seed:latest
```

## Procedure

We assume that the `mqdq` MongoDB database is available from your MongoDB service.

1. **dump the database** like this: `.\mongodump.exe /db:mqdq /out:C:\Users\dfusi\Desktop\dump\`. If your machine has no MongoDB installed (in mine, I use MongoDB as a dockerized service), you will need to download the [MongoDB tools](https://www.mongodb.com/download-center/community) for dumping databases: select the target OS, then `ZIP` (for Windows) or `tools` (for Ubuntu), and download.

2. **copy** the contents of the `mqdq` directory created in the dump folder into a `mqdq_seed\cadmus` folder.

3. **create the Docker seeder image** from folder `mqdq_seed` (see above).

Once you have this image, just insert it in the compose stack, e.g.:

```yml
version: '3.7'

services:
  # MongoDB
  cadmus-mongo:
    image: mongo
    container_name: cadmus-mongo
    environment:
      - MONGO_DATA_DIR=/data/db
      - MONGO_LOG_DIR=/dev/null
    command: mongod --logpath=/dev/null # --quiet
    ports:
      - 27017:27017
    networks:
      - cadmus-network

  mqdq-seed:
    image: vedph2020/mqdq_seed:latest
    depends_on:
      - cadmus-mongo

  cadmus-api:
    image: vedph2020/cadmus_api:latest
    ports:
      # https://stackoverflow.com/questions/48669548/why-does-aspnet-core-start-on-port-80-from-within-docker
      - 60304:80
    depends_on:
      - cadmus-mongo
    # wait for mongo before starting: https://github.com/vishnubob/wait-for-it
    command: ["./wait-for-it.sh", "cadmus-mongo:27017", "--", "dotnet", "CadmusApi.dll"]
    environment:
      # for Windows use : as separator, for non Windows use __
      # (see https://github.com/aspnet/Configuration/issues/469)
      - CONNECTIONSTRINGS__DEFAULT=mongodb://cadmus-mongo:27017/{0}
      - MESSAGING__APIROOTURL=http://cadmusapi.azurewebsites.net
      - MESSAGING__APPROOTURL=http://cadmusapi.com/
      - MESSAGING__SUPPORTEMAIL=support@cadmus.com
      - SENDGRID__ISENABLED=true
      - SENDGRID__SENDEREMAIL=info@cadmus.com
      - SENDGRID__SENDERNAME=cadmus
      - SENDGRID__APIKEY=todo
      - SERILOG__CONNECTIONSTRING=mongodb://cadmus-mongo:27017/cadmus-logs
      - STOCKUSERS__0__PASSWORD=P4ss-W0rd!
    networks:
      - cadmus-network

  cadmus-web:
    image: vedph2020/cadmus_web:latest
    ports:
      - 4200:80
    depends_on:
      - cadmus-api
    networks:
      - cadmus-network

networks:
  cadmus-network:
    driver: bridge
```

Here the addition is just the seed layer:

```yml
  mqdq-seed:
    image: vedph2020/mqdq_seed:latest
    depends_on:
      - cadmus-mongo
```
