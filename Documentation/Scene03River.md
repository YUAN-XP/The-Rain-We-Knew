# Scene03 河堤与吉他节奏游戏

河堤仍属于 Scene03_Childhood，接在同 Scene 迷宫出口后。男孩身上的 RiverTalkInteractable 调用 Scene03Chapter 对象上的 Scene03RiverSequence；章节控制器保持激活，因此隐藏河堤并返回家属院不会中断黑屏退去和释放控制锁。

## Inspector 配置

- Scene03RiverSequence：四个 DialogueData、GuitarRhythmGame、Music AudioSource、Full Birthday Song、男孩、两处座位、赵炳程、离开点、幼年视觉根、四条腿、鱼竿。
- GuitarRhythmGame：四个 RhythmPhraseData、Chapter、Instrument AudioSource、Approach Seconds。
- RhythmPhrase1–4.asset：持续和弦依次 LeftArrow / RightArrow / RightArrow / LeftArrow；主要音符数量 6 / 6 / 7 / 6；各段最后一个音符必须是 Space。
- 每个音符可编辑 key、time（相对本段开始的秒数）、tone。时间严格递增，Space 与最后一个字母音符间保留足够空隙，避免多键冲突。
- 当前 Perfect 窗口为 ±0.13 秒，Good 为 ±0.30 秒。判定按实际输入轨寻找对应音符，不让另一轨的相邻音符抢占判定。
- `Assets/Audio/Stage4BPlaceholder/HappyBirthday_Guitar_Placeholder.wav` 是 26.9 秒合成拨弦占位音频。替换 Full Birthday Song 可更换完整编曲；单音反馈通过谱面各音符的 Tone 更换。

## 输入与流程

进入对白与演出持有 Cutscene 锁，音游另持有 CloseUp 锁；移动、奔跑、世界 E、相机和背包不能在音游中并行操作。UI 初始按 E 开始。

音符从右向左经过判定线。必须持续按住当前箭头；按下字母或 Space 的时刻，未按正确箭头或同时按反向箭头均不合格。输入使用真实 Input.GetKey / Input.GetKeyDown，不用一次按下锁定和弦。每次只要求当前箭头 + 一个音符键。段尾 Space 在字母音符结束并释放后操作。

任何 Miss 都让本段继续结束，然后仅重来本段。已经通过的段落保留。切出应用后重新聚焦，会重新准备当前段，不丢失已通过段落。

四段完成后 UI 淡出，镜头慢慢拉远并播放完整歌曲。前八秒不显示离开提示，随后淡入 `[E] 离开`；E 或音频自然结束都进入夸奖对白。赵炳程传话后两人离开，整理鱼竿并独白，黑屏直接回家属院，恢复幼年角色自由控制。

## 状态与范围

追加 Scene03RhythmCompleted、Scene03RiverSequenceCompleted、Scene03ReturnedHome。保留旧枚举值。

重新载入且节奏已完成时，可在男孩处继续歌曲与后续对白。若河堤独白已经完成但尚未完成回家遮罩，重新载入时恢复回家过渡。这里只使用存活 PersistentRuntime 的会话状态，未实现磁盘存档。

本检查点没有实现傍晚客厅合影、成年返回、0715 小油灯或 Scene04 Bridge，分别留给 4B-3 / 4B-4。

## 验证边界

自动 Play Mode 已覆盖数据、Miss、Good/Perfect、前段保留、控制互斥、歌曲八秒门槛、自然结束、黑屏回家、重力与单一活动角色。真实持续箭头和键盘按键冲突必须单独由 Game View 试玩记录验证；自动调用判定方法不能替代该项。
