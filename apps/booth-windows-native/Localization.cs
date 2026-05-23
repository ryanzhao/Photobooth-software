namespace Photobooth.BoothNative;

public static class Localization
{
    public static string NormalizeLanguage(string? languageCode) =>
        string.Equals(languageCode, "en-US", StringComparison.OrdinalIgnoreCase) ? "en-US" : "zh-CN";

    public static string Get(string languageCode, string key)
    {
        var zh = NormalizeLanguage(languageCode) == "zh-CN";
        return key switch
        {
            "app_title" => zh ? "Photobooth 原生 Booth" : "Photobooth Native Booth",
            "header_title" => zh ? "Photobooth 原生 Booth" : "Photobooth Native Booth",
            "header_subtitle" => zh ? "自动监听 Windows 相机插拔，并在 bridge 可用时自动恢复有线预览。" : "Automatically watches Windows camera attach and detach events, and restores tethered preview when the bridge is available.",
            "refresh_devices" => zh ? "刷新设备" : "Refresh Devices",
            "new_session" => zh ? "新建 Session" : "New Session",
            "launch_digicamcontrol" => zh ? "启动 digiCamControl" : "Launch digiCamControl",
            "launch_digicamcontrol_done" => zh ? "digiCamControl 已启动，并已开始自动连接相机。" : "digiCamControl is running and camera auto-connect is in progress.",
            "launch_digicamcontrol_failed" => zh ? "无法启动 digiCamControl，或启动后没有建立本地连接。" : "Unable to launch digiCamControl, or the local bridge did not come online.",
            "start_live_manual" => zh ? "手动启动 Live" : "Start Live",
            "start_live_auto_status" => zh ? "正在自动初始化 Live bridge..." : "Automatically initializing live bridge...",
            "start_live_manual_status" => zh ? "正在手动初始化 Live bridge..." : "Manually initializing live bridge...",
            "start_live_success" => zh ? "Live bridge 已就绪，正在更新实时画面。" : "Live bridge is ready and the preview is updating.",
            "start_live_failed" => zh ? "Live bridge 初始化失败，请手动重试或检查 digiCamControl Web 服务。" : "Live bridge initialization failed. Retry manually or check the digiCamControl web service.",
            "capture_now" => zh ? "立即拍照" : "Capture Now",
            "auto_focus" => zh ? "自动对焦" : "Auto Focus",
            "open_session_folder" => zh ? "打开 Session 文件夹" : "Open Session Folder",
            "language" => zh ? "语言" : "Language",
            "language_chinese" => zh ? "中文" : "Chinese",
            "language_english" => zh ? "英文" : "English",
            "stage_title" => zh ? "实时 Booth 画面" : "Live Booth Stage",
            "stage_subtitle" => zh ? "现在会同时跟踪 Windows USB 设备状态和相机 bridge 状态，拔线时会安全清空预览。" : "The booth now tracks both Windows USB device state and bridge state, and safely clears preview on disconnect.",
            "waiting_live_view" => zh ? "等待有线相机实时画面" : "Waiting for tethered live view",
            "placeholder_hint" => zh ? "如果一直没有实时画面，先看右侧设备诊断。常见原因是 bridge 没启动，或者相机虽然被 Windows 识别但还没被 bridge 接管。" : "If live view does not appear, check the device diagnostics. Common causes are a bridge that is not running, or a camera that Windows sees but the bridge has not taken over yet.",
            "stat_mode" => zh ? "模式" : "Mode",
            "stat_shots" => zh ? "张数" : "Shots",
            "stat_countdown" => zh ? "倒计时" : "Countdown",
            "stat_templates" => zh ? "模板" : "Templates",
            "recent_sessions" => zh ? "最近 Session" : "Recent Sessions",
            "photo_gallery" => zh ? "照片图库" : "Photo Gallery",
            "template_pack" => zh ? "模板包" : "Template Pack",
            "controls_title" => zh ? "Windows 原生 Booth 控制台" : "Windows Native Booth Controls",
            "controls_subtitle" => zh ? "拍照、自动对焦、参数调节和本地保存现在都直接接到了 digiCamControl 的有线工作流。" : "Capture, auto focus, camera parameters, and local saving now run directly through digiCamControl's tethered workflow.",
            "capture_mode" => zh ? "拍摄模式" : "Capture Mode",
            "shot_count" => zh ? "拍摄张数" : "Shot Count",
            "countdown_seconds" => zh ? "倒计时秒数" : "Countdown Seconds",
            "detected_devices" => zh ? "已检测设备" : "Detected Devices",
            "camera_parameters" => zh ? "相机参数" : "Camera Parameters",
            "active_session" => zh ? "当前 Session" : "Active Session",
            "gallery_empty" => zh ? "当前还没有照片。拍一张后这里会自动出现。" : "No photos yet. Captured images will appear here automatically.",
            "gallery_preview" => zh ? "图库预览" : "Gallery Preview",
            "iso" => zh ? "ISO" : "ISO",
            "shutter_speed" => zh ? "快门速度" : "Shutter Speed",
            "aperture" => zh ? "光圈" : "Aperture",
            "white_balance" => zh ? "白平衡" : "White Balance",
            "exposure_comp" => zh ? "曝光补偿" : "Exposure Compensation",
            "checking_bridge" => zh ? "正在检查有线桥接..." : "Checking tethered bridge...",
            "mode_single" => zh ? "单拍" : "Single",
            "mode_multi" => zh ? "多拍" : "Multi",
            "mode_burst" => zh ? "连拍" : "Burst",
            "yes" => zh ? "是" : "Yes",
            "no" => zh ? "否" : "No",
            "transport_usb_tethered" => zh ? "USB 有线联机" : "USB Tethered",
            "transport_tethered" => zh ? "有线联机" : "Tethered",
            "transport_webcam" => zh ? "摄像头" : "Webcam",
            "state_connected" => zh ? "已连接" : "Connected",
            "state_disconnected" => zh ? "已断开" : "Disconnected",
            "state_attention" => zh ? "需检查" : "Attention",
            "state_ready" => zh ? "可用" : "Ready",
            "device_placeholder" => zh ? "桥接 / 相机未就绪" : "Bridge / Camera Not Ready",
            "device_webcam_fallback" => zh ? "摄像头回退模式" : "Webcam Fallback",
            "diagnostics_bridge_online" => zh ? "bridge 已在线，支持远程快门、传图和实时预览。" : "The bridge is online and supports remote trigger, transfer, and live view.",
            "diagnostics_bridge_online_no_devices" => zh ? "bridge 已在线，但当前没有暴露出任何相机。" : "The bridge is online, but no camera is currently exposed.",
            "diagnostics_bridge_online_liveview_unavailable" => zh ? "bridge 已连接到相机，但当前实时预览不可用。通常是相机刚重连，或 live view 还没恢复。" : "The bridge sees the camera, but live view is currently unavailable. This usually means the camera just reconnected or live view has not recovered yet.",
            "diagnostics_bridge_not_running" => zh ? "检测到 bridge 已安装，但本地桥接服务没有运行。" : "The bridge appears to be installed, but the local bridge service is not running.",
            "diagnostics_bridge_missing" => zh ? "没有检测到有线 bridge。请安装或启动 digiCamControl，再重新连接相机。" : "No tethered bridge detected. Install or start digiCamControl, then reconnect the camera.",
            "diagnostics_usb_detected_bridge_missing" => zh ? "Windows 已检测到相机，但没有发现 bridge。要做真正有线联机拍摄，还需要启动 bridge。" : "Windows detects the camera, but no bridge is available yet. A tethered bridge is still required for real wired capture.",
            "diagnostics_usb_detected_bridge_not_running" => zh ? "Windows 已检测到相机，但 bridge 没有运行。请先启动 bridge，软件会自动接管。" : "Windows detects the camera, but the bridge is not running. Start the bridge and the booth will take over automatically.",
            "diagnostics_usb_detected_waiting_bridge_pairing" => zh ? "Windows 已看到相机，bridge 也在线，但还没有把这台相机配对到可控会话。" : "Windows sees the camera and the bridge is online, but the bridge has not paired it into a controllable session yet.",
            "diagnostics_webcam_fallback" => zh ? "这里只是回退方案，不是真正的单反/微单有线联机工作流。" : "Fallback only. This is not the tethered DSLR workflow.",
            "trigger_label" => zh ? "远程快门" : "Trigger",
            "transfer_label" => zh ? "传图" : "Transfer",
            "liveview_label" => zh ? "实时预览" : "LiveView",
            "connection_label" => zh ? "状态" : "State",
            "session_created" => zh ? "已创建本地 Session" : "Created local session",
            "manual_refresh_complete" => zh ? "手动设备刷新完成。" : "Manual device refresh complete.",
            "starting_auto_detection" => zh ? "正在启动自动检测..." : "Starting auto detection...",
            "capture_saved" => zh ? "拍照成功，已自动保存到当前 Session。" : "Capture saved to the active session.",
            "capture_starting" => zh ? "正在触发相机快门..." : "Triggering camera shutter...",
            "capture_waiting_transfer" => zh ? "正在等待相机传图并自动保存..." : "Waiting for camera transfer and auto-save...",
            "capture_in_progress" => zh ? "正在拍照，请稍候。" : "Capture already in progress.",
            "capture_failed" => zh ? "拍照失败。" : "Capture failed.",
            "focus_done" => zh ? "已发送自动对焦命令。" : "Auto focus command sent.",
            "focus_failed" => zh ? "自动对焦失败。" : "Auto focus failed.",
            "parameter_updated" => zh ? "相机参数已更新。" : "Camera parameter updated.",
            "parameter_failed" => zh ? "相机参数更新失败。" : "Camera parameter update failed.",
            _ => key
        };
    }

