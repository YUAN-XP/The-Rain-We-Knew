# ProjectMystery Audio Pass 0 音乐接入与验证报告

本轮已完成统一音乐系统、已确认歌曲首次接入、正常入口全 Demo 流程回归和 Windows 正式构建。任务书的 12 项音乐流程检查在功能与状态层面通过，最终正常流程记录未出现本轮音乐代码异常。最终混音、无缝循环和主观听感不属于本轮已通过的范围，下面分别说明证据与限制。

交付日期：2026 年 10 月 6 日。工程：[D 盘 ProjectMystery 工程](<D:/GitHub Desktop/ProjectMystery/ProjectMystery>)。本轮依据：[Audio Pass 0 原始任务书](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/AudioPass0_任务书原文.txt>)。基线提交：`f6d31de6320a6838cbddb91c6ad0e902e847ef74`；交付时 HEAD 未改变，未执行 Commit 或 Push。

## 1 打开游戏与核对结果

正式入口（2026 年 10 月 11 日更新）：[ProjectMystery.exe](<D:/游戏设计/正式游戏/ProjectMystery/ProjectMystery.exe>)，桌面快捷方式现已统一指向这里。原 [Audio Pass 0 阶段导出](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/build_release/ProjectMystery.exe>) 保留为阶段快照；两个目录的 221 个构建文件已逐项核对一致。这次入口修复不修改游戏内容。请保留程序同目录的 `ProjectMystery_Data`、运行库等文件，不能只复制 exe。后续版本按照 [正式发布规范](../../Tools/Release/README.md) 更新固定入口，具体核对记录见 [桌面入口修复记录](../Release/DesktopEntry_20261011.md)。

全流程在 Unity Play Mode 从原主菜单与新游戏入口开始，以输入回放和必要的原界面操作完成，没有直接授予剧情标记、替换答案或跳过场景。测试记录区间为 00:34:03 至 01:48:58，其中包含等待、暂停、界面核对与分段恢复；这不是 Demo 通关时长指标。之后退出测试并释放输入，停止 Play Mode，保存文件，再构建。

Windows 构建完成于 01:52:25，结果为成功、0 个构建错误、488 条警告。[构建结果](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/checks/release-build.txt>) 与 [完整构建消息](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/checks/release-build-messages.txt>) 已保留。警告主要涉及工程现有 AI/Sentis 包的着色器编译信息，另有字体导入一致性和 RuntimePipelineManager 提示；没有为消除这些消息而修改包、字体或渲染设置，也不声称所有警告均已逐条确认来自本轮之前。

保存并关闭 Unity 编辑器以释放内存后，冷启动此正式构建，实际检查了中文主菜单、新游戏确认、原开场、自由行动时静止超过一分钟以及 Escape 退出确认，均正常；程序正常退出。该次正式窗口检查没有进行第二遍全 Demo 回放，也没有完成主观音频试听。[正式窗口检查记录](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/release_smoke/smoke-results.json>)、[正式窗口日志](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/release_smoke/Player.log>) 可核对。日志有 Unity TLS 网络提示和原 PersistentRuntime 的出生点提示，没有检测到异常栈或音乐配置错误。正式窗口画面已在本任务对话中展示，没有另外保存一套长视频或图片序列。

## 2 音乐系统的最终实现

`MusicManager` 是独立的唯一持久对象，不替代或重建原 `PersistentRuntime`。普通 BGM 使用 A/B 两个声源做可配置的淡入淡出；旧声源到零增益才停止。相同状态不重新起播，快速连续请求保留最后目标，不增加第三个普通 BGM 声源。稳态通常只有一个普通 BGM；渐变期短暂双声源属于设计中的交叉淡变。

剧情脚本无需认识歌曲名。`MusicStateRouter` 读取原小游戏、学校世界、追逐和结尾阶段，通知统一状态；`SceneMusicProfile` 保存场景默认状态。它不写任务标记，不修改谜题答案，也不按 Shift 或角色坐标决定追逐音乐。原主菜单、全局暂停和失焦暂停保留，只有主菜单 Cue 在菜单暂停下继续播放。

