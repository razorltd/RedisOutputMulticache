namespace RedisOutputMulticache.MVC5
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Runtime.Caching;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Web.Caching;
    using StackExchange.Redis;

    public class RedisOutputMulticacheProvider : OutputCacheProvider
    {
        private static readonly MemoryCache memoryCache = MemoryCache.Default;

        private ConnectionMultiplexer redisConnection;
        private NameValueCollection nameValueCollection;
        private int databaseId;
        private string applicationName;
        private TimeSpan memoryTimeout = TimeSpan.FromMinutes( 1 );

        public override void Initialize( string name, NameValueCollection config )
        {
            nameValueCollection = config;
            base.Initialize( name, config );

            int.TryParse( nameValueCollection[ "databaseId" ], out databaseId );
            TimeSpan.TryParse( nameValueCollection[ "memoryTimeout" ], out memoryTimeout );
            applicationName = nameValueCollection[ "applicationName" ];
            redisConnection = new Lazy<ConnectionMultiplexer>( GetRedisConnection ).Value;
        }

        public override object Get( string key )
        {
            string transformedKey = TransformedKey( key );

            if ( memoryCache.Contains( transformedKey ) )
                return memoryCache[ transformedKey ];

            var redisValue = GetFromRedisCache( transformedKey );

            if ( redisValue == null )
                return null;

            AddToMemoryCache( transformedKey, redisValue, DateTime.UtcNow.Add( memoryTimeout ) );
            return redisValue;
        }

        // Add to the cache only if it does not exist and isn't expired
        public override object Add( string key, object entry, DateTime utcExpiry )
        {
            string transformedKey = TransformedKey( key );
            utcExpiry = DateTime.SpecifyKind( utcExpiry, DateTimeKind.Utc );

            if ( memoryCache.Contains( transformedKey ) )
                return memoryCache[ transformedKey ];

            var redisValue = GetFromRedisCache( transformedKey );
            if ( redisValue != null )
            {
                return redisValue;
            }

            AddToMemoryCache( transformedKey, entry, DateTime.UtcNow.Add( memoryTimeout ) );
            AddToRedisCache( transformedKey, entry, utcExpiry );
            return entry;
        }

        // Set the value in the cache, regardless of whether it exists or not
        public override void Set( string key, object entry, DateTime utcExpiry )
        {
            string transformedKey = TransformedKey( key );
            utcExpiry = DateTime.SpecifyKind( utcExpiry, DateTimeKind.Utc );

            AddToMemoryCache( transformedKey, entry, DateTime.UtcNow.Add( memoryTimeout ) );
            AddToRedisCache( transformedKey, entry, utcExpiry );
        }

        public override void Remove( string key )
        {
            string transformedKey = TransformedKey( key );
            memoryCache.Remove( transformedKey );
            var redisDatabase = redisConnection.GetDatabase( databaseId );
            redisDatabase.KeyDelete( transformedKey, CommandFlags.FireAndForget );
        }

        private ConnectionMultiplexer GetRedisConnection()
        {
            string connectionString = ConfigurationManager.AppSettings[ nameValueCollection.Get( "connectionString" ) ];
            return ConnectionMultiplexer.Connect( connectionString );
        }

        private string TransformedKey( string key )
        {
            return applicationName.IsNullOrEmpty()
                ? key
                : $"{applicationName}_{key}";
        }

        private void AddToMemoryCache( string key, object entry, DateTime utcExpiry )
        {
            var cacheItemPolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = utcExpiry
            };

            memoryCache.Add( key, entry, cacheItemPolicy );
        }

        private object GetFromRedisCache( string key )
        {
            var redisDatabase = redisConnection.GetDatabase( databaseId );

            if ( !redisDatabase.KeyExists( key ) )
                return null;

            var bytes = (byte[]) redisDatabase.StringGet( key );

            if ( bytes == null )
                return null;

            using ( var stream = new MemoryStream( bytes ) )
            {
                return new BinaryFormatter().Deserialize( stream );
            }
        }

        private void AddToRedisCache( string key, object entry, DateTime utcExpiry )
        {
            using ( var stream = new MemoryStream() )
            {
                new BinaryFormatter().Serialize( stream, entry );
                var bytes = stream.ToArray();

                var redisDatabase = redisConnection.GetDatabase( databaseId );

                var timeSpan = utcExpiry - DateTime.UtcNow;
                redisDatabase.StringSet( key, bytes, timeSpan );
            }
        }
    }
}
