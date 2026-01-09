namespace MessagingApp.Config;

/// <summary>
/// Agora RTC configuration for voice calling.
/// Get your App ID and App Certificate from https://console.agora.io
/// </summary>
public static class AgoraConfig
{
    /// <summary>
    /// Your Agora App ID. Get it from Agora Console.
    /// Example: "a1b2c3d4e5f6g7h8i9j0"
    /// </summary>
    public const string AppId = "YOUR_AGORA_APP_ID_HERE";

    /// <summary>
    /// Your Agora App Certificate (optional but recommended for production).
    /// Required for generating token on server side.
    /// </summary>
    public const string AppCertificate = "YOUR_AGORA_APP_CERTIFICATE_HERE";

    /// <summary>
    /// Log file path for Agora SDK logs (optional).
    /// </summary>
    public static string LogFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "3Mess",
        "Logs",
        "agora_sdk.log"
    );
}
