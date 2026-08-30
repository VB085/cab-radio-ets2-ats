# 架构与关键设计决策

## 1. 总体架构

```
┌─────────────────────────────────────────────────┐
│ ATS / ETS2 游戏                                  │
│   Radio → 「QQ Music Radio」                     │
└───────────────┬─────────────────────────────────┘
                │ GET http://127.0.0.1:17890/stream.mp3
                ▼
┌─────────────────────────────────────────────────┐
│ Companion (C# / .NET 8, 托盘程序)                │
│  HttpServer ── RingBuffer ── LAME 编码           │
│                              ▲                  │
│                     WASAPI loopback 采集         │
│  + 静音填充 / 设备管理 / 进程感知 / sii 直写兜底  │
└───────────────┬─────────────────────────────────┘
                │ PCM (共享模式)
                ▼
┌─────────────────────────────────────────────────┐
│ 虚拟音频设备: VB-CABLE                           │
│   CABLE Input (播放端) → CABLE Output (录制端)   │
└───────────────┬─────────────────────────────────┘
                │ 音频输出
                ▼
         任意音频应用（播放器 / 浏览器 / …）
```

## 2. 组件职责

### Workshop Mod（最薄的一层）
- `def/live_streams.sii` 只定义一条电台：`http://127.0.0.1:17890/stream.mp3`
- 不含任何代码、可执行文件、版权内容

### Companion（项目核心）
- 音频采集（WASAPI loopback，共享模式）
- MP3 编码（LAME，CBR 128/192 kbps）
- 本地 HTTP 推流（支持多客户端并发）
- **静音填充**（无音频活动/设备缺失时持续推静音，游戏电台不断开）
- 设备枚举 / 热插拔自动重连
- 托盘图标、配置、开机启动
- 游戏进程感知（`amtrucks.exe` / `eurotrucks2.exe`，自动启停推流）
- `live_streams.sii` 直写兜底（不依赖创意工坊也能接入）

### 虚拟音频设备
- 采用 **VB-CABLE**：QQ 音乐输出到 `CABLE Input`（播放端），Companion 从 `CABLE Output`（录制端）loopback 采集
- **为什么不自研驱动**：Windows 10/11 音频驱动需要 EV 代码签名证书 + 微软 attestation 签名，成本高、维护重、对用户无感知增益。这是对原方案最重要的一条修正。

## 3. 数据流与音频格式

```
WASAPI loopback (float32, 48kHz, 立体声, 设备混音格式)
  → 重采样 44.1kHz + 16-bit
  → LAME CBR 128/192kbps
  → MP3 帧 → 环形缓冲 (RingBuffer)
  → HTTP 响应流 (每客户端独立读游标)
```

## 4. 关键设计决策

| 决策 | 选择 | 理由 | 备选 |
|---|---|---|---|
| 虚拟设备 | VB-CABLE | 免费成熟、支持静默安装、社区验证充分 | Voicemeeter；自研驱动（否决：EV 签名成本） |
| 采集库 | NAudio `WasapiLoopbackCapture` | 纯托管、成熟稳定 | CSCore |
| 编码器 | LAME (`lame_enc.dll`) | MP3 编码质量最好 | Shine（纯托管、零原生依赖、质量略低）—— 可作为分发简化备选 |
| 编码格式 | MP3 CBR | 游戏端兼容性最确定 | OGG（游戏支持但资料少）；AAC（社区说法不一，不赌） |
| HTTP 服务器 | 手写 `TcpListener`（~150 行） | 无端口 ACL 问题、零依赖、单文件 exe | Kestrel；HttpListener（127.0.0.1 字面前缀对非管理员有 ACL 坑） |
| 静音填充 | RMS 活动检测 + 启动时预编码静音帧 | 暂停/切歌/无设备时游戏电台保持连接 | 断流重启（游戏端会报错，体验差） |
| 分发 | 创意工坊 + 直写 `Documents\...\live_streams.sii` 双通道 | Workshop 负责发现与更新，直写负责兜底（不订阅也能用） | 仅 Workshop |
| 码率 | 默认 128 kbps，可调 192 | 128 对音乐够用且延迟更低 | 320（无必要） |
| 驱动分发 | 引导用户下载安装 | VB-Audio 为 donationware，不可捆绑 | 捆绑（不合规，否决） |

