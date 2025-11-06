// PinyinSortProvider.cs
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging; // ✅ ILogger
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;
using PinYinSort.Utils;

namespace PinYinSort.Providers;

// =============== 基础 Provider 抽象（可选，但避免重复）==============
// 为简化，我们直接在每个类中注入 ILogger（Emby 不支持 Provider 基类依赖注入）

// =============== Movie Provider ===============
public class PinYinSortProviderMovie : ICustomMetadataProvider<Movie>, IHasOrder
{
    private readonly ILogger _logger;

    public PinYinSortProviderMovie(ILogger logger)
    {
        _logger = logger;
    }

    public string Name => "Pinyin Sort & Search Provider (Movie)";
    public int Order => 0;

    public async Task<ItemUpdateType> FetchAsync(
        MetadataResult<Movie> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var item = itemResult.Item;
        var nameToProcess = item.Name;

        if (string.IsNullOrEmpty(nameToProcess))
        {
            _logger.Debug("[PinYinSort] Movie: 名称为空，跳过处理。Item ID: {0}", item.Id);
            return ItemUpdateType.None;
        }

        if (!PinyinHelper.ContainsChinese(nameToProcess))
        {
            _logger.Debug("[PinYinSort] Movie: 不含中文，跳过处理。名称: {0}, ID: {1}", nameToProcess, item.Id);
            return ItemUpdateType.None;
        }

        string pinyinInitials = await Task.Run(() => PinyinHelper.GetPinyinInitials(nameToProcess), cancellationToken);
        if (string.IsNullOrEmpty(pinyinInitials))
        {
            _logger.Debug("[PinYinSort] Movie: 拼音计算失败，跳过。名称: {0}, ID: {1}", nameToProcess, item.Id);
            return ItemUpdateType.None;
        }

        var pinyinUpper = pinyinInitials.ToUpper();
        bool updated = false;

        if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
        {
            item.SetSortNameDirect(pinyinUpper);
            PinyinProviderHelper.SafeAddLockedField(item, MetadataFields.SortName);
            updated = true;
            _logger.Debug("[PinYinSort] Movie: 已更新 SortName 为 {0}。名称: {1}, ID: {2}", pinyinUpper, item.Name, item.Id);
        }
        else
        {
            _logger.Debug("[PinYinSort] Movie: SortName 无需更新。当前: {0}, 期望: {1}, 名称: {2}, ID: {3}",
                item.SortName, pinyinUpper, item.Name, item.Id);
        }

        if (config.EnablePinyinSearch)
        {
            var currentOT = item.OriginalTitle ?? string.Empty;
            var tag = $" #{pinyinUpper}";
            if (!currentOT.Contains(tag))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                updated = true;
                _logger.Debug("[PinYinSort] Movie: 已更新 OriginalTitle。新值: {0}, ID: {1}", item.OriginalTitle, item.Id);
            }
            else
            {
                _logger.Debug("[PinYinSort] Movie: OriginalTitle 已包含拼音标签，跳过。ID: {0}", item.Id);
            }
        }

        return updated ? ItemUpdateType.MetadataEdit : ItemUpdateType.None;
    }
}

// =============== Series Provider ===============
public class PinYinSortProviderSeries : ICustomMetadataProvider<Series>, IHasOrder
{
    private readonly ILogger _logger;

    public PinYinSortProviderSeries(ILogger logger)
    {
        _logger = logger;
    }

    public string Name => "Pinyin Sort & Search Provider (Series)";
    public int Order => 0;

