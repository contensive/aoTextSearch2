
using System;
using Contensive.BaseClasses;

namespace Contensive.Addons.TextSearch {
    public class OnInstallAddon : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                cp.Layout.updateLayout(
                    Constants.guidLayoutTextSearch,
                    Constants.nameLayoutTextSearch,
                    Constants.pathFilenameLayoutTextSearch
                );
                //
                // -- if full-text search is enabled, ensure the catalog and index exist
                if (cp.Site.GetBoolean(Constants.sitePropertyUseFullTextSearch)) {
                    ensureFullTextIndex(cp);
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "TextSearch.OnInstallAddon.Execute");
            }
            return "";
        }
        //
        private static void ensureFullTextIndex(CPBaseClass cp) {
            try {
                //
                // -- create a full-text catalog if none exists
                string sqlCheckCatalog = "SELECT COUNT(*) AS cnt FROM sys.fulltext_catalogs WHERE name = 'ftCatalog'";
                System.Data.DataTable dt = cp.Db.ExecuteQuery(sqlCheckCatalog);
                if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["cnt"]) == 0) {
                    cp.Db.ExecuteNonQuery("CREATE FULLTEXT CATALOG ftCatalog AS DEFAULT");
                }
                //
                // -- create the full-text index on ccSpiderDocs.BodyText if not already indexed
                string sqlCheckIndex = "SELECT COUNT(*) AS cnt FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('ccSpiderDocs')";
                dt = cp.Db.ExecuteQuery(sqlCheckIndex);
                if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["cnt"]) == 0) {
                    //
                    // -- find the primary key index name
                    string sqlGetPK = "SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('ccSpiderDocs') AND is_primary_key = 1";
                    dt = cp.Db.ExecuteQuery(sqlGetPK);
                    if (dt.Rows.Count > 0) {
                        string pkName = dt.Rows[0]["name"].ToString();
                        cp.Db.ExecuteNonQuery($"CREATE FULLTEXT INDEX ON ccSpiderDocs(BodyText) KEY INDEX [{pkName}] ON ftCatalog WITH CHANGE_TRACKING AUTO");
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "TextSearch.OnInstallAddon.ensureFullTextIndex");
            }
        }
    }
}