混音器具有 Master、Music、Ambience、SFX、UI 分组。BGM、三门母曲及完整生日演奏进入 Music，原未分组音源进入 SFX；Ambience 和 UI 是后续接入接口，本轮没有重新加入已清除的环境循环或新 UI 音效。原设置中的总音量和存储方式保留，没有通过总音量归零处理音频。

普通对白将音乐压到 0.45 倍，压低约 0.2 秒、恢复约 0.8 秒；原记忆/登车漫画打开时普通音乐压到 0，让演出优先。具体参数在 MusicLibrary 中可调整。本轮保留原电话、相机、篮球、危险反馈、吉他按键与剧情声音。

### 新生产脚本

| 文件 | 作用 |
| --- | --- |
| [MusicState.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/Audio/MusicState.cs>) | 定义 None 和 12 个音乐状态，主菜单与结束页保留独立状态。 |
| [MusicCue.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/Audio/MusicCue.cs>) | 每个状态独立保存 Clip、Loop、基础音量、淡入淡出、起播时间与播放归属。 |
| [MusicLibrary.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/Audio/MusicLibrary.cs>) | 统一 Cue 与混音分组绑定，以及对白压低参数和伴奏备用引用。 |
| [SceneMusicProfile.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/Audio/SceneMusicProfile.cs>) | 场景角色及默认音乐状态；不依赖角色坐标。 |
| [MusicManager.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/Audio/MusicManager.cs>) | 唯一持久音乐管理器、双声源渐变、同状态连续播放与原暂停规则。 |
| [MusicStateRouter.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/Audio/MusicStateRouter.cs>) | 读取现有剧情与小游戏状态，决定音乐状态，不写剧情标记或移动角色。 |
| [ThreeDoorsMusicSettings.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/Audio/ThreeDoorsMusicSettings.cs>) | 三门清晰度、阶段恢复、声像、距离、淡出和错误反馈接口。 |
| [ThreeDoorsMusicController.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/Audio/ThreeDoorsMusicController.cs>) | 复用原三门声源，同母曲同 DSP 时刻启动，动态更新滤波和音量。 |

### 配置与调节位置

统一配置：[MusicLibrary.asset](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/MusicLibrary.asset>)。混音器：[ProjectMystery_AudioMixer.mixer](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Mixer/ProjectMystery_AudioMixer.mixer>)。三门参数：[ThreeDoorsSettings.asset](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/ThreeDoorsSettings.asset>)。

后续换歌只需更换相应 Cue 的 Clip；调音量、淡入淡出、Loop 或起播时间，只需改对应 Cue。主菜单和结束页虽然使用同一首完整主题曲，仍为两个独立配置。不要重新运行制作脚本来覆盖人工调音后的 Cue。

| Cue | 曲目 | 基础音量 | 淡入 / 淡出 秒 | 起播 秒 | 循环 |
| --- | --- | --- | --- | --- | --- |
| [MainTheme](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/MainTheme.asset>) | 道别是一件难事 | 0.3917 | 1.5 / 1.5 | 0 | 是 |
| [EndTheme](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/EndTheme.asset>) | 道别是一件难事 | 0.3917 | 1.8 / 1.5 | 0 | 是 |
| [RainExplore](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/RainExplore.asset>) | 雨里醒来 | 0.2901 | 1.5 / 1.5 | 0 | 是 |
| [HomeExplore](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/HomeExplore.asset>) | 熟悉的空房间 | 0.1349 | 1.5 / 1.5 | 0 | 是 |
| [ThreeDoors](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/ThreeDoors.asset>) | 门后的吉他 | 0.1354 | 1.5 / 1.5 | 0 | 是 |
| [StreetExplore](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/StreetExplore.asset>) | 七月的街道 | 0.2742 | 1.5 / 1.5 | 0 | 是 |
| [MazeExplore](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/MazeExplore.asset>) | 绕远一点 | 0.1354 | 1.5 / 1.5 | 0 | 是 |
| [BirthdayPerformance](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/BirthdayPerformance.asset>) | 生日快乐+by怪盗V | 0.5794 | 1.5 / 1.5 | 0 | 否 |
| [SchoolOtherworld](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/SchoolOtherworld.asset>) | 空校舍 | 0.1347 | 1.5 / 1.5 | 0 | 是 |
| [SchoolMemory](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/SchoolMemory.asset>) | 课间的光 | 0.1199 | 1.5 / 1.5 | 0 | 是 |
| [Chase](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/Chase.asset>) | 别停下 | 0.1376 | 1.5 / 1.5 | 0 | 是 |
| [Companionship](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Resources/AudioPass0/Companionship.asset>) | 有人陪你走 | 0.1255 | 1.5 / 1.5 | 0 | 是 |

