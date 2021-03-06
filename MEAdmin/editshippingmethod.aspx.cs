// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Data;
using System.Globalization;
using System.Text;
using AspDotNetStorefrontCore;
using System.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Web;
using System.Web.UI;

namespace AspDotNetStorefrontAdmin
{
    /// <summary>
    /// Summary description for editshippingmethod
    /// </summary>
    public partial class editshippingmethod : AdminPageBase
    {

        int ShippingMethodID;
        private int m_storeid;
        public int StoreId
        {
            get { return m_storeid; }
            set { m_storeid = value; }
        }

        private List<Store> m_stores;
        public List<Store> Stores
        {
            get { return m_stores; }
            set { m_stores = value; }
        }

        private void InitializeStores()
        {
            Stores = Store.GetStoreList();
        }

        protected override void OnInit(EventArgs e)
        {
            InitializeStores();
            base.OnInit(e);
        }

        private Guid ShippingMethodGuid
        {get;set;}

        private SqlCommand _updateCommand;
        private SqlCommand updateCommand
        {
            get
            {
                if (_updateCommand != null)
                {
                    return _updateCommand;
                }

                #region updateSQL
                
                
                _updateCommand = new SqlCommand(
                    @"
            
                    IF NOT EXISTS(SELECT ShippingMethodGUID FROM ShippingMethod WHERE ShippingMethodID = @ID)
                    BEGIN
                     INSERT INTO ShippingMethod(ShippingMethodGUID, ShipRushTemplate, [Name]) 
                     VALUES (@GUID, @ShipRushTemplate, @Name)
                     SELECT @ID = ShippingMethodID FROM ShippingMethod WHERE ShippingMethodGUID = @GUID
                    END
                    ELSE
                    BEGIN
                     DECLARE @ISRtShipping tinyint
                     SELECT @IsRtShipping  = IsRTShipping FROM ShippingMethod WHERE ShippingMethodID = @ID
                     UPDATE ShippingMethod SET
                        [Name] = CASE WHEN @IsRtShipping = 1 Then [Name] ELSE @Name END,
                        ShipRushTemplate = @ShipRushTemplate
                     WHERE 
                        ShippingMethodID = @ID
                    END
                    "
                    );
                #endregion

                SqlParameter prmID = new SqlParameter("@ID", SqlDbType.Int);
                prmID.Value = DBNull.Value;
                prmID.Direction = ParameterDirection.InputOutput;

                _updateCommand.Parameters.AddRange(new SqlParameter[] {
                    new SqlParameter("@GUID", ShippingMethodGuid),
                    new SqlParameter("@ShipRushTemplate", DBNull.Value),
                    new SqlParameter("@Name", AppLogic.FormLocaleXml("Name")),
                    prmID
                });
                return _updateCommand;
            }
        }

        protected void Page_Load(object sender, System.EventArgs e)
        {
            Response.CacheControl = "private";
            Response.Expires = 0;
            Response.AddHeader("pragma", "no-cache");


            ShippingMethodID = 0;
            Boolean addedMethod = false;

            StoreId = CommonLogic.FormNativeInt("StoreId");
            if (StoreId == 0)
            {
                StoreId = Request.QueryStringNativeInt("StoreId");
            }
            if (StoreId == 0)
            {
                var stores = Store.GetStoreList();
                var defaultStore = stores.FirstOrDefault(store => store.IsDefault);
                StoreId = defaultStore.StoreID;
            }

            if (CommonLogic.QueryStringCanBeDangerousContent("ShippingMethodID").Length != 0 && CommonLogic.QueryStringCanBeDangerousContent("ShippingMethodID") != "0")
            {
                Editing = true;
                ShippingMethodID = Localization.ParseUSInt(CommonLogic.QueryStringCanBeDangerousContent("ShippingMethodID"));
            }
            else
            {
                Editing = false;
            }

            if (CommonLogic.FormBool("IsSubmit"))
            {
                
                if (!Editing)
                {
                    // ok to add:
                    ShippingMethodGuid = new Guid(DB.GetNewGUID());
                    if (AppLogic.AppConfigBool("ShipRush.Enabled"))
                        updateCommand.Parameters["@ShipRushTemplate"].Value = CommonLogic.FormCanBeDangerousContent("ShipRushTemplate");
                    DB.ExecuteSQL(updateCommand);
                    ShippingMethodID = (int)updateCommand.Parameters["@ID"].Value;
                    Editing = true;
                    DataUpdated = true;
                    addedMethod = true;
                }
                else
                {
                    // ok to update:

                    if (AppLogic.AppConfigBool("ShipRush.Enabled"))
                        updateCommand.Parameters["@ShipRushTemplate"].Value = CommonLogic.FormCanBeDangerousContent("ShipRushTemplate");
                    updateCommand.Parameters["@ID"].Value = ShippingMethodID;
                    DB.ExecuteSQL(updateCommand);
                    Editing = true;
                    DataUpdated = true;
                }
                // for the store mapping
                if (Stores.Count > 1)
                {
                    DB.ExecuteSQL(string.Format("DELETE ShippingMethodStore WHERE ShippingMethodId = {0}", ShippingMethodID));
                    var chkStoreMapElementNames = Request.Form.AllKeys.Where(elem => elem.StartsWith("chkStoreMap_"));
                    foreach (string chkMap in chkStoreMapElementNames)
                    {
                        int storeId = chkMap.Split('_')[1].ToNativeInt();
                        DB.ExecuteSQL(string.Format("INSERT INTO ShippingMethodStore(StoreId, ShippingMethodId) Values({0}, {1})", storeId, ShippingMethodID));
                    }
                }
                else
                {
                    SqlParameter smID = new SqlParameter("@ShippingID", ShippingMethodID);
                    SqlParameter storeID = new SqlParameter("@StoreID", Stores[0].StoreID);
                    string sql = 
                        @"
                        IF NOT EXISTS(SELECT ShippingMethodID FROM ShippingMethodStore WHERE StoreID = @StoreID and ShippingMethodID = @ShippingID)
                        BEGIN
                            INSERT INTO ShippingMethodStore(StoreID, ShippingMethodID) VALUES (@StoreID, @ShippingID)
                        END
                        ";
                    DB.ExecuteSQL(sql,new SqlParameter[]{smID, storeID});
                }
                _updateCommand = null;
            }
            SectionTitle = "<a href=\"" + AppLogic.AdminLinkUrl("shippingmethods.aspx") + "\">" + AppLogic.GetString("admin.menu.ShippingMethods", SkinID, LocaleSetting) + "</a> - " + AppLogic.GetString("admin.editshippingmethod.ManageShippingMethods", SkinID, LocaleSetting) + "";
            if (addedMethod)
            {
                Response.Redirect("shippingmethods.aspx", true);
                return;
            }
            Render();
        }

