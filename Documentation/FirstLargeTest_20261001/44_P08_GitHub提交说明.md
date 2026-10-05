# P08 GitHub Desktop archive text

Status: P08 implementation and planned internal validation are complete; awaiting the user's manual art and experience review. No commit or push has been created by this task.

## Summary

Complete P08 full-demo regression and final asset audit

## Description

Finish the original full-demo input flow through childhood side quests, the maze and guitar sequence, family photography, both school worlds, the chase, the continuous home return, and the ending.

Fix the childhood return gate's interaction layer and release the resident school's inventory policy after returning home. Keep authored movement, item acquisition, story order, cameras, and pause rules intact.

Audit all 121 requirements against the original feedback document and retained stage evidence. Validate opposite side-quest order, real outfit selection, long-file reading, six rendered aspect/resolution layouts, and four naturally produced saves in a separate process. Remove 21 confirmed unused image assets and their metadata, and index the current production artwork on D:\游戏设计 without removing user documents.

The final Windows release build succeeded with 0 errors and 487 recorded warnings. Verify the actual release window across three launches: volume persistence and restoration, four native display modes, original new-game item restrictions, pause, quit cancellation, and confirmed process exit. Preserve both existing user save files unchanged. Retain normal-speed review clips and explicit before/after records. Editor QA remains opt-in, and the temporary cold-read startup adjustment was restored before the release build. Final human art approval remains separate from internal validation.

## Include

- Assets/Scenes/Scene03_Childhood.unity
- Assets/Scripts/School/SchoolSecurityFinale.cs
- The 21 confirmed unused PNG deletions and their 21 .meta deletions, listed in P08Evidence/checks/cleanup-completed.json
- Assets/Editor/P08FullReplay.cs, P08ReviewRecorder.cs, P08ProjectAudit.cs, P08GlobalAudit.cs and their metadata
- Documentation/FirstLargeTest_20261001/P08Evidence
- Updated requirement ledger, progress README, and reports 42-44

## Keep outside the commit

The D-drive build_release directory, raw recordings, diagnostic stores, and rollback/cache files are outside the repository. User documents and approved current art are not deletion targets.

Commit only after reviewing the final report and preview. Push remains a separate manual action in GitHub Desktop.