这些基础音量是第一轮接入的初值，不是最终响度标准。源文件没有 Normalize、重编码或裁剪；普通长曲使用 Streaming、Vorbis、保留立体声与采样率导入。

### 三门的最终设置

沿用原左、中、右声源位置 `(12.5, 8.16)`、`(15, 8.16)`、`(17.5, 8.16)`，全部使用《门后的吉他》完整母曲，Unity 导入长度约 161.09 秒。所有声源使用同一个计划 DSP 起播时刻。答案仍为右、左、中，选错仍走原异常反馈和重置。

Clear / Medium / Distant 的相对音量为 1 / 0.44 / 0.19，低通初值为 12000 / 5200 / 2100 Hz。Stage0 至 Stage3 的恢复音量为 0.72 / 0.82 / 0.92 / 1，截止频率倍数为 0.5 / 0.65 / 0.82 / 1。参数平滑约 0.25 秒；采用距离衰减和左右声像，SpatialBlend、Reverb 可调但本轮默认不启用。长廊引导和最终完成淡出也保留独立参数。WrongDoorSFX 接口绑定原错误素材备用，原错误反馈机制继续工作，没有生成新音乐。

采样时逐个读取三声源的 `timeSamples`，阶段 0 和 2 的观测差值为 0 或一个 DSP 块 1024 样本，阶段 1 和 3 为 0。它支持共同起播、没有持续累积漂移的判断，但不能把跨音频线程的逐个读取说成每个瞬间都严格相等；同一 DSP 起播安排和绑定检查是同步机制的依据。三门听辨难度、声像方向、滤波听感仍需要人工试听。

## 3 原曲与实际导入文件

12 个原文件均保留原文件名与原内容，总计 50,574,710 字节。每个文件在 Unity 音乐目录有且仅有一个 SHA256 相同的副本。《道别是一件难事（伴奏）》仅导入备用，未分配剧情 Cue。

| 原始文件 | Unity 文件 | 核对 |
| --- | --- | --- |
| [七月的街道.mp3](<D:/游戏设计/音乐/七月的街道.mp3>) | [Assets/Audio/Music/Exploration/StreetExplore.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Exploration/StreetExplore.mp3>) | SHA256 相同 |
| [别停下.mp3](<D:/游戏设计/音乐/别停下.mp3>) | [Assets/Audio/Music/Chase/Chase.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Chase/Chase.mp3>) | SHA256 相同 |
| [有人陪你走.mp3](<D:/游戏设计/音乐/有人陪你走.mp3>) | [Assets/Audio/Music/Narrative/Companionship.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Narrative/Companionship.mp3>) | SHA256 相同 |
| [熟悉的空房间.mp3](<D:/游戏设计/音乐/熟悉的空房间.mp3>) | [Assets/Audio/Music/Exploration/HomeExplore.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Exploration/HomeExplore.mp3>) | SHA256 相同 |
| [生日快乐+by怪盗V.mp3](<D:/游戏设计/音乐/生日快乐+by怪盗V.mp3>) | [Assets/Audio/Music/Narrative/BirthdayPerformance.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Narrative/BirthdayPerformance.mp3>) | SHA256 相同 |
| [空校舍.mp3](<D:/游戏设计/音乐/空校舍.mp3>) | [Assets/Audio/Music/Exploration/SchoolOtherworld.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Exploration/SchoolOtherworld.mp3>) | SHA256 相同 |
| [绕远一点.mp3](<D:/游戏设计/音乐/绕远一点.mp3>) | [Assets/Audio/Music/Exploration/MazeExplore.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Exploration/MazeExplore.mp3>) | SHA256 相同 |
| [课间的光.mp3](<D:/游戏设计/音乐/课间的光.mp3>) | [Assets/Audio/Music/Exploration/SchoolMemory.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Exploration/SchoolMemory.mp3>) | SHA256 相同 |
| [道别是一件难事.mp3](<D:/游戏设计/音乐/道别是一件难事.mp3>) | [Assets/Audio/Music/Theme/Theme_Full.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Theme/Theme_Full.mp3>) | SHA256 相同 |
| [道别是一件难事（伴奏）.mp3](<D:/游戏设计/音乐/道别是一件难事（伴奏）.mp3>) | [Assets/Audio/Music/Theme/Theme_Instrumental_Reserve.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Theme/Theme_Instrumental_Reserve.mp3>) | SHA256 相同 |
| [门后的吉他.mp3](<D:/游戏设计/音乐/门后的吉他.mp3>) | [Assets/Audio/Music/Puzzle/ThreeDoors_MotherTrack.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Puzzle/ThreeDoors_MotherTrack.mp3>) | SHA256 相同 |
| [雨里醒来.mp3](<D:/游戏设计/音乐/雨里醒来.mp3>) | [Assets/Audio/Music/Exploration/RainExplore.mp3](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Audio/Music/Exploration/RainExplore.mp3>) | SHA256 相同 |

