using System.ComponentModel.DataAnnotations;

namespace OpenAi.Models.Settings;

public class OpenAISetting
{
    public const string SectionName = nameof(OpenAISetting);
    [Required, Url]
    public string Endpoint { get; set; } = string.Empty;
    [Required]
    public string DeploymentName { get; set; } = string.Empty;
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}