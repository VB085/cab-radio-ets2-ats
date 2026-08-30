# 开发路线图

## 里程碑

| 里程碑 | 目标 | 预估 | 交付物 | 验收标准 |
|---|---|---|---|---|
| **M0** 零代码验证 | 不写代码跑通全链路 | 半天 | mvp-zero-code.md + 验证报告 | 游戏内稳定播放 ≥30 分钟；换歌实时跟随；记录暂停/断流表现 |
| **M1** Companion 核心 | 采集→编码→推流→静音 | 1–2 周 | C# 项目（CLI 形态）+ 基本配置 | ✅ 完成（游戏内实测：跟随/静音/2-3s 延迟） |
| **M2** 体验完善 | 托盘、自动化 | 1 周 | 托盘/进程感知/热插拔/状态页/开机启动 | ✅ 完成（2026-08-26 用户实测通过） |
| **M3** 创意工坊 | mod 包 + 上传 | 2–3 天 | mod 内容 + 上传 ATS/ETS2 + sii 直写兜底 | 订阅→mod 管理器启用→电台列表可选 |
| **M4** 发布 | 打包与分发 | 1 周 | 安装器（Inno Setup）、图标、GitHub Release、图文教程 | 另一台干净电脑按教程 10 分钟复现 |

## M0 — 零代码验证（先做这个）

目标：用现成工具（VB-CABLE + VLC 或 Icecast+BUTT）验证游戏端能稳定播放本地流。
详见 [mvp-zero-code.md](mvp-zero-code.md)。

**为什么先做 M0**：所有技术风险（游戏对本地流的支持、虚拟设备链路、缓冲行为）都能在半天内验证或证伪，再决定是否投入写代码。

## M1 — Companion 核心（CLI 形态）

- 项目骨架：.NET 8，`QqMusicRadio.Companion`，单文件发布
- `AudioCapture`：NAudio loopback 采集 `CABLE Output`，float32 → 16-bit/44.1kHz
- `Mp3Encoder`：LAME CBR 128/192，封装 lame_enc.dll（LGPL 合规：动态链接 + 附许可证声明）
- `RingBuffer` + `HttpServer`：多客户端并发推流
- `SilenceSource`：RMS 活动检测 + 预编码静音帧
- 配置：设备名、端口、码率（JSON）
- **验收**：游戏内播放 30 分钟无断流；暂停/换歌不断流；Ctrl+C 干净退出

## M2 — 体验完善

- 托盘图标（设备选择、码率、开机启动、打开状态页、退出）
- `DeviceManager`：MMDevice 枚举 + 热插拔自动重连
- `GameWatcher`：游戏进程启动/退出自动启停
- 状态页 `http://127.0.0.1:17890/`：设备、码率、客户端数、推流状态
- **验收**：拔掉/禁用 CABLE 设备后自动恢复；游戏退出后推流自动暂停

## M3 — 创意工坊

- mod 内容与打包，详见 [workshop-mod.md](workshop-mod.md)
- 上传 ATS / ETS2 两套创意工坊（各一个 item，内容相同）
- `SiiWriter` 直写兜底（幂等：按 URL 定位条目，替换或追加，同步计数）
- **验收**：订阅→启用→电台列表出现 QQ Music Radio；不订阅时 Companion 也能接入

## M4 — 发布

- Inno Setup 安装器（含开机启动项、VB-CABLE 检测与引导下载）
- 图标、截图、中文图文教程（Steam 指南 + B 站）
- GitHub Release（源码 + 单文件 exe + 安装包）
- **验收**：干净环境按教程 10 分钟跑通

## V2 Backlog（不承诺排期）

- 进程级回环捕获（免虚拟声卡）
- 网易云 / Spotify / 系统音频模式
- 局域网远程收听
- 音量归一化
- 自动更新（Velopack）

## 目录结构规划

```
qq-music-radio/
├─ README.md
├─ docs/
│  ├─ architecture.md
│  ├─ roadmap.md          # 本文件
│  ├─ mvp-zero-code.md
│  └─ workshop-mod.md
├─ src/
│  └─ QqMusicRadio.Companion/     # M1 起
├─ mod/
│  └─ qq-music-radio/             # M3 起（workshop 内容）
└─ scripts/                       # M4 起（打包/安装脚本）
```