    public static string GetRuntimeStatus(string languageCode, BoothRuntimeStatus runtime)
    {
        var zh = NormalizeLanguage(languageCode) == "zh-CN";
        return runtime.StatusCode switch
        {
            "bridge_online_devices" => zh ? $"已识别 {runtime.BridgeCameraCount} 台 bridge 可控相机，并且实时预览已恢复。" : $"Detected {runtime.BridgeCameraCount} bridge-controlled camera(s), and live view has recovered.",
            "bridge_online_liveview_unavailable" => Get(languageCode, "diagnostics_bridge_online_liveview_unavailable"),
            "bridge_online_no_devices" => Get(languageCode, "diagnostics_bridge_online_no_devices"),
            "usb_detected_bridge_not_running" => Get(languageCode, "diagnostics_usb_detected_bridge_not_running"),
            "usb_detected_bridge_missing" => Get(languageCode, "diagnostics_usb_detected_bridge_missing"),
            "usb_detected_waiting_bridge_pairing" => Get(languageCode, "diagnostics_usb_detected_waiting_bridge_pairing"),
            "bridge_not_running" => Get(languageCode, "diagnostics_bridge_not_running"),
            _ => Get(languageCode, "diagnostics_bridge_missing")
        };
    }

    public static string GetDeviceName(string languageCode, NativeDeviceRecord device) => device.Kind switch
    {
        "placeholder" => Get(languageCode, "device_placeholder"),
        "webcam-fallback" => Get(languageCode, "device_webcam_fallback"),
        _ => device.Name
    };

