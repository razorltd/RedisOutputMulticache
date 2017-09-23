# RedisOutputMulticacheProvider
ASP.NET MVC5 Redis output cache provider with fast expiring memory cache buffer
This package is loosely based on https://github.com/Azure/aspnet-redis-providers, with the addition of a fast-expiring memory cache to stop your redis instance being hit needlessly.

## How to use
Simply reference the nuget package in your MVC5 project, and amend your web.config accordingly

``` xml
<system.web>
    ...
    <caching>
        <outputCache defaultProvider="RedisOutputMulticache">
            <providers>
                <add name="RedisOutputMulticache" 
                     type="RedisOutputMulticache.MVC5.RedisOutputMulticacheProvider, RedisOutputMulticache.MVC5" 
                     connectionString="RedisConnectionString" 
                     applicationName="MyWebSite" 
                     databaseId="1" 
                     memoryTimeout="00:01:00"/>
            </providers>
        </outputCache>
    </caching>
</system.web>
```