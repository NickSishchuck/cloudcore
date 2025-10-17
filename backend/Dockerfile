FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY *.csproj ./
COPY CloudCore.Tests/*.csproj ./CloudCore.Tests/
RUN dotnet restore

COPY . ./
RUN dotnet build -c Release --no-restore

# Test stage (optional)
FROM build AS test
ARG RUN_TESTS=false
RUN if [ "$RUN_TESTS" = "true" ]; then \
  echo "Running tests..." && \
  dotnet test --no-build -c Release --logger "console;verbosity=normal"; \
  else \
  echo "Skipping tests (RUN_TESTS=$RUN_TESTS)"; \
  fi

FROM build AS publish
RUN dotnet publish -c Release -o out --no-build

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

RUN mkdir -p /app/storage && \
  chmod 755 /app/storage

COPY --from=publish /app/out .

EXPOSE 5000

ENTRYPOINT ["dotnet", "CloudCore.dll"]
