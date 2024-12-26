FROM debian:12

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

RUN apt-get update && apt-get install -y libssl-dev vim \
  && rm -rf /var/lib/apt/lists/* \
  && mkdir /opt/data

COPY ./PasswordManagerApp/bin/Release/net6.0/debian-x64/publish /opt/PasswordManager

# TODO:
# - Copy config file & password file. Might explain that in a script.
# - We expect the "data" dir path to be "../data". 

WORKDIR /opt/PasswordManager

EXPOSE 5000

ENTRYPOINT ./PasswordManagerApp