## 5. Companion 内部模块

```
QqMusicRadio.Companion/
├─ Program.cs            # 入口，单实例互斥
├─ Capture/              # WASAPI loopback → 重采样 → 编码 → RingBuffer
│  ├─ AudioCapture.cs    # NAudio 采集封装
│  ├─ Mp3Encoder.cs      # LAME 封装
│  └─ SilenceSource.cs   # 静音帧生成（设备缺失/无活动时接管）
├─ Server/
│  ├─ HttpServer.cs      # 手写 TcpListener；GET /stream.mp3 + / 状态页
│  └─ RingBuffer.cs      # 多读游标；慢客户端超限断开
├─ Devices/
│  └─ DeviceManager.cs   # 枚举/选择/热插拔检测（MMDevice API）
├─ Game/
│  └─ GameWatcher.cs     # 轮询游戏进程，自动启停推流
├─ Sii/
│  └─ SiiWriter.cs       # 幂等写入 live_streams.sii（兜底通道）
├─ Tray/
│  └─ TrayApp.cs         # NotifyIcon：设备/码率/开机启动/状态页/退出
└─ Config.cs             # %APPDATA%\QQMusicRadio\config.json
```

**线程模型：** 采集线程 → 编码 → RingBuffer（锁保护）；每个 HTTP 客户端一个读游标；无客户端且游戏未运行时自动暂停采集省资源。

## 6. live_streams.sii 格式

游戏读取 `Documents\American Truck Simulator\live_streams.sii`（ETS2 同理）。条目为管道分隔，**无空格**：

```
stream_data[N]: "URL|名称|类型|语言|码率|收藏"
```

示例：

```
stream_data[0]: "http://127.0.0.1:17890/stream.mp3|QQ Music Radio|Pop|CN|128|1"
```

规则：
- 顶部 `stream_data: N` 计数必须等于条目总数（追加条目时要同步 +1）
- 索引连续无空洞
- 游戏内「Update From Internet」不会覆盖自定义条目（可直接刷新列表）
- mod 形态为 `def/live_streams.sii`，包装在 `live_stream_def` 块内，打包前解包 base.scs 核对一次实际格式

## 7. 用户流程

**首次：** 订阅 mod → 装 VB-CABLE → 装 Companion → 播放器输出选 `CABLE Input`
**日常：** 电脑上放歌 → Companion（可开机自启，跟随游戏自动启停）→ 进游戏 → 电台选 `Cab Radio`

## 8. 风险与对策

| 风险 | 对策 |
|---|---|
| Steam 审核（曾有教"导入本地流"的指南被移除） | mod 极简：一条 URL 定义、无 exe、无版权内容；直写 sii 兜底 |
| VB-Audio 授权 | 不捆绑，引导安装；README 注明 donationware |
| 杀软误报（本地 HTTP + 音频捕获） | 开源透明；发布版可选代码签名 |
| 端口 17890 冲突 | 端口可配置；状态页显示占用提示 |
| 设备被卸载/占用 | 热插拔检测 + 自动重连 + 静音兜底 |
| 游戏更新改 sii 格式 | 仅依赖公开稳定的 live_streams.sii；发布前解包核对 |
| 游戏重写 live_streams.sii 导致条目丢失（文件解析失败时实测；Update 不删条目已确认） | 写入逻辑保证文件始终有效；Companion 的 SiiWriter 做开机自检，条目丢失时自动重写 |
| 游戏内两次"无缝"疑问 | 连接无缝（选台一次即可）可保证；sample 级零间隙不可保证——但背景音乐场景无需在意 |

## 9. V2 扩展方向

- **进程级回环捕获**（Win10 2004+，OBS「应用程序音频捕获」同款技术）：直接抓 QQ 音乐进程，免虚拟声卡
- 网易云 / Spotify / 系统音频模式（共用 Companion，只换采集源）
- 局域网远程收听（副驾/手机听同一条流）
- 音量归一化、元数据显示
