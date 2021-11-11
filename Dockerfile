FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["TestK8s/TestK8s.csproj", "TestK8s/"]
RUN dotnet restore "TestK8s/TestK8s.csproj"
COPY . .
WORKDIR "/src/TestK8s"
RUN dotnet build "TestK8s.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestK8s.csproj" -c Release -o /app/publish

FROM base AS final

# Bring in metadata via --build-arg to publish
ARG BRANCH=unknown
ARG IMAGE_CREATED=unknown
ARG IMAGE_REVISION=unknown
ARG IMAGE_VERSION=unknown

# Configure image labels
LABEL branch=$branch \
    maintainer="Harald Nagel <dev@hpn.is>" \
    org.opencontainers.image.authors="Harald Nagel <dev@hpn.is>" \
    org.opencontainers.image.created=$IMAGE_CREATED \
    org.opencontainers.image.description="Basic test of Kubernetes with ASP.NET Core" \
    org.opencontainers.image.documentation="https://github.com/k7hpn/testk8s" \
    org.opencontainers.image.licenses="MIT" \
    org.opencontainers.image.revision=$IMAGE_REVISION \
    org.opencontainers.image.source="https://github.com/k7hpn/testk8s" \
    org.opencontainers.image.title="TestK8s" \
    org.opencontainers.image.url="https://github.com/k7hpn/testk8s" \
    org.opencontainers.image.version=$IMAGE_VERSION

# Default image environment variable settings
ENV org.opencontainers.image.created=$IMAGE_CREATED \
    org.opencontainers.image.revision=$IMAGE_REVISION \
    org.opencontainers.image.version=$IMAGE_VERSION

WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TestK8s.dll"]