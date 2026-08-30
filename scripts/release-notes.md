Cab Radio · 驾驶室电台 v0.3.0
=============================

**把本机音频开进你的驾驶室** — Stream your local audio into your truck cab.

首个可日常使用的发布：电脑上放什么（播放器/浏览器/任何能出声的程序），
ATS/ETS2 游戏电台里就实时播什么。

First usable release: whatever plays on your PC streams live into the
ATS/ETS2 in-game radio.

## 特性 / Features

- 托盘程序，自动跟随游戏启停采集（省电）
- WASAPI 采集 → LAME MP3 编码（96-320kbps 可切）→ 本机 HTTP 电台
- 静音兜底：暂停/切歌不断流
- 自动注入电台条目：游戏重写 live_streams.sii 后自愈
- 设备热插拔自动重连；开机自启（可选）
- 中英双语 UI / Bilingual UI

## 安装 / Setup

1. 安装 [VB-CABLE](https://vb-audio.com/Cable/)（装完重启）
2. 运行 `CabRadio.exe`（托盘程序）
3. 把播放器输出指到 `CABLE Input`（没有设备选项的应用用 Windows 设置 → 声音 → 音量合成器强制路由）
4. 进游戏 → 电台 → 选 `Cab Radio`

> mod（`qq-music-radio.scs`）可选：放进 `Documents\<游戏>\mod\` 并在 mod 管理器启用。
> 即使用不到 mod，Companion 也会自动把电台条目写进游戏列表。

## 说明 / Notes

- 全程本机 127.0.0.1，无需联网
- 状态页：http://127.0.0.1:17890/
- 日志：`%APPDATA%\CabRadio\companion.log`
- MIT 开源：https://github.com/VB085/cab-radio-ets2-ats
