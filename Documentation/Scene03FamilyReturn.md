# Scene03 Stage 4B：家庭合影与成年返回

## 合影

`Scene03FamilySequence` 放在 Scene03Chapter 上。沿用独立 LivingRoomInterior 和现有 RoomTransition 门的完成事件。河堤返回后，玩家先自由走到客厅门，进门对白结束后主动 `[E] 放置相机`。电视柜旁的 FamilyCameraPlacement 只负责转发交互。

机械定时器、人物站位、男孩从画面外跑到边缘、肖鹏小幅让位、快门白闪、干净定格 2.6 秒依次执行。家庭相机使用固定构图，不进入超自然 CameraMode。对白仅称“男孩”，未制作真实姓名音轨。

WhiteSceneTransition 持有单独 SceneTransition 控制锁和白色遮罩，跨场景加载后淡出。原相机物品保留；童年临时物品按既有规则清理。

## Scene02 返回客厅

`Scene02ReturnChapter / Scene02ReturnController` 绑定冷色 LivingRoomInterior_Return。布局复制正常客厅，相对家具位置不变，整体平移 150 个单位避开现有主卧、次卧和长走廊空间。

- 桌子 x=350，沙发 x=341.5，电视柜 x=359；成年落点 x=346.5，在桌和沙发之间。
- 内门 x=338，通过既有 RoomTransition 回到原一楼客厅门左侧 x=-3.8。
- 原 LockedLivingRoom_Placeholder 使用通用 RoomTransition 的可选 StoryFlag 门锁；未开门前保留原“门打不开”对白，从内部开门后永久可双向通行。
- 无 Scene03Completed 时内部空间关闭，不改变初访 Scene02 的流程。
- 桌上文件碎片2进入普通 Items，可组合；描述为未完整的文件碎片，不扩写未定背景。
- 相机先生实体卡只在童年获卡时出现，成年调查后隐藏，不再次 AddFile。
- 匿名引路人移至二楼次卧右侧，沿用原 DialogueInteractable。

## 文件与油灯

`F_S02_CleanFamilyPhoto` 与原污渍照片共用 `file_stained_photo` ID，FileArchiveManager 替换运行时列表中原位置；原 ScriptableObject 不改写，按 ID 防重复。预览图为当前四人占位合影截图，InventoryUI 保持宽高比。简介明确 7 月 15 日生日。

原 DeskCloseupController 继续使用四位滚轮与 0715。生日未知时仍拒绝；已知后获得原 `I_S02_OilLamp`，ID `key_oil_lamp` 保留，分类为普通 Items、非永久。本轮通过黑雾后保留，不决定后续消耗。

黑雾使用 Scene02ReturnInteractable：无油灯沿用原调查；有油灯变为 `[E] 进入`，调用 Scene04TransitionBridge 和原黑色淡出。targetSceneName 留空是当前批准的黑屏终点，未制作 Scene04 正文。

## 接线与维护

所有新增状态追加于 StoryFlag 末尾。RoomTransition 新门锁默认关闭，原楼梯/房间保持兼容。DistortedCorridorController 在 Scene03Completed 后停止旧的楼梯禁用与重复恢复逻辑。

合影对白、机械计时、雨声、全家福图片均为 Inspector 可替换资产。新音轨是占位音，不代表最终正式吉他编曲或原 Scene01 雨录音。无需更改 Inspector 即可从 Scene01 正常走完整主线。

临时自动验证器归档在工作台的 `work/stage4b`，验收后从 Assets 移除，不随游戏运行。
