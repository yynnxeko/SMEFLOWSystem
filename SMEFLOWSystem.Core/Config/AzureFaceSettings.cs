namespace SMEFLOWSystem.Core.Config;

public class AzureFaceSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public double ConfidenceThreshold { get; set; } = 0.75;
}
