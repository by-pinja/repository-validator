# This container supports local testing of functions
FROM mcr.microsoft.com/dotnet/sdk:3.1-alpine AS build
WORKDIR /source

COPY ValidationLibrary.AzureFunctions/. ./ValidationLibrary.AzureFunctions/
COPY ValidationLibrary.GitHub/. ./ValidationLibrary.GitHub/
COPY ValidationLibrary.Rules/. ./ValidationLibrary.Rules/
COPY ValidationLibrary/. ./ValidationLibrary/

WORKDIR /source/ValidationLibrary.AzureFunctions
RUN dotnet publish -c release -o /app

FROM mcr.microsoft.com/azure-functions/dotnet:3.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true 

COPY --from=build /app /home/site/wwwroot
ADD ValidationLibrary.AzureFunctions/dev_secrets/host.json /azure-functions-host/Secrets/host.json