    public static string GetDeviceTransport(string languageCode, NativeDeviceRecord device) => device.Transport switch
    {
        "USB Tethered" => Get(languageCode, "transport_usb_tethered"),
        "Tethered" => Get(languageCode, "transport_tethered"),
        "Webcam" => Get(languageCode, "transport_webcam"),
        _ => device.Transport
    };

    public static string GetConnectionState(string languageCode, NativeDeviceRecord device) => device.ConnectionState switch
    {
        "connected" => Get(languageCode, "state_connected"),
        "attention" => Get(languageCode, "state_attention"),
        "ready" => Get(languageCode, "state_ready"),
        _ => Get(languageCode, "state_disconnected")
    };

    public static string GetDeviceDiagnostics(string languageCode, NativeDeviceRecord device) => device.Diagnostics switch
    {
        "bridge_online" => Get(languageCode, "diagnostics_bridge_online"),
        "bridge_online_no_devices" => Get(languageCode, "diagnostics_bridge_online_no_devices"),
        "bridge_online_liveview_unavailable" => Get(languageCode, "diagnostics_bridge_online_liveview_unavailable"),
        "bridge_not_running" => Get(languageCode, "diagnostics_bridge_not_running"),
        "bridge_missing" => Get(languageCode, "diagnostics_bridge_missing"),
        "usb_detected_bridge_missing" => Get(languageCode, "diagnostics_usb_detected_bridge_missing"),
        "usb_detected_bridge_not_running" => Get(languageCode, "diagnostics_usb_detected_bridge_not_running"),
        "usb_detected_waiting_bridge_pairing" => Get(languageCode, "diagnostics_usb_detected_waiting_bridge_pairing"),
        "webcam_fallback" => Get(languageCode, "diagnostics_webcam_fallback"),
        _ => device.Diagnostics
    };

    public static string GetTemplateName(string languageCode, NativeTemplateRecord template)
    {
        var zh = NormalizeLanguage(languageCode) == "zh-CN";
        return template.Id switch
        {
            "single-hero" => zh ? "4x6 单图主视觉" : "4x6 Single Hero",
            "grid-4x6" => zh ? "4x6 四宫格" : "4x6 Grid",
            "strip-2x6" => zh ? "2x6 经典长条" : "2x6 Classic Strip",
            "square-collage" => zh ? "方形拼贴" : "Square Collage",
            "freeform-hero" => zh ? "自由排版主视觉" : "Freeform Event Hero",
            _ => template.Name
        };
    }

    public static string GetTemplateDescription(string languageCode, NativeTemplateRecord template)
    {
        var zh = NormalizeLanguage(languageCode) == "zh-CN";
        return template.Id switch
        {
            "single-hero" => zh ? "适合单张人像主图输出。" : "Single photo layout for portrait hero shots.",
            "grid-4x6" => zh ? "适合活动快拍四图拼版。" : "Four-up event collage.",
            "strip-2x6" => zh ? "经典照片条，底部可放二维码。" : "Classic photo strip with QR footer.",
            "square-collage" => zh ? "适合社交平台分享的方形成片。" : "Social-first square output.",
            "freeform-hero" => zh ? "更自由的高级活动排版。" : "Asymmetric premium event composition.",
            _ => template.Description
        };
    }

    public static string GetCaptureMode(string languageCode, string mode) => mode switch
    {
        "single" => Get(languageCode, "mode_single"),
        "burst" => Get(languageCode, "mode_burst"),
        _ => Get(languageCode, "mode_multi")
    };
}