新建文件的完整清单，包括 `.meta`、全部 Cue、混音器、编辑器工具及文档，在 [文件清单](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Documentation/AudioPass0/AudioPass0_FileManifest.tsv>) 中逐项记录路径、大小和哈希。清单只描述交付文件，不执行提交。

### 新增测试工具

[AudioPass0Build.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Editor/AudioPass0Build.cs>) 用于本轮资源装配、审计与构建；[AudioPass0QA.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Editor/AudioPass0QA.cs>) 用于状态、声源与隔离系统验证；[AudioPass0Replay.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Editor/AudioPass0Replay.cs>) 是明确启停的输入回放；[AudioPass0OutputProbe.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/System/AudioPass0OutputProbe.cs>) 只在编辑器编译，用于复制最终输出 PCM，不访问麦克风。

正式构建的 `Assembly-CSharp.dll` 中未发现回放、录音探针或 AudioPass0QA 类型，也没有 Editor 程序集进入 Managed 目录。[构建排除核对](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/checks/player-build-audit.json>) 记录最终程序集 SHA256：`1fdfd6e832ebeb0e8ab91a395d3adcfcdc316f112ea946b6093f5fa9ee4def26`。测试结束恢复了原存档目录并释放所有输入，不会在玩家正常启动时自动向右走或运行测试计划。

## 4 现有文件的改动与保护

| 现有文件 | 具体改动 |
| --- | --- |
| [Assets/Scripts/Story/ThreeDoorPuzzleController.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/Story/ThreeDoorPuzzleController.cs>) | 新三门控制器接管时跳过旧声源启动和更新；原答案、选择与重置逻辑不变。 |
| [Assets/Scripts/Story/DistortedCorridorController.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/Story/DistortedCorridorController.cs>) | 统一音乐系统有效时停用旧吉他引导的重复驱动；原长廊、灯光及移动逻辑不变。 |
| [Assets/Scripts/School/SchoolReadingChair.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/School/SchoolReadingChair.cs>) | 坐下阅读时沿用学校 Cue，避免旧程序生成的休息音乐叠加；原坐下流程不变。 |
| [Assets/Scripts/Scene03/Scene03RiverSequence.cs](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scripts/Scene03/Scene03RiverSequence.cs>) | 完整生日歌曲播放前等待探索曲退出，结束时淡出；原四句节奏、最短欣赏时长、E 离开、镜头目标和剧情顺序保留。 |
| [Assets/Scenes/Scene01_Rain.unity](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scenes/Scene01_Rain.unity>) | 增加 Rain 类型的 SceneMusicProfile 根对象。 |
| [Assets/Scenes/Scene02_Courtyard.unity](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scenes/Scene02_Courtyard.unity>) | 增加 Home Profile，在原三门对象挂音乐控制器并绑定原位置的三声源。 |
| [Assets/Scenes/Scene03_Childhood.unity](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scenes/Scene03_Childhood.unity>) | 增加 Childhood Profile，将原河堤完整演奏字段绑定已确认生日歌曲。 |
| [Assets/Scenes/Scene04_School.unity](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scenes/Scene04_School.unity>) | 增加 School Profile；里世界与记忆世界按原 Variant 切换音乐。 |
| [Assets/Scenes/EndingSequence.unity](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets/Scenes/EndingSequence.unity>) | 增加 Ending Profile；按原 EndingDirector 阶段决定陪伴曲、留白及结束主题曲。 |

