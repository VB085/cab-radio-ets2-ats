# 创意工坊 mod 制作与上传

## mod 目录结构

```
qq-music-radio/
├─ manifest.sii
├─ def/
│  └─ live_streams.sii
├─ description.txt
└─ icon.jpg
```

## 文件内容

### manifest.sii

```
SiiNunit
{
mod_package : .package_name {
    package_version: "1.0"
    display_name: "Cab Radio · 驾驶室电台"
    author: "your name"
    category[]: "other"
    icon: "icon.jpg"
    description_file: "description.txt"
}
}
```

可选：`compatible_versions[]: "1.5*"` 限定兼容的游戏大版本。

> **⚠️ 实测坑（2026-08，游戏 1.60+）**：manifest 的单元名**必须**用 `.package_name`（官方匿名写法，SiSL's Trailer Pack 的 manifest 注释原文推荐"Please keep this form"）。自定义名如 `.qq_music_radio` 会被游戏拒绝，报错 `The unit name '_nameless.qq_music_radio' is in wrong format`，导致整个 mod 内容树加载失败（mod 管理器能看到、能启用，但 def 不生效）。

### def/live_streams.sii

```
SiiNunit
{
live_stream_def : _nameless.5151.4d52.0001 {
    stream_data: 1
    stream_data[0]: "http://127.0.0.1:17890/stream.mp3|Cab Radio|Music|CN|128|1"
}
}
```

> **⚠️ 单元名格式**：用 `_nameless.xxx.xxx.xxx` 三段式（与游戏自身生成的 live_streams.sii 一致），避免同款"wrong format"报错。

> **注意**：字段格式以游戏 base.scs 内 `def/live_streams.sii` 的实际结构为准。打包前用 SCS 官方 **scs_extractor** 解包 base.scs 核对一次，再生成最终文件。

### description.txt

必须写清楚使用前提：本 mod 只提供电台入口，**声音来自用户本机的 Companion**（未安装 Companion + VB-CABLE 时该电台无声）。避免创意工坊评论区被"没声音"刷屏。

### icon.jpg

建议方形 512×512（创意工坊预览图）。mod 本身极小（几 KB）。

## 打包

- `.scs` 本质是 **zip 重命名**：根目录直接是 `manifest.sii`、`def/`（不要多套一层文件夹）
- 或使用 SCS 官方 `scs_packer`（需要额外的打包用 manifest）

## 上传（SCS Workshop Uploader）

1. Steam → 库 → 工具 → 安装 **SCS Workshop Uploader**
2. 登录 → 选择游戏（**ATS 和 ETS2 各传一次**，是两个独立创意工坊）
3. New Item → 选择 mod 文件 → 上传
4. 填写标题、描述、预览图
5. 后续更新：改 `package_version` 后对同一 item 重新上传

## 上传前检查清单

- [ ] 游戏内 mod 管理器启用后，电台列表出现 `QQ Music Radio`
- [ ] 包内不含任何 exe / dll（只允许数据文件）
- [ ] 描述写清使用前提（Companion + VB-CABLE + QQ 音乐）
- [ ] 两个游戏都测试过

## 审核风险与对策

- Steam 上曾有一个教「导入本地流」的指南**因违反社区准则被移除**，说明平台对这类内容有审核敏感性
- 对策：mod 只有一条 127.0.0.1 URL 定义，无代码、无可执行文件、无版权内容，风险极低
- **创意工坊永远是"锦上添花"**：Companion 直写 `live_streams.sii` 才是核心通道，即使 mod 被下架或不订阅，功能照常

## Companion 直写 live_streams.sii（兜底通道）

- 游戏关闭时写入最安全；游戏运行时写入则需重启游戏刷新
- **⚠️ 实测教训（2026-08）**：游戏「Update From Internet」**不会**删除自定义条目（对照实验确认，与社区 wiki 一致）。真正会丢条目的场景是「文件解析失败时游戏重写文件」（最早由我们插入位置 bug 触发，已修复）——写入逻辑保证文件始终有效即可
- **幂等逻辑**：
  1. 扫描所有 `stream_data[*]`，找到 URL 为 `http://127.0.0.1:17890/...` 的条目 → 原地替换
  2. 没找到 → 追加到末尾（最大编号 +1）
  3. 同步更新顶部 `stream_data: N` 计数
- 两个游戏目录分别处理：
  - `Documents\American Truck Simulator\live_streams.sii`
  - `Documents\Euro Truck Simulator 2\live_streams.sii`
- 首次运行游戏前文件不存在 → 提示用户先运行一次游戏（或按格式生成最小文件）
