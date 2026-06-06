using System.Text.Json;
using System.Text.Json.Serialization;

namespace AliceShop.Extensions
{
    public static class SessionExtensions
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value, SerializerOptions));
        }

        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value, SerializerOptions);
        }
    }
}
