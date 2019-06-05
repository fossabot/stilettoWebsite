// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Web;
using System.Text;
using System.Data;
using System.Globalization;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;
using AspDotNetStorefrontGateways.Processors;

namespace AspDotNetStorefrontAdmin
{
    /// <summary>
    /// Summary description for recurringrefundcancel.
    /// </summary>
    public partial class recurringrefundcancel : System.Web.UI.Page
    {
        protected void Page_Load(object sender, System.EventArgs e)
        {
            StringBuilder writer = new StringBuilder();
            Response.CacheControl = "private";
            Response.Expires = 0;
            Response.AddHeader("pragma", "no-cache");

            /****************************************************************************/
            // * WARNING TO DEVELOPERS
            // * The redirect below is a SAFETY feature.  Removing the redirect will not
            // * enable ML-only features on a lower version of AspDotNetStorefront.
            // * Attempting to do so can very easily result in a partially implemented
            // * feature, invalid or incomplete data in your DB, and other serious 
            // * conditions that will cause your store to be non-functional.
            // *
            // * If you break your store attempting to enable ML-only features in PRO or
            // * Standard, our staff cannot help you fix it, and it will also invalidate
            // * your AspDotNetStorefront License.
            /***************************************************************************/

            if (!AppLogic.m_ProductIsML())
            {
                Response.Redirect(AppLogic.AdminLinkUrl("restrictedfeature.aspx"));
            }

            Customer ThisCustomer = ((AspDotNetStorefrontPrincipal)Context.User).ThisCustomer;
            writer.Append("<div align=\"left\">");

            if (!ThisCustomer.IsAdminUser) // safety check
            {
                writer.Append("<b><font color=red>PERMISSION DENIED</b></font>");
            }
            else
            {
                int ONX = CommonLogic.QueryStringUSInt("OrderNumber");
                Order ord = new Order(ONX, ThisCustomer.LocaleSetting);

                writer.Append("<b>CANCEL AUTO-BILL AND FULLY REFUND ORDER: " + ONX.ToString() + "</b><br/><br/>");
                if (CommonLogic.FormCanBeDangerousContent("IsSubmit") == "true")
                {
                    String RefundReason = CommonLogic.FormCanBeDangerousContent("RefundReason");
                    String Status = Gateway.OrderManagement_DoFullRefund(ord, ThisCustomer.LocaleSetting, RefundReason);
                    writer.Append("Refund Status: " + Status);
                    if (Status == AppLogic.ro_OK)
                    {
                        RecurringOrderMgr rmgr = new RecurringOrderMgr(null, null);
                        if (ord.ParentOrderNumber == 0)
                        {
                            Status = rmgr.CancelRecurringOrder(ONX);
                        }
                        else
                        {
                            Status = rmgr.CancelRecurringOrder(ord.ParentOrderNumber);
                        }
                        writer.Append("<p>Cancel Auto-Bill Status: " + Status + "</p>");

                        if (Status == AppLogic.ro_OK)
                        {
                            writer.Append("<script type=\"text/javascript\">\n");
                            writer.Append("opener.window.location.reload();");
                            writer.Append("</script>\n");
                        }
                    }
                    writer.Append("<p align=\"center\"><a href=\"javascript:self.close();\">Close</a></p>");
                }
                else
                {
                    writer.Append("<form method=\"POST\" action=\"" + AppLogic.AdminLinkUrl("recurringrefundcancel.aspx") + "?ordernumber=" + ONX.ToString() + "&confirm=yes\" id=\"RefundOrderForm\" name=\"RefundOrderForm\">");
                    writer.Append("<input type=\"hidden\" name=\"IsSubmit\" value=\"true\">");
                    writer.Append("<p align=\"center\">Are you sure you want to stop future billing and refund this order?<br/><br/></p>");
                    writer.Append("<p align=\"center\">Reason: <input type=\"text\" size=\"50\" maxlength=\"100\" name=\"RefundReason\"></p>");
                    writer.Append("<p align=\"center\"><input type=\"submit\" name=\"submit\" value=\"&nbsp;&nbsp;Yes&nbsp;&nbsp;\">");
                    writer.Append("<img src=\"" + AppLogic.LocateImageURL("~/App_Themes/Admin_Default/images/spacer.gif") + "\" width=\"100\" height=\"1\">");
                    writer.Append("<input type=\"button\" name=\"cancel\" value=\"&nbsp;&nbsp;No&nbsp;&nbsp;\" onClick=\"javascript:self.close();\">");
                    writer.Append("</p>");
                    writer.Append("</form>");
                }
            }

            writer.Append("</div>");
            ltContent.Text = writer.ToString();
        }

    }
}