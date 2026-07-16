// PinyinProvider.cs
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Threading;
using System.Threading.Tasks;
using System;
using PinyinSeek.Utils;

namespace PinyinSeek.Providers;

// =============== 基础提供者类（共享逻辑）===============
public abstract class BasePinyinProvider<T> : ICustomMetadataProvider<T>, IHasOrder
    where T : BaseItem
{
    protected readonly ILogger _logger;
    protected readonly string _typeName;

    public BasePinyinProvider(ILogger logger, string typeName)
    {
        _logger = logger;
        _typeName = typeName;
    }

    public int Order => 0;
    public string Name => $"Pinyin Sort & Search Provider ({_typeName})";

    public async Task<ItemUpdateType> FetchAsync(
        MetadataResult<T> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var item = itemResult.Item;
        var nameToProcess = item.Name;

        if (string.IsNullOrEmpty(nameToProcess))
        {
            _logger.Debug("[PinyinSeek] {0}: 名称为空，跳过处理。Item ID: {1}", _typeName, item.Id);
            return ItemUpdateType.None;
        }

        if (!PinyinHelper.ContainsChinese(nameToProcess))
        {
            return ItemUpdateType.None;
        }

        string pinyinInitials = await Task.Run(() => PinyinHelper.GetPinyinInitials(nameToProcess), cancellationToken);
        if (string.IsNullOrEmpty(pinyinInitials))
        {
            return ItemUpdateType.None;
        }

        var pinyinUpper = pinyinInitials.ToUpper();
        bool updated = false;

        // --- SortName 更新 ---
        if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
        {
            item.SetSortNameDirect(pinyinUpper);
            PinyinProviderHelper.SafeAddLockedField(item, MetadataFields.SortName);
            updated = true;
            _logger.Debug("[PinyinSeek] {0}: 已更新 SortName 为 {1}。名称: {2}, ID: {3}", _typeName, pinyinUpper, item.Name, item.Id);
        }

        // --- OriginalTitle 拼音搜索标签 ---
        if (config.EnablePinyinSearch)
        {
            var currentOT = item.OriginalTitle ?? string.Empty;
            var tag = $" #{pinyinUpper}";
            if (!currentOT.Contains(tag))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                updated = true;
                _logger.Debug("[PinyinSeek] {0}: 已更新 OriginalTitle。新值: {1}, ID: {2}", _typeName, item.OriginalTitle, item.Id);
            }
        }

        return updated ? ItemUpdateType.MetadataEdit : ItemUpdateType.None;
    }
}

// =============== Movie Provider ===============
public class PinyinProviderMovie : BasePinyinProvider<Movie>
{
    public PinyinProviderMovie(ILogger logger) : base(logger, "Movie") { }
}

// =============== Series Provider ===============
public class PinyinProviderSeries : BasePinyinProvider<Series>
{
    public PinyinProviderSeries(ILogger logger) : base(logger, "Series") { }
}

// =============== BoxSet Provider ===============
public class PinyinProviderBoxSet : BasePinyinProvider<BoxSet>
{
    public PinyinProviderBoxSet(ILogger logger) : base(logger, "BoxSet") { }
}