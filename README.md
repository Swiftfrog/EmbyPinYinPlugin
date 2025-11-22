# PinyinSeek

**Emby Plugin** 

> **拼音排序与搜索插件**

PinyinSeek 实现中文媒体项目的排序和搜索体验。

通过将中文标题转换为拼音首字母，实现 Emby 的排序和搜索功能，改善中文媒体库的浏览效果。

> 多音字问题是在太难，能力有限，解决不了。例如：谍影重重 => DYZZ

![PinyinSeekLogo](https://raw.githubusercontent.com/Swiftfrog/swiftfrog.github.io/master/PinyinSeekLogo.png)

## 功能特性

*   **拼音排序 (Pinyin Sort):** 自动实现中文标题的拼音首字母排序，使中文标题能够按照字母顺序正确排列。
*   **拼音搜索 (Pinyin Search):** 在项目的 `OriginalTitle` 字段添加 `#拼音`，实现对拼音的搜索。
*   **智能更新:** 开启后，仅在 `SortName` 为空时填充拼音，以保留用户手动设置的自定义排序。
*   **批量处理:** 扫描整个媒体库，为所有中文标题批量应用拼音排序和搜索标签。
*   **恢复处理:** 扫描整个媒体库，恢复到默认状态。

## 安装

1.  下载 `PinyinSeek.dll` 。
2.  `PinyinSeek.dll` 文件放入 Emby 服务器的插件目录（通常位于 `config/plugins`）。
3.  重启 Emby 服务器。
4.  `设置` -> `插件` -> `已安装` 中找到 "PinyinSeek" 插件并设置启用。

## 使用

*   **即时处理:** 添加或者刷新媒体，自动更新中文标题的排序和搜索。
*   **批量处理:** 手动刷新整个媒体库的中文标题排序和搜索。也可以添加计划任务。
*   **批量恢复:** 执行计划任务，恢复到默认状态。

## 开源与贡献

- **项目地址**：https://github.com/Swiftfrog/EverMedia  
- **贡献**：欢迎提交 Issue / Pull Request  
- **许可证**：[![License: GPL v3](https://img.shields.io/badge/License-GPL%20v3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0) 任何基于本项目代码的分发（包括商业用途）**必须以相同许可证开源全部源代码**。