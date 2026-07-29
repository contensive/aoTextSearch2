
using Contensive.BaseClasses;
using Contensive.DesignBlockBase.Models.Db;
using Contensive.Models.Db;
using System;

namespace Contensive.Addons.TextSearch.Models.Db {
    public class TextSearchWidgetModel : SettingsBaseModel {
        //
        // ====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; private set; } = new DbBaseTableMetadataModel("Text Search Widgets", "dbTextSearchWidgets", "default", false);
        //
        // ====================================================================================================
        // -- instance properties (migrated from addon argument list)
        public int searchResultsPageId { get; set; }
        public bool includeSearchResults { get; set; }
        public bool includeImageInResults { get; set; }
        public bool includeSampleText { get; set; }
        public bool includeSectionOptions { get; set; }
        public bool includeTopicOptions { get; set; }
        //
        public static new TextSearchWidgetModel createOrAddSettings(CPBaseClass cp, string settingsGuid, string recordNameSuffix) {
            var result = create<TextSearchWidgetModel>(cp, settingsGuid);
            if (result is not null) { return result; }
            //
            // -- create default content
            result = addDefault<TextSearchWidgetModel>(cp);
            result.name = $"{tableMetadata.contentName} {result.id}, created {DateTime.Now}" + (string.IsNullOrEmpty(recordNameSuffix) ? "" : ", " + recordNameSuffix);
            result.ccguid = settingsGuid;
            result.includeSearchResults = true;
            result.includeImageInResults = true;
            result.includeSampleText = true;
            result.save(cp);
            //
            // -- track the last modified date
            cp.Content.LatestContentModifiedDate.Track(result.modifiedDate);
            //
            return result;
        }
    }
}
