
using System;
using Contensive.BaseClasses;

namespace Contensive.Addons.TextSearch {
    public class DiagnosticAddon : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                string result = "ok, Text Search";
                //
                // -- check if full text search is enabled but the catalog is not configured
                bool useFullText = cp.Site.GetBoolean(Constants.sitePropertyUseFullTextSearch);
                if (useFullText) {
                    try {
                        string sql = "SELECT OBJECTPROPERTY(OBJECT_ID('ccSpiderDocs'), 'TableFullTextCatalogId') AS CatalogId";
                        System.Data.DataTable dt = cp.Db.ExecuteQuery(sql);
                        if (dt.Rows.Count > 0) {
                            object catalogIdObj = dt.Rows[0]["CatalogId"];
                            if (catalogIdObj == null || catalogIdObj == DBNull.Value || Convert.ToInt32(catalogIdObj) <= 0) {
                                result = "ERROR, Full Text Search is enabled in Text Search Settings but no full-text catalog is configured on the ccSpiderDocs table. Create a full-text index on ccSpiderDocs.BodyText or disable the setting.";
                            }
                        }
                    } catch (Exception) {
                        result = "WARNING, Full Text Search is enabled but the full-text catalog status could not be verified. Ensure a full-text index exists on ccSpiderDocs.BodyText.";
                    }
                }
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
