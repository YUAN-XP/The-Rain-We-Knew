# P07 GitHub Desktop 提交说明

P07制作与内部检查完成，等待用户人工审计。当前本地基线103a1dea8e00ddd5c39fb81e1aa0a2936978f419；本轮尚未Commit/Push。审计通过后复制下面英文，不需要逐项勾选长清单。

## Summary

Complete P07 menus, opening, quest display, and acquisition presentation

## Description

Add painted start and completion menus, shared audio/video settings and credits, four-slot save/load presentation, and pause confirmations. Choose three or five start-menu actions from valid saves, continue the newest valid slot, and reset a new session without overwriting existing save files. Preserve free-roam-only saving and the existing focus-pause rules.

Implement the paired bilingual opening quote, ink transition, portrait-free monologue, and grounded native-rig get-up sequence. Connect all 24 requested quest nodes through semantic events, including hidden intervals and instant load restoration. Queue acquisition notices and first-item/file close-ups, keep outfits toast-only, and notify successful combinations after transaction commit. Translate legacy display strings without changing item identities or saved content.

Validation: 474 final isolated Play Mode assertions and 25 production rule checks passed with 0 failures. Native Windows testing verified actual resolution selection at 1280x720, 1920x1080 and 1920x1200, wheel/scrollbar input, volume and resolution persistence after restart, and two real menu exits. The development build succeeded with 0 errors and 486 documented ConvGeneric shader warnings. Restore test preferences, preserve both real save-slot hashes and timestamps, remove replay fixtures, and return the editor to clean Scene01.

Keep the P06 art catalog, Scene02, Scene03 and the ending scene byte-for-byte; Scene01/Scene04 serialized changes are display text only. Final review media, requirement updates, source hashes and verification limits are included. User art and experience review must precede archive; a fresh full-Demo walkthrough, wider device coverage and release validation remain in P08.

## 本轮提交范围

P07Evidence/checks/commit-scope.json列出最终本轮路径。包含生产UI/任务/获得/暂停/开场接入、两张菜单图及meta、中文字体字形、少量可见文字、Editor复测工具、34项台账整合、README、38–41号文档和最终P07Evidence。

不提交D盘约2.8GiB测试构建、工作区源录像副本、原始用户DOCX、玩家/隔离存档、Library、Temp、Logs、Assets/_Recovery或其他阶段无关文件。asset与meta一起保留。请先审计实际预览；有反馈时先修复/复测，刷新版本哈希再使用这段提交文案。
