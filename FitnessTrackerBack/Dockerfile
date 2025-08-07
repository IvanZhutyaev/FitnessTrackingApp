FROM mcr.microsoft.com/dotnet/sdk:9.0

RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /app
RUN mkdir -p FitnessTrackerBack

COPY ["FitnessTrackerBack.csproj", "FitnessTrackerBack/"]
WORKDIR /app/FitnessTrackerBack
RUN dotnet restore

COPY . .
WORKDIR /app

COPY entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh


EXPOSE 5024

ENTRYPOINT ["/app/entrypoint.sh"]















