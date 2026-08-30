# Cab Radio · 驾驶室电台

> **把本机音频开进你的驾驶室** — Stream your local audio into your truck cab.

[English](#english) | [中文](#中文)

---

## English

Stream **any audio from your PC** (music players, browsers, anything that makes sound) into the in-game radio of **American Truck Simulator** and **Euro Truck Simulator 2**.

Select the "Cab Radio" station in-game — whatever plays on your PC follows in real time: track changes, pauses, everything.

### How it works

- **Zero game modification** — no DLL injection, no game file changes. The game just plays a normal radio stream served from your own machine (`http://127.0.0.1:17890/stream.mp3`).
- **No dependence on any player's internal API** — the pipeline is: your audio app → virtual audio device (VB-CABLE) → Cab Radio Companion (capture + MP3 encode + local HTTP) → game radio.

```
Workshop Mod (ATS / ETS2)  →  one station entry pointing to 127.0.0.1:17890
Companion (tray app)       →  WASAPI capture, LAME MP3 96-320kbps, local HTTP stream
Virtual audio device       →  VB-CABLE (free, donationware)
Your audio apps            →  any source: players, browsers, ...
```

### Setup (first time)

1. Subscribe to the Workshop mod (or run `scripts/add-stream.ps1` / enable the bundled local mod)
2. Install [VB-CABLE](https://vb-audio.com/Cable/)
3. Route your audio app's output to `CABLE Input` (players with a device option: pick it in settings; apps without one: Windows Settings → Sound → Volume mixer → force-route the app)
4. Run the Companion (tray icon; optional "Run at startup")

### Daily use

Play something on your PC → start the game → pick **Cab Radio** → drive. The Companion follows the game automatically (starts/stops capture with the game, streams silence while paused so the radio never drops).

### FAQ

- **Latency?** ~2-3 s — irrelevant for background music.
- **Internet needed?** No — everything runs on 127.0.0.1.
- **Paused / no audio?** The Companion keeps streaming silence; the radio stays connected.
- **Quality?** 128 kbps default, switchable 96-320 in the tray menu; 192 is a good in-cab setting.

### License

MIT. See [LICENSE](LICENSE) for third-party notices. VB-CABLE is donationware by VB-Audio, downloaded separately by the user. This project is not affiliated with SCS Software or any audio platform.

---

## 中文

把电脑上的任意音频（音乐播放器、浏览器、任何能出声的程序）实时接入《美国卡车模拟》(ATS) /《欧洲卡车模拟 2》(ETS2) 的游戏电台。

游戏里选台 → 「Cab Radio」，电脑上换歌、暂停、切歌，游戏里实时跟随。

### 工作原理

- **零游戏改造**：不注入 DLL、不改游戏文件——游戏只是播放一个由你本机提供的普通电台流（`http://127.0.0.1:17890/stream.mp3`）
- **不依赖任何播放器的内部 API/协议**：链路是「音频应用 → 虚拟声卡（VB-CABLE）→ Cab Radio Companion（采集 + MP3 编码 + 本地 HTTP）→ 游戏电台」，播放器怎么改版都不受影响

```
创意工坊 mod（ATS / ETS2）→ 只提供一条指向 127.0.0.1:17890 的电台定义
Companion（托盘程序）     → WASAPI 采集、LAME 编码 96-320kbps、本地 HTTP 推流
虚拟声卡                 → VB-CABLE（免费，donationware）
任意音频应用              → 播放器 / 浏览器 / …
```

### 首次安装

1. 订阅创意工坊 mod（或运行 `scripts/add-stream.ps1` / 启用附带的本地 mod）
2. 安装 [VB-CABLE](https://vb-audio.com/Cable/)
3. 把播放器输出指到 `CABLE Input`（播放器自带设备选项的直接选；没有选项的应用用 Windows 设置 → 声音 → 音量合成器强制路由；教程见 [docs/mvp-zero-code.md](docs/mvp-zero-code.md)，以 QQ 音乐为例）
4. 运行 Companion（托盘程序，可勾选「开机自启」）

### 日常使用

电脑上放歌 → 进游戏 → 选 **Cab Radio** → 开车。Companion 自动跟随游戏启停采集；暂停时持续推静音，电台永不断流。

### FAQ

- **延迟？** 约 2-3 秒，背景音乐场景无关紧要
- **需要联网？** 不需要，全程本机 127.0.0.1
- **暂停/没声音？** Companion 持续推静音，游戏电台保持连接
- **音质？** 默认 128kbps，托盘菜单可切 96-320，游戏内推荐 192

### 许可

MIT，第三方声明见 [LICENSE](LICENSE)。VB-CABLE 为 VB-Audio 的 donationware，由用户自行下载。本项目与 SCS Software 及各音频平台无关联。

---

## 文档索引 / Docs

- [docs/architecture.md](docs/architecture.md) — 架构与技术选型（中文）
- [docs/roadmap.md](docs/roadmap.md) — 开发路线图（中文）
- [docs/mvp-zero-code.md](docs/mvp-zero-code.md) — 零代码验证指南（中文）
- [docs/workshop-mod.md](docs/workshop-mod.md) — 创意工坊流程（中文）