没有修改现有 Prefab、游戏数据、PlayerController、CameraFollow、GamePause、背包、对话管理器、EndingDirector 或 SchoolChaseController。河堤歌曲离开后会先完成音乐淡出再回到原镜头，这是本轮音乐衔接增加的约 1.5 秒等待，原相机目标、缩放值、任务与对白顺序没有改写。

交付后再次比对 845 个受保护文件，只有上表 9 个计划内文件改变，意外修改为 0。场景按组件比较：未删除任何原组件，原组件的语义变动仅限场景根列表、Scene02 三门音乐组件绑定与 Scene03 完整歌曲引用。恢复了编辑器播放造成的字体动态缓存与纯序列化格式差异。12 个音乐原文件和两个用户存档的 SHA256 全部保持不变。[最终范围与哈希审计](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/checks/final-scope-audit.json>) 可核对。

可回退的原文件放在 [修改前备份](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/rollback>)。如以后决定撤销本轮，请依据文件清单与基线做针对性恢复，不要把整个后续工程覆盖回旧版。

## 5 每个逻辑场景的音乐状态

现有工程实际只有 Scene01_Rain、Scene02_Courtyard、Scene03_Childhood、Scene04_School 和 EndingSequence 五个主要场景文件。任务书的 Scene05 正常高中是 Scene04_School 的 Memory 状态，Scene06 和 Scene07 在原 EndingSequence 中分阶段实现；没有为了配乐新造或改名场景。

| 逻辑位置 | 当前状态与曲目 | 切换依据 |
| --- | --- | --- |
| 主菜单 | MainTheme，道别是一件难事完整版 | 原主菜单状态；起播 0，循环 |
| Scene01 雨夜 | RainExplore，雨里醒来 | 场景默认；普通走路、奔跑与调查不切歌 |
| Scene02 普通家中 | HomeExplore，熟悉的空房间 | 同场景不同房间保持连续 |
| Scene02 儿童残影 | None | 原残影演出优先；不是 Chase |
| Scene02 长廊和三门 | ThreeDoors，门后的吉他母曲 | 原长廊 Joined 与谜题 Active / Progress |
| Scene03 童年街道 | StreetExplore，七月的街道 | 常规探索、NPC、可选小游戏附近 |
| Scene03 双布局迷宫 | MazeExplore，绕远一点 | 原 IsInMaze；A/B 切换不重播 |
| Scene03 河堤演奏 | BirthdayPerformance | 原节奏阶段保留按键音；完整欣赏阶段播放正式生日曲，不叠普通 BGM |
| Scene04 高中里世界 | SchoolOtherworld，空校舍 | 原 SchoolWorldVariant.Otherworld |
| Scene05 高中记忆 | SchoolMemory，课间的光 | 原 SchoolWorldVariant.Memory |
| Scene05 向焱追逐 | Chase，别停下；死亡/恢复为 None | 原危险 ChaseStep；安全剧情阶段按当前世界恢复 |
| Scene06 交接、车内、童年及高中闪回 | Companionship，有人陪你走 | 原 Handover / EndingSession / EndingStep；本轮暂定 Cue，可替换 |
| Scene07 前往医院与最终演出 | None | 原 WalkHospital 起淡出，医院、床前、宣告及最后对白不进主题曲 |
| 正式结束页 | EndTheme，道别是一件难事完整版 | 原最后一句完成后进入 Complete；起播 0，循环，淡入 1.8 秒 |

