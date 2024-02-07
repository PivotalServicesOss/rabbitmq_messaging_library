#!/usr/bin/env sh

docker compose up -d
echo '   Sleeping for 30 s because RabbitMQ takes forever to start'
sleep 30
dotnet run
docker compose down
