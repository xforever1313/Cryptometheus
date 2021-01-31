# Run this from the dist/linux-x64 or dist/linux-arm directory.

FROM mcr.microsoft.com/dotnet/aspnet:3.1

# Create user who does not have root access.
RUN useradd -d /cryptometheus -m containeruser
RUN chown -R containeruser:containeruser /cryptometheus

USER containeruser
RUN mkdir /cryptometheus/bin/
COPY ./ /cryptometheus/bin

# Sanity Check
RUN ["/cryptometheus/bin/Cryptometheus", "--version"]

ENTRYPOINT ["/cryptometheus/bin/Cryptometheus"]