## 6 测试结果与证据范围

### 正常入口的 12 项音乐流程测试

下表的通过表示实际流程、状态、播放绑定及输出数据检查通过，不能替代人工混音与审美验收。

| 任务书项目 | 功能结果 | 已执行的验证与限制 |
| --- | --- | --- |
| 01 主菜单主题曲 | 通过 | 原主菜单进入与结尾返回主菜单都出现 MainTheme，完整版 Clip 从头播放，保留实际输出片段；正式构建菜单冷启动也正常。 |
| 02 新游戏音乐切换 | 通过 | 从原开始新游戏和确认进入 Scene01，MainTheme → RainExplore；仅渐变期允许两声源过渡，稳定阶段叠播计数为 0。渐变的主观顺滑程度待试听。 |
| 03 雨夜探索 | 通过 | 实际移动、调查、普通对白及奔跑持续 RainExplore；记忆漫画打开时压低到 0，不替换原演出声音。 |
| 04 家中探索连续性 | 通过 | 经过原一楼、二楼、主卧、隐藏房间及房间过渡，同一 HomeExplore 连续；独立同状态测试也证明播放位置推进。 |
| 05 三门母曲与定位 | 通过 | 实际选错重置后按右 → 左 → 中完成；原三个位置保留，三声源同 Clip 同计划启动，四阶段参数均更新。包含阶段 1 的隔离存档与读档。辨向的最终听感待人工试听。 |
| 06 街道与双布局迷宫 | 通过 | StreetExplore → MazeExplore；实际 Q / Space 切换 A/B，布局 0 和 1 均采样到，同状态不重启；原迷宫目标触发成功。 |
| 07 河堤吉他 | 通过 | 按原按键节奏完成四句，原吉他单音保留；完整欣赏时才播放已确认生日母曲，节奏阶段和完整欣赏阶段的普通 BGM 叠播计数均为 0。E 离开后原流程继续。 |
| 08 高中里世界 | 通过 | 正常进入学校，实际篮球、钥匙、器材室、书桌及相机操作时观察 SchoolOtherworld。 |
| 09 高中记忆与向焱追逐 | 通过 | 原世界切换进入 SchoolMemory；原危险追逐阶段为 Chase，安全取油/修灯阶段按当前世界恢复，最终冲锋再进入 Chase；追逐完成后正常返家。 |
| 10 陪伴与连续闪回 | 通过 | 实际凭证组合、交接、上车、车内、童年和高中片段走完；使用可替换的 Companionship Cue，不添加剧情触发条件。 |
| 11 医院最终演出 | 通过 | 前往医院起为 None，原推床、床前、聚焦及医生宣告段落走完，保留原剧情声音；声音清晰度与响度的人工听感未作最终验收。 |
| 12 游戏结束主题曲 | 通过 | 原最后一句结束并进入 Complete 后才观察到 EndTheme，提前进入主题曲计数为 0；从 0 起播、Loop=true。循环播放器绕回另有临近曲尾测试，未以自然等待整曲多圈替代证据。 |

本轮没有仍未修复的已发现音乐功能阻塞项。人工听感、最终混音和自然等待主题曲多圈的体验验证仍为未验证项；不是把这些项目标成通过。单独验证“主题曲不会只播放一次后永久停音”的方法是实际播放器临近曲尾绕回测试，而不是改短源文件。

### 原功能回归

玩家移动与 Shift 奔跑、普通对白、E 交互、Q / Space 相机、背包、场景过渡、原 Fade、三门、双布局迷宫、吉他四句、向焱追逐、油与灯组合、连续结尾演出、主菜单和结束页面均实际运行。实际篮球投球、钥匙掉落与拾取、原 4268 密码操作、返家 0715 抽屉、凭证组合及交接也完成。没有用测试计划直接写剧情完成标记。

原吉他按键四句记录均为 Perfect，原节奏计时保留。测试用存档写入 [本轮隔离存档](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/runtime/isolated-slots>)，阶段 1 读档完成；正式用户存档没有覆盖。结束后的回放关闭和静止待机由正式窗口复查支持。

### 三类证据分别成立

