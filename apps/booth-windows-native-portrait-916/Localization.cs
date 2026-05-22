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
            "app_title" => zh ? "Photobooth 竖版 9:16" : "Photobooth Portrait 9:16",
            "header_title" => zh ? "Photobooth 竖版 9:16" : "Photobooth Portrait 9:16",
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
            "open_final" => zh ? "打开成片" : "Open Final",
            "print_final" => zh ? "打印成片" : "Print Final",
            "upload_to_website" => zh ? "上传到网站" : "Upload to Website",
            "restart_session" => zh ? "重新开始" : "Restart",
            "language" => zh ? "语言" : "Language",
            "orientation_toggle" => zh ? "切换横竖屏" : "Toggle Orientation",
            "language_chinese" => zh ? "中文" : "Chinese",
            "language_english" => zh ? "英文" : "English",
            "stage_title" => zh ? "实时 Booth 画面" : "Live Booth Stage",
            "stage_subtitle" => zh ? "现在会同时跟踪 Windows USB 设备状态和相机 bridge 状态，拔线时会安全清空预览。" : "The booth now tracks both Windows USB device state and bridge state, and safely clears preview on disconnect.",
            "waiting_live_view" => zh ? "等待有线相机实时画面" : "Waiting for tethered live view",
            "placeholder_hint" => zh ? "如果一直没有实时画面，先看右侧设备诊断。常见原因是 bridge 没启动，或者相机虽然被 Windows 识别但还没被 bridge 接管。" : "If live view does not appear, check the device diagnostics. Common causes are a bridge that is not running, or a camera that Windows sees but the bridge has not taken over yet.",
            "stage_upload_photo" => zh ? "上传到主预览" : "Upload to Stage",
            "stage_fullscreen" => zh ? "全屏查看" : "Fullscreen Preview",
            "stage_clear_preview" => zh ? "清空测试预览" : "Clear Test Preview",
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
            "beauty_level" => zh ? "美颜级别" : "Beauty Level",
            "beauty_off" => zh ? "关闭" : "Off",
            "beauty_low" => zh ? "低" : "Low",
            "beauty_medium" => zh ? "中" : "Medium",
            "beauty_high" => zh ? "高" : "High",
            "frame_overlay" => zh ? "边框 / Overlay" : "Frame / Overlay",
            "source_mode" => zh ? "来源模式" : "Source Mode",
            "source_mode_camera" => zh ? "相机拍摄" : "Camera",
            "source_mode_upload" => zh ? "本地上传" : "Upload",
            "source_mode_gallery" => zh ? "图库选图" : "Gallery",
            "upload_photos" => zh ? "上传照片" : "Upload Photos",
            "use_gallery_selection" => zh ? "使用图库选中照片" : "Use Gallery Selection",
            "import_mode_title" => zh ? "无相机测试 / 导入照片" : "No-Camera Test / Import Photos",
            "import_mode_hint" => zh ? "没有连接相机时，切到“本地上传”或“图库选图”，再用这里的按钮导入照片测试特效和排版。" : "When no camera is connected, switch to Upload or Gallery mode and use these buttons to test effects and layouts with photos.",
            "import_drop_hint" => zh ? "如果选图窗口没有弹出，也可以直接把本地照片拖到这个区域导入。" : "If the file picker does not appear, drag local photos into this area to import them.",
            "upload_dialog_failed" => zh ? "无法打开本地选图窗口，请重试。" : "Unable to open the local file picker. Please try again.",
            "upload_no_valid_files" => zh ? "没有找到可导入的有效图片文件。" : "No valid image files were selected for import.",
            "upload_import_started" => zh ? "正在导入本地照片..." : "Importing local photos...",
            "upload_opening_dialog" => zh ? "正在打开本地选图窗口..." : "Opening local file picker...",
            "drop_import_started" => zh ? "正在导入拖入的照片..." : "Importing dropped photos...",
            "drop_no_valid_files" => zh ? "拖入内容里没有可识别的图片文件。" : "No valid image files were found in the dropped content.",
            "effect_preset" => zh ? "风格 / 滤镜" : "Effect Preset",
            "preview_sticker" => zh ? "互动贴纸" : "Interactive Sticker",
            "preview_mask_mode" => zh ? "局部遮罩" : "Mask Mode",
            "sticker_none" => zh ? "无贴纸" : "None",
            "sticker_dog_ears" => zh ? "狗耳朵" : "Dog Ears",
            "sticker_party_hat" => zh ? "派对帽" : "Party Hat",
            "sticker_hearts" => zh ? "爱心" : "Hearts",
            "mask_none" => zh ? "无遮罩" : "None",
            "mask_left_half" => zh ? "左半区域" : "Left Half",
            "mask_right_half" => zh ? "右半区域" : "Right Half",
            "mask_center_spotlight" => zh ? "中心聚焦" : "Center Spotlight",
            "assignment_mode" => zh ? "排版填充" : "Assignment Mode",
            "assignment_auto" => zh ? "自动顺序填充" : "Auto Fill",
            "assignment_manual" => zh ? "手动点选填格" : "Manual Assign",
            "photo_editor_title" => zh ? "照片编辑器" : "Photo Editor",
            "photo_editor_hint" => zh ? "可替换/删除照片，或调节缩放、旋转、偏移并实时查看排版效果。" : "Replace/delete photos and adjust scale, rotation, and offset with live layout preview.",
            "editable_photo" => zh ? "编辑目标照片" : "Editable Photo",
            "replace_photo" => zh ? "替换照片" : "Replace Photo",
            "delete_photo" => zh ? "删除照片" : "Delete Photo",
            "assign_slot" => zh ? "放入格子" : "Assign Slot",
            "move_prev_slot" => zh ? "前移一格" : "Move Prev",
            "move_next_slot" => zh ? "后移一格" : "Move Next",
            "reset_photo_edit" => zh ? "重置缩放/旋转" : "Reset Transform",
            "scale_label" => zh ? "缩放" : "Scale",
            "rotation_label" => zh ? "旋转" : "Rotation",
            "offset_x_label" => zh ? "水平偏移" : "Horizontal Offset",
            "offset_y_label" => zh ? "垂直偏移" : "Vertical Offset",
            "selected_sources" => zh ? "已选来源" : "Selected Sources",
            "select_slot_hint" => zh ? "先点模板格子，再点右侧图库中的照片进行手动填充。" : "Click a template slot first, then click a gallery photo to assign it manually.",
            "source_import_ready" => zh ? "可直接用上传或图库照片测试模板效果。" : "You can test templates directly with uploaded or gallery images.",
            "preview_test_ready" => zh ? "测试预览已更新，可直接查看滤镜、贴纸和遮罩效果。" : "Test preview updated. You can now inspect filters, stickers, and mask effects.",
            "preview_test_cleared" => zh ? "已清空上传测试预览，恢复等待实时画面。" : "Test preview cleared. Waiting for live view again.",
            "fullscreen_preview_title" => zh ? "全屏效果预览" : "Fullscreen Effect Preview",
            "effect_clean_modern" => zh ? "清爽现代" : "Clean Modern",
            "effect_bw_classic_strip" => zh ? "经典黑白条" : "B&W Classic Strip",
            "effect_vintage_soft" => zh ? "复古柔和" : "Vintage Soft",
            "effect_scrapbook_cute" => zh ? "可爱手账" : "Scrapbook Cute",
            "effect_overlay_only" => zh ? "仅装饰层" : "Overlay Only",
            "session_progress" => zh ? "Session 进度" : "Session Progress",
            "detected_devices" => zh ? "已检测设备" : "Detected Devices",
            "camera_parameters" => zh ? "相机参数" : "Camera Parameters",
            "website_upload_title" => zh ? "网站上传" : "Website Upload",
            "website_upload_hint" => zh ? "可将当前成片上传到网站，并设置公开/私密访问。" : "Upload the current final output to the website with public/private access.",
            "website_base_url" => zh ? "网站地址" : "Website Base URL",
            "website_code" => zh ? "4位访问代码" : "4-Digit Access Code",
            "website_event_name" => zh ? "活动名称（可选）" : "Event Name (Optional)",
            "website_format" => zh ? "上传格式" : "Upload Format",
            "website_visibility" => zh ? "隐私设置" : "Visibility",
            "website_public" => zh ? "公开（Public）" : "Public",
            "website_private" => zh ? "私密（Private）" : "Private",
            "website_private_password" => zh ? "私密访问密码（4位，可留空自动生成）" : "Private Password (4 digits, leave empty to auto-generate)",
            "website_upload_starting" => zh ? "正在上传到网站..." : "Uploading to website...",
            "website_upload_need_code" => zh ? "请先输入4位数字访问代码。" : "Please enter a valid 4-digit access code first.",
            "website_upload_need_password" => zh ? "私密上传需要4位数字密码（或留空自动生成）。" : "Private upload needs a 4-digit password (or leave empty to auto-generate).",
            "website_upload_need_image" => zh ? "当前没有可上传的成片或预览图。" : "No final output or preview image is available for upload.",
            "website_upload_success" => zh ? "上传成功：" : "Upload succeeded: ",
            "website_upload_failed" => zh ? "上传失败：" : "Upload failed: ",
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
            "grid-4x6-2x3" => zh ? "4x6 六宫格" : "4x6 Portrait Grid 2x3",
            "grid-4x6-2x2" => zh ? "4x6 四宫格" : "4x6 Portrait Grid 2x2",
            "strip-1x4" => zh ? "1x4 经典长条" : "1x4 Strip",
            "grid-4x6-2x4" => zh ? "4x6 八宫格" : "4x6 Grid 2x4",
            "collage-editorial-feature" => zh ? "杂志风拼贴" : "Editorial Collage",
            "strip-scrapbook-cute" => zh ? "手账可爱长条" : "Scrapbook Cute Strip",
            _ => template.Name
        };
    }

    public static string GetTemplateDescription(string languageCode, NativeTemplateRecord template)
    {
        var zh = NormalizeLanguage(languageCode) == "zh-CN";
        return template.Id switch
        {
            "grid-4x6-2x3" => zh ? "6 张照片拼成 4x6 竖版输出。" : "Six-photo 4x6 print with two columns and three rows.",
            "grid-4x6-2x2" => zh ? "4 张照片拼成 4x6 竖版输出。" : "Four-photo 4x6 layout with balanced spacing.",
            "strip-1x4" => zh ? "4 张照片的经典纵向照片条。" : "Classic four-photo vertical strip.",
            "grid-4x6-2x4" => zh ? "8 张照片拼成 4x6 竖版输出。" : "Eight-photo 4x6 layout with two columns and four rows.",
            "collage-editorial-feature" => zh ? "不等宽布局的活动拼贴成片。" : "Event-style editorial collage with uneven image blocks.",
            "strip-scrapbook-cute" => zh ? "带贴纸和撕边装饰的手账风照片条。" : "Decorative scrapbook strip with playful stickers and torn-paper mood.",
            _ => template.Description
        };
    }

    public static string GetEffectPresetName(string languageCode, NativeEffectPresetRecord preset)
    {
        var zh = NormalizeLanguage(languageCode) == "zh-CN";
        return preset.Id switch
        {
            "clean-modern" => Get(languageCode, "effect_clean_modern"),
            "bw-classic-strip" => Get(languageCode, "effect_bw_classic_strip"),
            "vintage-soft" => Get(languageCode, "effect_vintage_soft"),
            "scrapbook-cute" => Get(languageCode, "effect_scrapbook_cute"),
            "overlay-only" => Get(languageCode, "effect_overlay_only"),
            _ => zh ? preset.Name : preset.Name
        };
    }

    public static string GetCaptureMode(string languageCode, string mode) => mode switch
    {
        "single" => Get(languageCode, "mode_single"),
        "burst" => Get(languageCode, "mode_burst"),
        _ => Get(languageCode, "mode_multi")
    };
}
