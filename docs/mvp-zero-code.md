# M0：零代码验证（半天）

**目标**：不写一行代码，验证「虚拟声卡 → 采集编码 → 本地 HTTP → 游戏电台」全链路，并记录游戏端的真实行为（尤其暂停/断流表现），为 Companion 设计提供依据。

## 准备

1. **安装 VB-CABLE**
   - 下载：https://vb-audio.com/Cable/ （donationware，免费；请按需捐赠）
   - 解压后运行 `VBCABLE_Setup_x64.exe`，安装后**重启**
   - 验证：系统声音设置里应出现播放设备 `CABLE Input`、录制设备 `CABLE Output`

2. **QQ 音乐指向虚拟设备**
   - QQ 音乐 → 设置 → 音频 → 输出设备 → 选 `CABLE Input (VB-Audio Virtual Cable)`
   - 放一首歌，确认耳机没声音（声音进了虚拟设备，属正常）
   - 可选验证：系统「声音设置 → 录制 → CABLE Output」应有电平跳动

## 方案 A：VLC（推荐，最少组件）

VLC 同时承担「采集 + 编码 + HTTP 推流」两个角色。两种用法任选，**GUI 方式更不容易出错**。

### A1. GUI 方式（推荐）

1. 安装 VLC（https://www.videolan.org/vlc/ ）后打开
2. 菜单「媒体 → 打开捕获设备」：
   - 捕获模式：DirectShow
   - 视频设备名称：无
   - 音频设备名称：`CABLE Output (VB-Audio Virtual Cable)`（从下拉列表里选，不用手打）
3. 底部「播放」按钮旁边的小箭头 ▼ → 选「串流」
4. 源页面直接「下一步」
5. 目标：新目标选 `HTTP` →「添加」→ 端口 `17890`、路径 `/stream.mp3` →「下一步」
6. 转码：勾选「激活转码」→ 配置文件选 `Audio - MP3` →「下一步」→「流」（该预设为 64kbps，验证链路足够；音质优化是 M1 Companion 的事）
7. 保持 VLC 运行，用浏览器打开 `http://127.0.0.1:17890/stream.mp3` 验证能听到 QQ 音乐

### A2. 命令行方式（备选）

在 PowerShell 里运行（`vlc` 默认不在 PATH，用完整路径；**窗口要一直开着**，Ctrl+C 停止）。若提示找不到 vlc.exe，先查实际安装路径：`Get-Process vlc | Select-Object Path`（常见：`D:\VLC\vlc.exe`、`C:\Program Files (x86)\VideoLAN\VLC\vlc.exe`）：

```powershell
& "C:\Program Files\VideoLAN\VLC\vlc.exe" -vv -I dummy dshow:// :dshow-vdev=none :dshow-adev="CABLE Output (VB-Audio Virtual Cable)" ":sout=#transcode{acodec=mp3,ab=128,channels=2,samplerate=44100}:standard{access=http,mux=raw,dst=127.0.0.1:17890/stream.mp3}"
```

> PowerShell 注意：`:sout=...` 必须加引号，因为 `#` 在 PowerShell 里是注释符，不加引号会被吞掉；`-vv` 会把错误打到窗口里，方便排查。

- 设备名必须与系统里显示的**完全一致**（含括号和空格），先在 GUI 的「打开采集设备」里核对全名
- 验证：用浏览器或另一个 VLC 实例打开 `http://127.0.0.1:17890/stream.mp3`
- 防火墙首次弹窗放行；若 17890 被占用，改端口（后面的 sii 条目同步改）

## 方案 B：Icecast + BUTT（备选，管理更直观）

1. **Icecast**（本地服务器）：配置 `icecast.xml`，端口 8000，关闭认证（本地环境）
2. **BUTT**（broadcast using this tool，danielnoethen.de/butt）：
   - Input：选 `CABLE Output` 录制端点
   - Server：`127.0.0.1:8000`，mountpoint `/qqmusic`
   - Codec：MP3 128 kbps
3. 连接后验证：浏览器打开 `http://127.0.0.1:8000/qqmusic`

## 接入游戏

> **更稳的方式（推荐）**：用 `mod/qq-music-radio.scs` 本地 mod（复制到 `Documents\<游戏>\mod\` 后在 mod 管理器启用）。mod 里的电台定义不受「Update From Internet」重写文件的影响。下面的 Documents 文件方式作为备选/验证用。

1. 先运行一次游戏，进 Radio 面板点 **Streaming → Update From Internet**，生成 `live_streams.sii`
2. **关闭游戏**，写入条目（辅助脚本或手动编辑，二选一）：

   **方式一（推荐）**：运行 `scripts\add-stream.ps1`
   ```
   powershell -ExecutionPolicy Bypass -File scripts\add-stream.ps1
   ```
   脚本幂等插入/更新条目，自动同步计数并备份原文件。文件位置：ATS 在 `Documents\American Truck Simulator\`，ETS2 在 `Documents\Euro Truck Simulator 2\`。

   **方式二（手动编辑）**：打开
   `Documents\American Truck Simulator\live_streams.sii`
   在 `SiiNunit` 的 `{ }` 内追加（`N` = 现有最大编号 +1）：

   ```
   stream_data[N]: "http://127.0.0.1:17890/stream.mp3|QQ Music Radio|Pop|CN|128|1"
   ```

   并把文件顶部的 `stream_data: N` 计数 **+1**。
3. 进游戏 → 电台 → 列表里选 **QQ Music Radio**

## 验收检查点

- [ ] 游戏内声音正常、无断续
- [ ] QQ 音乐换歌/暂停，游戏端实时跟随
- [ ] **暂停/没放歌时游戏端表现**（报错？静音？断开？——记录下来，这决定 Companion 的静音策略）
- [ ] 关掉推流工具再重新打开，游戏端表现（需要重新选台吗？）
- [ ] 延迟测量：秒表计时「点播放 → 游戏出声」的间隔
- [ ] 连续播放 30 分钟稳定性

## 验证报告模板

```
游戏版本:          ATS / ETS2 1.x
工具链:            VB-CABLE x.x + VLC/BUTT x.x
链路延迟:          ___ 秒
换歌跟随:          正常 / 异常（描述）
暂停表现:          游戏端 ___（静音/断流/报错）
推流中断恢复:       需要重新选台 / 自动恢复
结论:              链路可用 / 需调整 ___
```

## 常见问题

- **游戏电台列表没有新条目** → 计数没同步、游戏没完全退出、改错了文件目录
- **条目消失** → 已确认的原因：文件解析失败时游戏会重写 live_streams.sii 并丢弃自定义条目（旧版脚本插入位置错误导致过此问题，已修复）。「Update From Internet」已实测确认不会删条目。恢复办法：退出游戏 → 重跑 add-stream.ps1 → 重新进游戏
- **游戏里能选台但没声音** → 推流工具没在运行；防火墙拦截；URL 端口与推流端口不一致
- **VLC 抓不到 CABLE** → 设备全名不对；QQ 音乐输出设备没选对
- **浏览器能播、游戏不能** → 确认推流是 MP3 而非其他封装（mux=raw）