1. 配置与结构：23 项静态检查通过，验证 Cue、独立主题配置、混音分组、备用伴奏、五场景 Profile 和原三门绑定。
2. 隔离播放器：9 项实际播放器测试通过，含唯一管理器、重复实例删除、同状态连续、交叉淡变、连续请求、None 退出、菜单暂停例外、主题绕回及普通暂停。它们不是全流程通关证据。
3. 正常入口回归：22,062 条状态采样覆盖全部 13 个状态；MusicManager 和 PersistentRuntime 计数始终各为 1，渐变外普通音乐叠播为 0，吉他节奏/完整欣赏叠播为 0，主题曲早于最后一句为 0，最终 Application 错误记录为 0。

对应记录：[23 项静态检查](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/checks/unity-static-audit.json>)、[9 项播放器测试](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/checks/isolated-music-system.json>)、[全流程指标](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/checks/normal-flow-metrics.json>)、[输入与事件记录](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/runtime/events.txt>)、[全流程逐次音乐状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/runtime/music-observations.jsonl>)。最后一句完成后 EndTheme 首次观察时间为 01:44:53.535；医院至最后对白保持 None。

实际输出记录未出现削波。15 段 WAV 合计 139.85 秒，削波样本 0，录音峰值上限约 0.228；5 Hz 输出采样窗中最高峰约 0.318。这些是有限片段和采样窗，不是全曲真峰值、响度统一或每秒无削波的证明。

### 最终版本的试听、画面和状态

这些 WAV 取自游戏最终输出，不是把原 MP3 复制后称为测试录音。PNG 和状态 JSON 对应开始采集时的实际画面与状态；短阶段可能随原流程离开而提前结束。三门短片段不是同一时间点的标准 A/B 音质对照。

| 状态 | 片段长度 | 试听 | 对应画面与状态 |
| --- | --- | --- | --- |
| RainExplore | 12.00 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/01_RainExplore.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/01_RainExplore.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/01_RainExplore_state.json>) |
| HomeExplore | 12.00 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/02_HomeExplore.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/02_HomeExplore.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/02_HomeExplore_state.json>) |
| ThreeDoors Stage0 | 1.39 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/03_ThreeDoors_stage0.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/03_ThreeDoors_stage0.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/03_ThreeDoors_stage0_state.json>) |
| ThreeDoors Stage1 | 1.24 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/04_ThreeDoors_stage1.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/04_ThreeDoors_stage1.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/04_ThreeDoors_stage1_state.json>) |
| ThreeDoors Stage2 | 2.24 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/05_ThreeDoors_stage2.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/05_ThreeDoors_stage2.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/05_ThreeDoors_stage2_state.json>) |
| ThreeDoors Stage3 | 2.99 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/06_ThreeDoors_stage3.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/06_ThreeDoors_stage3.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/06_ThreeDoors_stage3_state.json>) |
| StreetExplore | 12.00 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/07_StreetExplore.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/07_StreetExplore.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/07_StreetExplore_state.json>) |
| MazeExplore | 12.00 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/08_MazeExplore.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/08_MazeExplore.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/08_MazeExplore_state.json>) |
| BirthdayPerformance | 12.00 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/09_BirthdayPerformance.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/09_BirthdayPerformance.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/09_BirthdayPerformance_state.json>) |
| SchoolOtherworld | 12.00 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/10_SchoolOtherworld.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/10_SchoolOtherworld.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/10_SchoolOtherworld_state.json>) |
| SchoolMemory | 12.00 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/11_SchoolMemory.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/11_SchoolMemory.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/11_SchoolMemory_state.json>) |
| Chase | 12.00 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/12_Chase.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/12_Chase.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/12_Chase_state.json>) |
| Companionship | 12.00 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/13_Companionship.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/13_Companionship.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/13_Companionship_state.json>) |
| EndTheme | 12.00 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/14_EndTheme.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/14_EndTheme.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/14_EndTheme_state.json>) |
| MainTheme | 12.00 秒 | [实际输出音频](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/15_MainTheme.wav>) | [画面](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/15_MainTheme.png>) / [状态](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/listening/15_MainTheme_state.json>) |

