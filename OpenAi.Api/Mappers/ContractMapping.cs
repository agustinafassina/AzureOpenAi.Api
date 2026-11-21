using Riok.Mapperly.Abstractions;

namespace OpenAi.Api.Mappers
{
    /// <summary>
    /// Mappings between API contracts and service DTOs.
    /// Mapperly generates the implementation at compile time: declare
    /// <c>public static partial TTarget Method(TSource source);</c> and it fills in the body.
    /// </summary>
    [Mapper]
    public static partial class ContractMapping
    {
    }
}
