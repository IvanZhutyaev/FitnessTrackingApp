FROM mcr.microsoft.com/dotnet/sdk:9.0

RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /app
RUN mkdir -p FitnessTrackerBack

COPY ["FitnessTrackerBack.csproj", "FitnessTrackerBack/"]
WORKDIR /app/FitnessTrackerBack
RUN dotnet restore

COPY . .
WORKDIR /app

EXPOSE 5024

ENTRYPOINT ["dotnet", "watch", "--project", "FitnessTrackerBack", "run", "--urls", "http://0.0.0.0:5024"]















