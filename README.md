# PinyinSeek
**Emby Plugin** 
> **拼音排序与搜索插件**

PinyinSeek旨在提升中文媒体项目的排序和搜索体验。

通过将中文标题转换为拼音首字母，实现 Emby 的排序和搜索功能，可以显著改善中文媒体库的浏览效果。

## 功能特性

*   **拼音排序 (Pinyin Sort):** 自动实现中文标题的拼音首字母排序，使中文标题能够按照字母顺序正确排列。
*   **拼音搜索 (Pinyin Search):** 在项目的 `OriginalTitle` 字段添加 `#拼音`，实现对拼音的搜索。
*   **智能更新:** 插件提供选项，可以选择仅在 `SortName` 为空时填充拼音，以保留用户手动设置的自定义排序。
*   **批量处理:** 计划任务扫描整个媒体库，为所有符合条件的项目批量应用拼音排序和搜索标签。
*   **恢复处理:** ~将所有修订的`SortName`和`OriginalTitle`恢复到默认状态。~ **开发ing**

## 安装

1.  从插件发布页面下载 `PinyinSeek.dll` 文件。
2.  将下载的 `PinyinSeek.dll` 文件放入 Emby 服务器的插件目录（通常位于 `config/plugins`）。
3.  重启 Emby 服务器。
4.  登录 Emby Web 管理界面，在 `设置` -> `插件` -> `已安装` 中找到 "PinyinSeek" 插件并启用。

## 配置

-   **启用拼音排序 (Enable Pinyin Sort):** 开启或关闭拼音排序功能
-   **启用拼音搜索 (Enable Pinyin Search):** 开启或关闭拼音搜索功能
-   **启用拼音排序或搜索任务 (Enable Pinyin Sort or Searching Task):** 开启或关闭全库扫描的计划任务。  **注意：** 该任务默认关闭，首次安装后建议手动开启一次并运行，以处理已有媒体库。之后可根据需要决定是否保持开启（新项目插件会自动处理，不需要手动任务处理。）。
-   **启用自定义排序 (Enable Custom Sorting):** 当开启时，插件将仅在项目的 `SortName` 为空时才应用拼音排序。如果关闭，则会覆盖任何非空的 `SortName`（即使它包含中文，插件也会将其拼音化以确保正确排序）。不理解的话，还请关闭。

## 使用

*   **即时处理:** 当您刷新单个媒体项目的元数据时，如果该项目的标题包含中文且满足配置条件，插件会自动为其设置拼音排序和搜索标签。
*   **批量处理:** 启用计划任务后，它将在每天凌晨 3 点自动运行，或您可以随时在 `设置` -> `任务` -> `PinyinSeek for Chinese` 中手动启动它来处理整个媒体库。

## 依赖

*   [MediaBrowser.Server.Core](https://www.nuget.org/packages/MediaBrowser.Server.Core)
*   [TinyPinyin](https://github.com/forhappy/TinyPinyin)

## 开源与贡献

- **项目地址**：https://github.com/Swiftfrog/EverMedia  
- **贡献**：欢迎提交 Issue / Pull Request  
- **许可证**：[![License: GPL v3](https://img.shields.io/badge/License-GPL%20v3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0) 任何基于本项目代码的分发（包括商业用途）**必须以相同许可证开源全部源代码**。
- **依赖**：Emby Server 4.9.1.80，.NET 8

---
*插件名称: PinyinSeek*
*描述: 为 Emby 添加拼音首字母，用于排序和搜索。*# PinyinSeek
Emby Plugin
Suitable for 4.9.1.x