三门 Stage0 片段仅约 1.39 秒且处于入场渐强，单凭此片段不能评价整个谜题的辨向难度。请结合实际游戏与后续阶段试听。截图与音频均取自三门缺组件修复后的同一轮最终运行；不使用旧失败预览证明当前版本。

## 7 本轮修复及测试计划调整

三门首次播放暴露了真实组件问题：Unity 缺失组件的特殊空对象没有被普通 C# 空值合并正确处理，导致滤波器更新异常。已改成 Unity 对象有效性检查后逐个补齐低通与混响组件；随后重新从正常入口运行，三个声源在四阶段均更新，最终错误记录为 0。[问题与修复摘要](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/checks/failed_first_three_door_run/resolved-error-summary.json>) 保留原因及复测依据。

输入计划也发生过等待不足、旧界面路径不匹配、自动展示后多余 Escape 引起暂停等失败。修正的是测试等待与原界面操作：按原继续游戏恢复、等待原对白/动画完成、在原锁界面输入密码、用原背包菜单组合物品，然后继续同一自然流程。事件记录中的这些旧 FAIL 不是隐去的证据，也不意味着修改了剧情来强行通过。最终范围审计和原完成标记的自然获得记录用于核对这一点。

## 8 尚需人工判断与后续音频阶段

| 项目 | 当前完成边界 | 待判断或后续处理 |
| --- | --- | --- |
| Loop | 完整母曲保留，主题与探索 Cue 循环配置正确；主题播放器实测绕回 | 尚未做最终无缝拼接、三门 20 至 40 秒剪辑或全曲循环接缝听审 |
| Fade | 普通过渡约 1.5 秒，结束主题淡入 1.8 秒，零增益后停止；可配置 | 各剧情情绪下的曲线、留白和体感节奏需试听精修 |
| 三门定位 | 同母曲同步启动，原三位置不变，音量/低通/距离/声像随阶段更新 | 耳机和扬声器辨向、远近等级、是否启用空间化与混响需人工判断 |
| 初始音量 | 通过 Cue 调节，没有破坏性 Normalize；有限输出样本未削波 | 人声主题、乐器曲、原 SFX 的主观响度关系，Dialogue Duck 是否合适需人工试听 |
| Scene06 | 暂用有人陪你走，可直接换 Cue，原流程走通 | 车内及各闪回最终配乐决策未锁死 |
| Scene07 和 End Page | 医院与最后对白不进普通曲；最后一句结束后主题从头淡入 | 原剧情声音的后期、留白长度与结束页停留体验留待后续 |
| 正式构建 | 冷启动、菜单、新游戏、原开场、静止与退出实测正常 | 没有第二遍独立播放器全 Demo 回放；没有在独立播放器里进行音频主观听审 |

本轮不制作最终 Mastering、Steam 最终压缩、OST 或新剧情音效。新增素材与所有本轮手动生成输出均在 D 盘；Unity 自身运行日志仍使用系统原有位置，没有为此改变用户系统配置。

## 9 文件保存与提交准备

保留 15 套短音频、画面与状态，共约 62.72 MiB；没有生成长视频。本轮清理了 38 个废弃生成文件，释放 44.75 MiB，没有删用户文档或原始音乐。

正式构建文件约 2.73 GiB；本轮外部工作目录在报告生成前约 2.85 GiB，其中大部分是这个可直接运行的构建。原文件备份约 27.82 MiB；完整事件与状态记录保留用于追溯，不进入游戏资源。

项目内交付文档：本报告、文件清单、英文提交说明和精简 Evidence。大体积 WAV、PNG、输入记录、可运行构建与回退备份在 [D 盘本轮工作目录](<D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0>)，文件清单把项目文件与外部证据分开列出，避免把整套构建和录制意外纳入 Git。

如人工验收后决定留档，可使用 [英文 Summary 和 Description](<D:/GitHub Desktop/ProjectMystery/ProjectMystery/Documentation/AudioPass0/GitHub_Commit_Text.txt>)。本轮未自动提交或推送，且没有改变 HEAD。报告中的“功能通过”与“人工听感待确认”保持分开，最终体验验收由你决定。
