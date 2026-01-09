# Hướng dẫn cài đặt Agora Voice SDK

## Bước 1: Download Agora SDK

1. Truy cập: https://docs.agora.io/en/sdks?platform=windows
2. Download **Agora Voice SDK for Windows**
3. Giải nén file tải về

## Bước 2: Copy DLL vào project

Từ folder SDK đã giải nén:

```
Agora_SDK/
├── sdk/
│   ├── x86/
│   │   └── agora_rtc_sdk.dll
│   └── x86_64/
│       └── agora_rtc_sdk.dll
```

Copy các file sau vào `3Mess/libs/`:

**Cho x64 (khuyến nghị):**
- `sdk/x86_64/agora_rtc_sdk.dll`
- `sdk/x86_64/agora_audio_process.dll` (nếu có)

**Cấu trúc thư mục sau khi copy:**
```
3Mess/
├── libs/
│   ├── agora_rtc_sdk.dll
│   └── agora_audio_process.dll (optional)
```

## Bước 3: Cập nhật 3Mess.csproj

Mở file `3Mess/3Mess.csproj` và thêm vào:

```xml
<ItemGroup>
  <!-- Copy Agora DLLs to output directory -->
  <None Include="libs\*.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Bước 4: Cấu hình Agora App ID

1. Đăng nhập https://console.agora.io
2. Tạo project mới (chọn "Secured mode: APP ID + Token" cho production, hoặc "Testing mode: APP ID" để test)
3. Copy **App ID**
4. Mở file `MessagingApp.Core/Config/AgoraConfig.cs`
5. Thay thế:
   ```csharp
   public const string AppId = "YOUR_AGORA_APP_ID_HERE";
   ```
   Thành:
   ```csharp
   public const string AppId = "a1b2c3d4e5f6g7h8i9j0"; // App ID thực của bạn
   ```

## Bước 5: Cài đặt Agora NuGet Package (nếu có)

Thử cài package C# wrapper (có thể không tồn tại):

```bash
cd 3Mess
dotnet add package Agora.Windows.SDK
```

**Nếu package không tồn tại**, bạn cần:

### Option A: Sử dụng P/Invoke (Native DLL calls)

Tạo file `3Mess/Agora/AgoraNativeWrapper.cs`:

```csharp
using System;
using System.Runtime.InteropServices;

namespace ThreeMess.Agora
{
    public static class AgoraRtc
    {
        private const string DLL_NAME = "agora_rtc_sdk.dll";

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr createAgoraRtcEngine();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int initialize(IntPtr engine, string appId);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int joinChannel(IntPtr engine, string token, string channelName, string userId, uint uid);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int leaveChannel(IntPtr engine);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int enableAudio(IntPtr engine);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int disableVideo(IntPtr engine);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int muteLocalAudioStream(IntPtr engine, bool mute);
    }
}
```

### Option B: Download Agora C# SDK từ GitHub

```bash
git clone https://github.com/AgoraIO-Community/Agora-C_Sharp-SDK.git
```

Copy source code vào project.

## Bước 6: Test kết nối

1. Build project:
   ```bash
   dotnet build
   ```

2. Run application:
   ```bash
   dotnet run
   ```

3. Khi gọi voice, check console logs:
   - `"Agora engine initialized successfully"` → Thành công
   - `"Joining channel: {callId}"` → Đang join
   - `"Joined channel successfully"` → Đã kết nối

## Troubleshooting

### DLL not found error
- Đảm bảo DLL đã được copy vào `bin/Debug/net8.0-windows/`
- Check platform target: x64 hoặc x86
- Thử set `<PlatformTarget>x64</PlatformTarget>` trong .csproj

### Initialization failed
- Kiểm tra App ID đã đúng chưa
- Kiểm tra internet connection
- Check Agora console có bật project chưa

### No audio
- Kiểm tra microphone permissions (Windows Settings)
- Check `enableAudio()` đã được gọi
- Verify không bị mute (`IsMicrophoneMuted = false`)

## Lưu ý bảo mật

**Không commit App ID/Certificate vào Git:**

Thêm vào `.gitignore`:
```
**/AgoraConfig.cs
```

Tạo file `AgoraConfig.Example.cs` để share template:
```csharp
public const string AppId = "PASTE_YOUR_APP_ID_HERE";
```

## Tham khảo

- Agora Docs: https://docs.agora.io/en/
- API Reference: https://api-ref.agora.io/en/voice-sdk/windows/4.x/
- Sample Code: https://github.com/AgoraIO/API-Examples
