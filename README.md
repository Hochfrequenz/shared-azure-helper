# shared-azure-helper

This repository is used to build the nuget package [Hochfrequenz.sharedAzureHelper](https://www.nuget.org/packages/Hochfrequenz.sharedAzureHelper/0.0.11). It is used in various of our projects, that somehow rely on Hochfrequenz services written in C# and deployed to Azure. There is a build pipeline set up that can be accessed in the [commit overview of the master branch](https://github.com/Hochfrequenz/shared-azure-helper/commits/master).

## Purpose
### LookupHelper 
The LookupHelper is a **wrapper** around the lookupService (see [Hochfrequenz/energy-service-hub/lookupService](https://github.com/Hochfrequenz/energy-service-hub/tree/master/lookupService). It's main purpose is to spare programmers of other projects to directly deal with manually instantiate http clients, setting header values, query parameters and so on to access the lookup service.

### AuthenticationHelper
The AuthenticationHelper is a central component used to check if http requests are authenticated using a [JWT](https://en.wikipedia.org/wiki/JSON_Web_Token) that is stored in a HTTP header of the request.
