using OpenAi.Api.Mappers;

namespace OpenAi.Api.Configurations
{
    public static class AutoMapperConfiguration
    {
        public static void AddMappers(this IServiceCollection services)
        {
            services.AddAutoMapper(new Type[] { typeof(ContractMapping) });
        }
    }
}