        private void Render()
        {
            StringBuilder writer = new StringBuilder();
            using (SqlConnection dbconn = DB.dbConn())
            {
                dbconn.Open();
                using (IDataReader rs = DB.GetRS("select * from ShippingMethod   with (NOLOCK)  where ShippingMethodID=" + ShippingMethodID.ToString(), dbconn))
                {
                    if (rs.Read())
                    {
                        Editing = true;
                    }

                    if (ErrorMsg.Length != 0)
                    {
                        writer.Append("<p><b><font color=red>" + ErrorMsg + "</font></b></p>\n");
                    }
                    if (DataUpdated)
                    {
                        writer.Append("<p align=\"left\"><b><font color=blue>" + AppLogic.GetString("admin.editCreditCard.Updated", SkinID, LocaleSetting) + "</font></b></p>\n");
                    }

                    if (AppLogic.NumLocaleSettingsInstalled() > 1)
                    {
                        writer.Append(CommonLogic.ReadFile("jscripts/tabs.js", true));
                    }

                    if (ErrorMsg.Length == 0)
                    {
                        if (Editing)
                        {
                            writer.Append("<b>" + String.Format(AppLogic.GetString("admin.editshippingmethod.EditingShippingMethod", SkinID, LocaleSetting),DB.RSFieldByLocale(rs, "Name", LocaleSetting),DB.RSFieldInt(rs, "ShippingMethodID").ToString()) + "<br/><br/></b>\n");
                        }
                        else
                        {
                            writer.Append("<div style=\"height:17;padding-top:3px;\" class=\"tablenormal\">" + AppLogic.GetString("admin.editshippingmethod.AddNewShippingMethod", SkinID, LocaleSetting) + ":</div><br/></b>\n");
                        }

                        writer.Append("<script type=\"text/javascript\">\n");
                        writer.Append("function ShippingMethodForm_Validator(theForm)\n");
                        writer.Append("{\n");
                        writer.Append("submitonce(theForm);\n");
                        writer.Append("return (true);\n");
                        writer.Append("}\n");
                        writer.Append("</script>\n");

                        writer.Append("<p>" + AppLogic.GetString("admin.editshippingmethod.ShippingMethodInfo", SkinID, LocaleSetting) + "</p>\n");
                        writer.Append("<form action=\"" + AppLogic.AdminLinkUrl("editshippingmethod.aspx") + "?ShippingMethodID=" + ShippingMethodID.ToString() + "&edit=" + Editing.ToString() + "\" method=\"post\" id=\"ShippingMethodForm\" name=\"ShippingMethodForm\" onsubmit=\"return (validateForm(this) && ShippingMethodForm_Validator(this))\" onReset=\"return confirm('" + AppLogic.GetString("admin.common.ResetAllFieldsPrompt", SkinID, LocaleSetting) + "');\">\n");
                        writer.Append("<input type=\"hidden\" name=\"IsSubmit\" value=\"true\">\n");
                        writer.Append("<input type=\"hidden\" name=\"StoreId\" value=\"" + StoreId.ToString()  + "\">\n");
                        writer.Append("<table width=\"100%\" cellpadding=\"4\" cellspacing=\"0\">\n");
                        writer.Append("              <tr valign=\"middle\">\n");
                        writer.Append("                <td width=\"100%\" colspan=\"2\" align=\"left\">\n");
                        writer.Append("                </td>\n");
                        writer.Append("              </tr>\n");
                        writer.Append("              <tr valign=\"middle\">\n");
                        writer.Append("                <td width=\"25%\" align=\"right\" valign=\"middle\">*" + AppLogic.GetString("admin.common.Name", SkinID, LocaleSetting) + ":&nbsp;&nbsp;</td>\n");
                        writer.Append("                <td align=\"left\" valign=\"top\">\n");
                        if (DB.RSFieldBool(rs, "IsRTShipping"))
                        {
                            writer.Append(DB.RSFieldByLocale(rs, "Name", LocaleSetting));
                        }
                        else
                        {
                            writer.Append(AppLogic.GetLocaleEntryFields(DB.RSField(rs, "Name"), "Name", false, true, true, AppLogic.GetString("admin.editshippingmethod.EnterShippingMethod", SkinID, LocaleSetting), 100, 30, 0, 0, false));
                        }
                        
                        writer.Append("                	</td>\n");
                        writer.Append("              </tr>\n");

                        if (AppLogic.AppConfigBool("ShipRush.Enabled"))
                        {
                            writer.Append("              <tr valign=\"middle\">\n");
                            writer.Append("                <td width=\"25%\" align=\"right\" valign=\"middle\">*" + AppLogic.GetString("admin.editshippingmethod.ShipRushTemplate", SkinID, LocaleSetting) + ":&nbsp;&nbsp;</td>\n");
                            writer.Append("                <td align=\"left\">\n");
                            writer.Append("                	<input maxLength=\"100\" size=\"50\" name=\"ShipRushTemplate\" value=\"" + CommonLogic.IIF(Editing, Server.HtmlEncode(DB.RSField(rs, "ShipRushTemplate")), "") + "\"> " + AppLogic.GetString("admin.editshippingmethod.Sample", SkinID, LocaleSetting) + "\n");
                            writer.Append("                	</td>\n");
                            writer.Append("              </tr>\n");
                        }

                        if (Stores.Count > 1)
                        {
                            List<int> mappedStoreIds = new List<int>();
                            string mappingQuery = string.Format("SELECT StoreId FROM ShippingMethodStore WITH (NOLOCK) WHERE ShippingMethodId = {0}", ShippingMethodID);
                                
                            if(Editing)
                            {
                                using (SqlConnection conn = new SqlConnection(DB.GetDBConn()))
                                {
                                    conn.Open();

                                    using (IDataReader rsMap = DB.GetRS(mappingQuery, conn))
                                    {
                                        while (rsMap.Read())
                                        {
                                            mappedStoreIds.Add(rsMap.FieldInt("StoreId"));
                                        }

                                        rsMap.Close();
                                        rsMap.Dispose();
                                    }
                                    conn.Close();
                                    conn.Dispose();
                                }
                            }

                            writer.Append("              <tr valign=\"middle\">\n");
                            writer.Append("                <td width=\"25%\" align=\"right\" valign=\"middle\">" + AppLogic.GetString("admin.editshippingmethod.MappedStores", SkinID, LocaleSetting) + ":&nbsp;&nbsp;</td>\n");
                            writer.Append("                <td align=\"left\">\n");

                            foreach (Store s in Stores)
                            {
                                bool alreadyMapped = Editing && mappedStoreIds.Contains(s.StoreID);
                                string html = string.Format("<input type=\"checkbox\" name=\"chkStoreMap_{0}\" value=\"{1}\" {2} /> {1}\n", s.StoreID, HttpUtility.HtmlEncode(s.Name), alreadyMapped ? "checked" : string.Empty);
                                writer.Append(html + "<br />");
                            }

                            writer.Append("                	</td>\n");
                            writer.Append("              </tr>\n");
                        }


                        writer.Append("<tr>\n");
                        writer.Append("<td></td><td align=\"left\" valign=\"top\"><br/>\n");

                        string backText = string.Empty;

                        if (Editing)
                        {
                            writer.Append("<input class=\"normalButtons\" type=\"submit\" value=\"" + AppLogic.GetString("admin.common.Update", SkinID, LocaleSetting) + "\" name=\"submit\">\n");
                            backText = AppLogic.GetString("admin.common.Back", SkinID, LocaleSetting);
                        }
                        else
                        {
                            writer.Append("<input type=\"submit\" class=\"normalButtons\" value=\"" + AppLogic.GetString("admin.common.AddNew", SkinID, LocaleSetting) + "\" name=\"submit\">\n");
                            backText = AppLogic.GetString("admin.common.Cancel", SkinID, LocaleSetting);                            
                        }

                        string backLink = string.Format("&nbsp;<a href=\"" + AppLogic.AdminLinkUrl("shippingmethods.aspx") + "?storeid={0}\">{1}</a>", StoreId, backText);
                        writer.Append(backLink);

                        writer.Append("        </td>\n");
                        writer.Append("      </tr>\n");
                        writer.Append("  </table>\n");
                        writer.Append("</form>\n");
                    }
                }
            }
            ltContent.Text = writer.ToString();
        }

    }
}
