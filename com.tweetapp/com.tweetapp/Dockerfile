#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#supervisor is being used to run multiple processes in the same containerRUN apt-get update && apt-get install -y supervisor
 RUN mkdir -p /var/log/supervisord

# Install FilebeatRUN apt-get update
RUN apt-get install -y wget
RUN apt-get install -y gnupg2
RUN wget -qO - https://artifacts.elastic.co/GPG-KEY-elasticsearch | apt-key add -
RUN echo "deb https://artifacts.elastic.co/packages/7.x/apt stable main" | tee -a /etc/apt/sources.list.d/elastic-7.x.list
RUN apt-get install apt-transport-https
RUN apt-get update && apt-get install filebeat

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["com.tweetapp/com.tweetapp.csproj", "com.tweetapp/"]
RUN dotnet restore "com.tweetapp/com.tweetapp.csproj"
COPY . .
WORKDIR "/src/com.tweetapp"
RUN dotnet build "com.tweetapp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "com.tweetapp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

# Copy configs into DockerCOPY log4netLogsToElasticsearch/filebeat.yml /etc/filebeat/filebeat.yml
 COPY ["com.tweetapp/supervisord.conf", "/etc/supervisor/conf.d/supervisord.conf"]

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "com.tweetapp.dll"]