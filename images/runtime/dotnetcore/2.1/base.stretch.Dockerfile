# dotnet tools are currently available as part of SDK so we need to create them in an sdk image
# and copy them to our final runtime image
ARG DEBIAN_FLAVOR
FROM mcr.microsoft.com/dotnet/core/sdk:2.1 AS tools-install
RUN dotnet tool install --tool-path /dotnetcore-tools dotnet-sos --version 5.0.236902

FROM oryx-run-base-stretch
ARG BUILD_DIR=/tmp/oryx/build

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        \
# .NET Core dependencies
        libc6 \
        libgcc1 \
        libgssapi-krb5-2 \
        libicu57 \
        icu-devtools \
        libssl1.0.2 \
        libstdc++6 \
        zlib1g \
        lldb \
        curl \
        file \
        libgdiplus \
    && apt-get upgrade -y \
    && rm -rf /var/lib/apt/lists/*

# Configure web servers to bind to port 80 when present
ENV ASPNETCORE_URLS=http://+:80 \
    # Enable detection of running in a container
    DOTNET_RUNNING_IN_CONTAINER=true \
    PATH="/opt/dotnetcore-tools:${PATH}"

COPY --from=tools-install /dotnetcore-tools /opt/dotnetcore-tools

# Install ASP.NET Core
RUN . ${BUILD_DIR}/__dotNetCoreRunTimeVersions.sh \
    && curl -SL --output aspnetcore.tar.gz https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/$ASPNET_CORE_APP_21/aspnetcore-runtime-$ASPNET_CORE_APP_21-linux-x64.tar.gz \
    && echo "$ASPNET_CORE_APP_21_SHA aspnetcore.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -zxf aspnetcore.tar.gz -C /usr/share/dotnet \
    && rm aspnetcore.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && dotnet-sos install \
    && rm -rf ${BUILD_DIR}
