# Platform Roles

- your team has an "app" (an "app" is a repo, twelve factor app, all that jazz.)
- Your app might have many "pieces" - one or more API services, maybe a frontend (Angular, react, etc.)
- Having things together in one repo is the BEST because they can't get out of sync - they get tested together, deployed together etc.


"Defaults for any service." - ServiceDefaults

- Security Configuration

- Observability "a pillar of DevOps" Emit Telemetry (OTEL: Logs, Tracing, Events)
- Turn the name of a service into an address (Sevice Location) = appconfig.json, app.ENVIRONMENT.json, "secrets", ENVIRONMENT VARIABLES
    - "https+http:/servicename" - looks for environment variables in a particular format
- Decide what to do when a call to another service fails (SRE)



Venues is sort of a Jeff invention - but man it really helps. Most viral programming thing I've ever created.