    public async Task<ItemUpdateType> FetchAsync(
        MetadataResult<Series> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var item = itemResult.Item;
        var nameToProcess = item.Name;

        if (string.IsNullOrEmpty(nameToProcess))
        {
            _logger.Debug("[PinYinSort] Series: 名称为空，跳过处理。Item ID: {0}", item.Id);
            return ItemUpdateType.None;
        }

        if (!PinyinHelper.ContainsChinese(nameToProcess))
        {
            _logger.Debug("[PinYinSort] Series: 不含中文，跳过处理。名称: {0}, ID: {1}", nameToProcess, item.Id);
            return ItemUpdateType.None;
        }

        string pinyinInitials = await Task.Run(() => PinyinHelper.GetPinyinInitials(nameToProcess), cancellationToken);
        if (string.IsNullOrEmpty(pinyinInitials))
        {
            _logger.Debug("[PinYinSort] Series: 拼音计算失败，跳过。名称: {0}, ID: {1}", nameToProcess, item.Id);
            return ItemUpdateType.None;
        }

        var pinyinUpper = pinyinInitials.ToUpper();
        bool updated = false;

        if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
        {
            item.SetSortNameDirect(pinyinUpper);
            PinyinProviderHelper.SafeAddLockedField(item, MetadataFields.SortName);
            updated = true;
            _logger.Debug("[PinYinSort] Series: 已更新 SortName 为 {0}。名称: {1}, ID: {2}", pinyinUpper, item.Name, item.Id);
        }
        else
        {
            _logger.Debug("[PinYinSort] Series: SortName 无需更新。当前: {0}, 期望: {1}, 名称: {2}, ID: {3}",
                item.SortName, pinyinUpper, item.Name, item.Id);
        }

        if (config.EnablePinyinSearch)
        {
            var currentOT = item.OriginalTitle ?? string.Empty;
            var tag = $" #{pinyinUpper}";
            if (!currentOT.Contains(tag))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                updated = true;
                _logger.Debug("[PinYinSort] Series: 已更新 OriginalTitle。新值: {0}, ID: {1}", item.OriginalTitle, item.Id);
            }
            else
            {
                _logger.Debug("[PinYinSort] Series: OriginalTitle 已包含拼音标签，跳过。ID: {0}", item.Id);
            }
        }

        return updated ? ItemUpdateType.MetadataEdit : ItemUpdateType.None;
    }
}

// =============== BoxSet Provider ===============
public class PinYinSortProviderBoxSet : ICustomMetadataProvider<BoxSet>, IHasOrder
{
    private readonly ILogger _logger;

    public PinYinSortProviderBoxSet(ILogger logger)
    {
        _logger = logger;
    }

    public string Name => "Pinyin Sort & Search Provider (BoxSet)";
    public int Order => 0;

    public async Task<ItemUpdateType> FetchAsync(
        MetadataResult<BoxSet> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var item = itemResult.Item;
        var nameToProcess = item.Name;

        if (string.IsNullOrEmpty(nameToProcess))
        {
            _logger.Debug("[PinYinSort] BoxSet: 名称为空，跳过处理。Item ID: {0}", item.Id);
            return ItemUpdateType.None;
        }

        if (!PinyinHelper.ContainsChinese(nameToProcess))
        {
            _logger.Debug("[PinYinSort] BoxSet: 不含中文，跳过处理。名称: {0}, ID: {1}", nameToProcess, item.Id);
            return ItemUpdateType.None;
        }

        string pinyinInitials = await Task.Run(() => PinyinHelper.GetPinyinInitials(nameToProcess), cancellationToken);
        if (string.IsNullOrEmpty(pinyinInitials))
        {
            _logger.Debug("[PinYinSort] BoxSet: 拼音计算失败，跳过。名称: {0}, ID: {1}", nameToProcess, item.Id);
            return ItemUpdateType.None;
        }

        var pinyinUpper = pinyinInitials.ToUpper();
        bool updated = false;

        if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
        {
            item.SetSortNameDirect(pinyinUpper);
            PinyinProviderHelper.SafeAddLockedField(item, MetadataFields.SortName);
            updated = true;
            _logger.Debug("[PinYinSort] BoxSet: 已更新 SortName 为 {0}。名称: {1}, ID: {2}", pinyinUpper, item.Name, item.Id);
        }
        else
        {
            _logger.Debug("[PinYinSort] BoxSet: SortName 无需更新。当前: {0}, 期望: {1}, 名称: {2}, ID: {3}",
                item.SortName, pinyinUpper, item.Name, item.Id);
        }

        if (config.EnablePinyinSearch)
        {
            var currentOT = item.OriginalTitle ?? string.Empty;
            var tag = $" #{pinyinUpper}";
            if (!currentOT.Contains(tag))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                updated = true;
                _logger.Debug("[PinYinSort] BoxSet: 已更新 OriginalTitle。新值: {0}, ID: {1}", item.OriginalTitle, item.Id);
            }
            else
            {
                _logger.Debug("[PinYinSort] BoxSet: OriginalTitle 已包含拼音标签，跳过。ID: {0}", item.Id);
            }
        }

        return updated ? ItemUpdateType.MetadataEdit : ItemUpdateType.None;
    }
}