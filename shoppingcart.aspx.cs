// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Xml;
using System.Xml.Xsl;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontControls;
using AspDotNetStorefrontGateways;
using System.Collections.Generic;
using AspDotNetStorefrontCore.ShippingCalculation;
using System.Linq;
using System.Web.UI;
using AspDotNetStorefront.Promotions;

namespace AspDotNetStorefront
{
    /// <summary>
    /// Summary description for ShoppingCartPage.
    /// </summary>
    public partial class ShoppingCartPage : SkinBase
    {
        #region Web Form Designer generated code
        override protected void OnInit(EventArgs e)
        {
            //
            // CODEGEN: This call is required by the ASP.NET Web Form Designer.
            //
            InitializeComponent();
            InitializeShoppingCartControl();

            base.OnInit(e);
        }

        private void InitializeShoppingCartControl()
        {
            cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);

            ctrlShoppingCart.DataSource = cart.CartItems;
            ctrlShoppingCart.DataBind();

            ctrlCartSummary.DataSource = cart;
            ctrlCartSummary.DataBind();

            InitializeOrderOptionControl();
        }

        /// <summary>
        /// Initializes the order option control
        /// </summary>
        private void InitializeOrderOptionControl()
        {
            ctrlOrderOption.ThisCustomer = ThisCustomer;
            ctrlOrderOption.EditMode = true;
            ctrlOrderOption.Datasource = cart;

            if (cart.AllOrderOptions.Count > 0)
            {
                ctrlOrderOption.DataBind();
                ctrlOrderOption.Visible = true;
            }
            else
            {
                ctrlOrderOption.Visible = false;
            }
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnContinueShoppingTop.Click += new EventHandler(btnContinueShoppingTop_Click);
            btnContinueShoppingBottom.Click += new EventHandler(btnContinueShoppingBottom_Click);
            btnCheckOutNowTop.Click += new EventHandler(btnCheckOutNowTop_Click);
            btnCheckOutNowBottom.Click += new EventHandler(btnCheckOutNowBottom_Click);
            btnInternationalCheckOutNowTop.Click += new EventHandler(btnInternationalCheckOutNowTop_Click);
            btnInternationalCheckOutNowBottom.Click += new EventHandler(btnInternationalCheckOutNowBottom_Click);
            btnQuickCheckoutTop.Click += new EventHandler(btnQuickCheckoutTop_Click);
            btnQuickCheckoutBottom.Click += new EventHandler(btnQuickCheckoutBottom_Click);
            btnUpdateCart1.Click += new EventHandler(btnUpdateCart1_Click);
            btnUpdateCart2.Click += new EventHandler(btnUpdateCart2_Click);
            btnUpdateCart3.Click += new EventHandler(btnUpdateCart3_Click);
            btnUpdateCart4.Click += new EventHandler(btnUpdateCart4_Click);
            btnUpdateCart5.Click += new EventHandler(btnUpdateCart5_Click);
        }
        #endregion

        private ErrorMessage _errorMessage;
        private ErrorMessage errorMessage
        {
            get 
            {
                if (_errorMessage == null)
                    _errorMessage = new ErrorMessage(CommonLogic.QueryStringNativeInt("ErrorMsg"));
                
                return _errorMessage;
            }
        }

        private ShoppingCart _cart = null;
        private ShoppingCart cart
        {
            get { return _cart; }
            set 
            { 
                _cart = value;
                InventoryTrimmedEarly = _cart.InventoryTrimmed || InventoryTrimmedEarly;
                if (_cart.TrimmedReason != InventoryTrimmedReason.None)
                    TrimmedEarlyReason = _cart.TrimmedReason;
            }
        }
        bool VATEnabled = false;
        bool VATOn = false;
        int CountryID = 0;
        int StateID = 0;
        string ZipCode = string.Empty;

        protected void Page_Load(object sender, System.EventArgs e)
        {            
            this.RequireCustomerRecord();
            RequireSecurePage();
            SectionTitle = AppLogic.GetString("AppConfig.CartPrompt", SkinID, ThisCustomer.LocaleSetting);
            ClearErrors();

            if (this.cart.CartItems.Select(ci => ci.ShippingAddressID).Distinct().Count() > 1)
			{
                this.cart.ApplyShippingRules();
				UpdateCart();
			}
            this.cart.ConsolidateCartItems();

			var checkOutType = CheckOutPageControllerFactory.CreateCheckOutPageController().GetCheckoutType();
			bool onePageCheckout = checkOutType == CheckOutType.BasicOPC || checkOutType == CheckOutType.SmartOPC;

			if (AppLogic.ProductIsMLExpress() == false && onePageCheckout)
            {
                // don't need here, it's redundant with the regular checkout button:
                btnQuickCheckoutTop.Visible = false;
                btnQuickCheckoutBottom.Visible = false;
            }
            else
            {
                btnQuickCheckoutTop.Visible = AppLogic.AppConfigBool("QuickCheckout.Enabled");
                btnQuickCheckoutBottom.Visible = AppLogic.AppConfigBool("QuickCheckout.Enabled");

            }

            VATEnabled = AppLogic.ProductIsMLExpress() == false && AppLogic.AppConfigBool("VAT.Enabled");
            VATOn = (VATEnabled && ThisCustomer.VATSettingReconciled == VATSettingEnum.ShowPricesInclusiveOfVAT);

            if (VATEnabled)
            {
                CountryID = AppLogic.AppConfigUSInt("VAT.CountryID");
                StateID = 0;
                ZipCode = string.Empty;

                if (ThisCustomer.IsRegistered)
                {
                    if (ThisCustomer.PrimaryShippingAddress.CountryID > 0)
                    {
                        CountryID = ThisCustomer.PrimaryShippingAddress.CountryID;
                    }
                    if (ThisCustomer.PrimaryShippingAddress.StateID > 0)
                    {
                        StateID = ThisCustomer.PrimaryShippingAddress.StateID;
                    }
                    if (ThisCustomer.PrimaryShippingAddress.Zip.Trim().Length != 0)
                    {
                        ZipCode = ThisCustomer.PrimaryShippingAddress.Zip.Trim();
                    }
                }
            }

            if (!this.IsPostBack)
            {
                string ReturnURL = CommonLogic.QueryStringCanBeDangerousContent("ReturnUrl");
                AppLogic.CheckForScriptTag(ReturnURL);
                ViewState["ReturnURL"] = ReturnURL;

                InitializePageContent(checkOutType);
                InitializeShippingAndEstimateControl();
            }
            else
            {
                pnlOrderOptions.Visible = !cart.IsEmpty();
                pnlUpsellProducts.Visible = !cart.IsEmpty();
                pnlCoupon.Visible = pnlPromotion.Visible = !cart.IsEmpty() && cart.CouponsAllowed;
                pnlOrderNotes.Visible = !AppLogic.AppConfigBool("DisallowOrderNotes") && !cart.IsEmpty();
                btnCheckOutNowBottom.Visible = btnCheckOutNowTop.Visible = (!cart.IsEmpty() && AppLogic.AllowRegularCheckout(cart));
                btnRequestEstimates.Visible = !cart.IsEmpty();
                pnlCartSummarySubTotals.Visible = !cart.IsEmpty();
            }

            if (!ThisCustomer.IsRegistered || ThisCustomer.PrimaryShippingAddressID <= 0)
            {
                InitializeShippingAndEstimateControl();
            }

            String CurrentCoupon = String.Empty;
            if (cart.Coupon.CouponCode.Length != 0 && String.IsNullOrEmpty(CouponCode.Text))
                CurrentCoupon = cart.Coupon.CouponCode;
            else if (cart.CartItems.CouponList.Count == 1)
                CurrentCoupon = cart.CartItems.CouponList[0].CouponCode;

            if (CurrentCoupon.Length > 0 && CouponCode.Text.Length == 0)
                CouponCode.Text = CurrentCoupon;

            btnRemoveCoupon.Visible = CouponCode.Text.Length != 0;

            lblPromotionError.Text = String.Empty;
            if (!IsPostBack)
                BindPromotions();
        }
        
        void btnContinueShoppingTop_Click(object sender, EventArgs e)
        {
            ContinueShopping();
        }
        void btnContinueShoppingBottom_Click(object sender, EventArgs e)
        {
            ContinueShopping();
        }
        void btnCheckOutNowTop_Click(object sender, EventArgs e)
        {
            UpdateCartQuantity();
            ProcessCart(true, false, false);
			InitializeShippingAndEstimateControl();
		}
        void btnCheckOutNowBottom_Click(object sender, EventArgs e)
        {
            UpdateCartQuantity();
			ProcessCart(true, false, false);
			InitializeShippingAndEstimateControl();
		}
        void btnInternationalCheckOutNowTop_Click(object sender, EventArgs e)
        {
            ProcessCart(true, false, true);
        }
        void btnInternationalCheckOutNowBottom_Click(object sender, EventArgs e)
        {
            ProcessCart(true, false, true);
        }
        void btnQuickCheckoutTop_Click(object sender, EventArgs e)
        {
            ProcessCart(true, true, false);
        }
        void btnQuickCheckoutBottom_Click(object sender, EventArgs e)
        {
            ProcessCart(true, true, false);
        }
        
        private bool InventoryTrimmedEarly = false;
        
        private bool InventoryTrimmed
        {
            get
            {
                if (InventoryTrimmedEarly)
                    return true;
                else if (Request.QueryString["InvTrimmed"] != null)
                    return CommonLogic.QueryStringBool("InvTrimmed");
                else
                    return false;
            }
            
        }
        
        private InventoryTrimmedReason TrimmedEarlyReason;
        
        void btnUpdateCart1_Click(object sender, EventArgs e)
        {
            UpdateCart();
        }

        protected void btnUpdateCart2_Click(object sender, EventArgs e)
        {
            UpdateCart();
        }
        
        void btnUpdateCart3_Click(object sender, EventArgs e)
        {
            UpdateCart();
        }
        
        void btnUpdateCart4_Click(object sender, EventArgs e)
        {
            UpdateCart();
        }
        
        void btnUpdateCart5_Click(object sender, EventArgs e)
        {
            UpdateCart();
        }

        private void UpdateCart()
        {
            if (cart.InventoryTrimmed)
            {
                cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
                ctrlCartSummary.DataSource = cart;
                Response.Redirect(string.Format("shoppingcart.aspx?InvTrimmed=true"));
            }
            cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
            cart.SetCoupon(CouponCode.Text.ToUpperInvariant(), true);
            UpdateCartQuantity();
            ctrlOrderOption.UpdateChanges();
            ProcessCart(false, false, false);
			InitializePageContent(CheckOutPageControllerFactory.CreateCheckOutPageController().GetCheckoutType());
            InitializeShippingAndEstimateControl();
            InitializeShoppingCartControl();
        }

        public void InitializePageContent(CheckOutType checkoutType)
        {
            int AgeCartDays = AppLogic.AppConfigUSInt("AgeCartDays");
            if (AgeCartDays == 0)
            {
                AgeCartDays = 7;
            }

            ShoppingCart.Age(ThisCustomer.CustomerID, AgeCartDays, CartTypeEnum.ShoppingCart);

            cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
            shoppingcartaspx8.Text = AppLogic.GetString("shoppingcart.aspx.8", SkinID, ThisCustomer.LocaleSetting);
            shoppingcartaspx10.Text = AppLogic.GetString("shoppingcart.aspx.10", SkinID, ThisCustomer.LocaleSetting);
            shoppingcartaspx11.Text = AppLogic.GetString("shoppingcart.aspx.11", SkinID, ThisCustomer.LocaleSetting);
            shoppingcartaspx9.Text = AppLogic.GetString("shoppingcart.aspx.9", SkinID, ThisCustomer.LocaleSetting);

            //shoppingcartcs27.Text = AppLogic.GetString("shoppingcart.cs.27", SkinID, ThisCustomer.LocaleSetting);
            //shoppingcartcs28.Text = AppLogic.GetString("shoppingcart.cs.28", SkinID, ThisCustomer.LocaleSetting);
            //shoppingcartcs29.Text = AppLogic.GetString("shoppingcart.cs.29", SkinID, ThisCustomer.LocaleSetting);

            shoppingcartcs31.Text = AppLogic.GetString("shoppingcart.cs.117", SkinID, ThisCustomer.LocaleSetting);
            btnUpdateCart1.Text = AppLogic.GetString("shoppingcart.cs.110", SkinID, ThisCustomer.LocaleSetting);
            btnUpdateCart2.Text = AppLogic.GetString("shoppingcart.cs.110", SkinID, ThisCustomer.LocaleSetting);
            btnUpdateCart3.Text = AppLogic.GetString("shoppingcart.cs.110", SkinID, ThisCustomer.LocaleSetting);
            btnUpdateCart4.Text = AppLogic.GetString("shoppingcart.cs.110", SkinID, ThisCustomer.LocaleSetting);
            btnUpdateCart5.Text = AppLogic.GetString("shoppingcart.cs.110", SkinID, ThisCustomer.LocaleSetting);
            lblOrderNotes.Text = AppLogic.GetString("shoppingcart.cs.66", SkinID, ThisCustomer.LocaleSetting);
            btnContinueShoppingTop.Text = AppLogic.GetString("shoppingcart.cs.62", SkinID, ThisCustomer.LocaleSetting);
            btnContinueShoppingBottom.Text = AppLogic.GetString("shoppingcart.cs.62", SkinID, ThisCustomer.LocaleSetting);
            btnCheckOutNowTop.Text = AppLogic.GetString("shoppingcart.cs.111", SkinID, ThisCustomer.LocaleSetting);
            btnCheckOutNowBottom.Text = AppLogic.GetString("shoppingcart.cs.111", SkinID, ThisCustomer.LocaleSetting);

            bool reqOver13 = AppLogic.AppConfigBool("RequireOver13Checked");
            btnCheckOutNowTop.Enabled = !cart.IsEmpty() && !cart.RecurringScheduleConflict && (!reqOver13 || (reqOver13 && ThisCustomer.IsOver13)) || !ThisCustomer.IsRegistered;
            if (btnCheckOutNowTop.Enabled && AppLogic.MicropayIsEnabled()
                && !AppLogic.AppConfigBool("MicroPay.HideOnCartPage") && cart.CartItems.Count == 1
                && cart.HasMicropayProduct() && ((CartItem)cart.CartItems[0]).Quantity == 0)
            {
                // We have only one item and it is the Micropay Product and the Quantity is zero
                // Don't allow checkout
                btnCheckOutNowTop.Enabled = false;
            }
            btnCheckOutNowBottom.Enabled = btnCheckOutNowTop.Enabled;
            ErrorMsgLabel.Text = CommonLogic.IIF(!cart.IsEmpty() && (reqOver13 && !ThisCustomer.IsOver13 && ThisCustomer.IsRegistered), AppLogic.GetString("Over13OnCheckout", SkinID, ThisCustomer.LocaleSetting), String.Empty);

            PayPalExpressSpan.Visible = false;
            PayPalExpressSpan2.Visible = false;

            Decimal MinOrderAmount = AppLogic.AppConfigUSDecimal("CartMinOrderAmount");

            if (!cart.IsEmpty() && !cart.ContainsRecurringAutoShip)
            {
                // Enable PayPalExpress if using PayPalPro or PayPal Express is an active payment method.
                bool IncludePayPalExpress = false;

                if (AppLogic.AppConfigBool("PayPal.Express.ShowOnCartPage") && cart.MeetsMinimumOrderAmount(MinOrderAmount))
                {
                    if (AppLogic.ActivePaymentGatewayCleaned() == Gateway.ro_GWPAYPALPRO)
                    {
                        IncludePayPalExpress = true;
                    }
                    else
                    {
                        foreach (String PM in AppLogic.AppConfig("PaymentMethods").ToUpperInvariant().Split(','))
                        {
                            String PMCleaned = AppLogic.CleanPaymentMethod(PM);
                            if (PMCleaned == AppLogic.ro_PMPayPalExpress)
                            {
                                IncludePayPalExpress = true;
                                break;
                            }
                        }
                    }
                }

                if (IncludePayPalExpress)
                {
                    if (AppLogic.AppConfigBool("PayPal.Promo.Enabled")
                        && cart.Total(true) >= AppLogic.AppConfigNativeDecimal("PayPal.Promo.CartMinimum")
                        && cart.Total(true) <= AppLogic.AppConfigNativeDecimal("PayPal.Promo.CartMaximum"))
                    {
                        btnPayPalExpressCheckout.ImageUrl = AppLogic.AppConfig("PayPal.Promo.ButtonImageURL");
                    }
                    else
                    {
                        btnPayPalExpressCheckout.ImageUrl = AppLogic.AppConfig("PayPal.Express.ButtonImageURL");
                    }

                    btnPayPalExpressCheckout2.ImageUrl = btnPayPalExpressCheckout.ImageUrl;
                    PayPalExpressSpan.Visible = true;
                    PayPalExpressSpan2.Visible = true;
                }
            }

            string googleimageurl = String.Format(AppLogic.AppConfig("GoogleCheckout.LiveCheckoutButton"), AppLogic.AppConfig("GoogleCheckout.MerchantId"));
            if (AppLogic.AppConfigBool("GoogleCheckout.UseSandbox"))
            {
                googleimageurl = String.Format(AppLogic.AppConfig("GoogleCheckout.SandBoxCheckoutButton"), AppLogic.AppConfig("GoogleCheckout.SandboxMerchantId"));
            }
            googleimageurl = CommonLogic.IsSecureConnection() ? googleimageurl.ToLower().Replace("http://", "https://") : googleimageurl;

            if (AppLogic.ProductIsMLExpress() == true)
            {
                googleimageurl = string.Empty;
            }

            btnGoogleCheckout.ImageUrl = googleimageurl;
            btnGoogleCheckout2.ImageUrl = googleimageurl;

            bool ForceGoogleOff = false;
            if (cart.IsEmpty() || cart.ContainsRecurringAutoShip || !cart.MeetsMinimumOrderAmount(MinOrderAmount) || ThisCustomer.ThisCustomerSession["IGD"].Length != 0 || (AppLogic.AppConfig("GoogleCheckout.MerchantId").Length == 0 && AppLogic.AppConfig("GoogleCheckout.SandboxMerchantId").Length == 0))
            {
                GoogleCheckoutSpan.Visible = false;
                GoogleCheckoutSpan2.Visible = false;
                ForceGoogleOff = true; // these conditions force google off period (don't care about other settings)
            }

            if (!AppLogic.AppConfigBool("GoogleCheckout.ShowOnCartPage"))
            {
                // turn off the google checkout, but not in a forced condition, as the mall may turn it back on
                GoogleCheckoutSpan.Visible = false;
                GoogleCheckoutSpan2.Visible = false;
            }

            // allow the GooglerMall to turn google checkout back on, if not forced off prior and not already visible anyway:
            if (!ForceGoogleOff && !GoogleCheckoutSpan.Visible && (AppLogic.AppConfigBool("GoogleCheckout.GoogleMallEnabled") && Profile.GoogleMall != String.Empty))
            {
                GoogleCheckoutSpan.Visible = true;
                GoogleCheckoutSpan2.Visible = true;
            }

            AmazonCheckoutSpan.Visible = false;
            AmazonCheckoutSpan2.Visible = false;

            GatewayCheckoutByAmazon.CheckoutByAmazon cba = new GatewayCheckoutByAmazon.CheckoutByAmazon();
            
            // Clear out any cba checkouts that haven't finished.
            if (cba.IsEnabled)
                cba.ResetCheckout(ThisCustomer.CustomerID);

            // If cba is enabled and the cart allows checkout in it's current state, then render the checkout buttons.
            Boolean allowCheckoutByAmazon = !cart.IsEmpty() && !cart.ContainsRecurring() && cart.MeetsMinimumOrderAmount(MinOrderAmount) && ThisCustomer.ThisCustomerSession["IGD"].Length == 0;
            if (allowCheckoutByAmazon && cba.IsEnabled)
            {
                AmazonCheckoutSpan.Visible =
                    AmazonCheckoutSpan2.Visible = true;

                litAmazonCheckoutButton.Text = cba.RenderCheckoutButton("CBAWidgetContainer1", new Guid(ThisCustomer.CustomerGUID), checkoutType != CheckOutType.Standard);
                litAmazonCheckoutButton2.Text = cba.RenderCheckoutButton("CBAWidgetContainer2", new Guid(ThisCustomer.CustomerGUID), checkoutType != CheckOutType.Standard);
            }

            if (!cart.ContainsRecurring() && (GoogleCheckoutSpan.Visible || PayPalExpressSpan.Visible || AmazonCheckoutSpan.Visible))
            {
                AlternativeCheckouts.Visible = true;
                AlternativeCheckouts2.Visible = true;
            }
            else
            {
                AlternativeCheckouts.Visible = false;
                AlternativeCheckouts2.Visible = false;
            }


            if (!ForceGoogleOff)
            {
                // hide GC button for carts that don't qualify
                imgGoogleCheckoutDisabled.Visible = !GoogleCheckout.PermitGoogleCheckout(cart);
                btnGoogleCheckout.Visible = !imgGoogleCheckoutDisabled.Visible;

                imgGoogleCheckout2Disabled.Visible = imgGoogleCheckoutDisabled.Visible;
                btnGoogleCheckout2.Visible = btnGoogleCheckout.Visible;
            }

            Shipping.ShippingCalculationEnum ShipCalcID = Shipping.GetActiveShippingCalculationID();
            
            StringBuilder html = new StringBuilder("");
            html.Append("<script type=\"text/javascript\">\n");
            html.Append("function Cart_Validator(theForm)\n");
            html.Append("{\n");
            String cartJS = CommonLogic.ReadFile("jscripts/shoppingcart.js", true);
            foreach (CartItem c in cart.CartItems)
            {
                html.Append(cartJS.Replace("%SKU%", c.ShoppingCartRecordID.ToString()));
            }
            html.Append("return(true);\n");
            html.Append("}\n");
            html.Append("</script>\n");

            ValidationScript.Text = html.ToString();

            JSPopupRoutines.Text = AppLogic.GetJSPopupRoutines();
            String XmlPackageName = AppLogic.AppConfig("XmlPackage.ShoppingCartPageHeader");
            if (XmlPackageName.Length != 0)
            {
                XmlPackage_ShoppingCartPageHeader.Text = AppLogic.RunXmlPackage(XmlPackageName, base.GetParser, ThisCustomer, SkinID, String.Empty, String.Empty, true, true);
            }

            String XRI = AppLogic.SkinImage("redarrow.gif");
            redarrow1.ImageUrl = XRI;
            redarrow2.ImageUrl = XRI;
            redarrow3.ImageUrl = XRI;
            redarrow4.ImageUrl = XRI;

            ShippingInformation.Visible = (!AppLogic.AppConfigBool("SkipShippingOnCheckout") && !cart.IsAllFreeShippingComponents() && !cart.IsAllSystemComponents());
            AddresBookLlink.Visible = ThisCustomer.IsRegistered;

            btnCheckOutNowTop.Visible = btnCheckOutNowBottom.Visible = (!cart.IsEmpty() && AppLogic.AllowRegularCheckout(cart));

            if (!cart.IsEmpty() && cart.HasCoupon() && !cart.CouponIsValid)
            {
                pnlCouponError.Visible = true;
                CouponError.Text = cart.CouponStatusMessage + " (" + Server.HtmlEncode(CommonLogic.IIF(cart.Coupon.CouponCode.Length != 0, cart.Coupon.CouponCode, ThisCustomer.CouponCode)) + ")";
                cart.ClearCoupon();
            }

            if (!String.IsNullOrEmpty(errorMessage.Message) || ErrorMsgLabel.Text.Length > 0)
            {
                pnlErrorMsg.Visible = true;
                ErrorMsgLabel.Text += errorMessage.Message;
            }

            if (cart.InventoryTrimmed || this.InventoryTrimmed)
            {
                pnlInventoryTrimmedError.Visible = true;
                if (cart.TrimmedReason == InventoryTrimmedReason.RestrictedQuantities || TrimmedEarlyReason == InventoryTrimmedReason.RestrictedQuantities)
                    InventoryTrimmedError.Text = AppLogic.GetString("shoppingcart.aspx.33", SkinID, ThisCustomer.LocaleSetting);
                else if (cart.TrimmedReason == InventoryTrimmedReason.MinimumQuantities || TrimmedEarlyReason == InventoryTrimmedReason.MinimumQuantities)
                    InventoryTrimmedError.Text = AppLogic.GetString("shoppingcart.aspx.7", SkinID, ThisCustomer.LocaleSetting);
                else
                    InventoryTrimmedError.Text = AppLogic.GetString("shoppingcart.aspx.3", SkinID, ThisCustomer.LocaleSetting);
                
                cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
                ctrlShoppingCart.DataSource = cart.CartItems;
                ctrlCartSummary.DataSource = cart;
            }

            if (cart.RecurringScheduleConflict)
            {
                pnlRecurringScheduleConflictError.Visible = true;
                RecurringScheduleConflictError.Text = AppLogic.GetString("shoppingcart.aspx.19", SkinID, ThisCustomer.LocaleSetting);
            }

            if (CommonLogic.QueryStringBool("minimumQuantitiesUpdated"))
            {
                pnlMinimumQuantitiesUpdatedError.Visible = true;
                MinimumQuantitiesUpdatedError.Text = AppLogic.GetString("shoppingcart.aspx.7", SkinID, ThisCustomer.LocaleSetting);
            }

            if (!cart.MeetsMinimumOrderAmount(MinOrderAmount))
            {
                pnlMeetsMinimumOrderAmountError.Visible = true;
                MeetsMinimumOrderAmountError.Text = String.Format(AppLogic.GetString("shoppingcart.aspx.4", SkinID, ThisCustomer.LocaleSetting), ThisCustomer.CurrencyString(MinOrderAmount));
            }

            int MinQuantity = AppLogic.AppConfigUSInt("MinCartItemsBeforeCheckout");
            if (!cart.MeetsMinimumOrderQuantity(MinQuantity))
            {
                pnlMeetsMinimumOrderQuantityError.Visible = true;
                MeetsMinimumOrderQuantityError.Text = String.Format(AppLogic.GetString("shoppingcart.cs.20", SkinID, ThisCustomer.LocaleSetting), MinQuantity.ToString(), MinQuantity.ToString());
            }


            if (AppLogic.MicropayIsEnabled() && AppLogic.AppConfigBool("Micropay.ShowTotalOnTopOfCartPage"))
            {
                pnlMicropay_EnabledError.Visible = true;
                Micropay_EnabledError.Text = "<div align=\"left\">" + String.Format(AppLogic.GetString("account.aspx.10", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), AppLogic.GetString("account.aspx.11", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), ThisCustomer.CurrencyString(ThisCustomer.MicroPayBalance)) + "</div>";
            }

            ctrlShoppingCart.HeaderTabImageURL = AppLogic.SkinImage("ShoppingCart.gif");

            pnlCartSummarySubTotals.Visible = !cart.IsEmpty();

            if (!cart.IsEmpty())
            {
                if (cart.AllOrderOptions.Count > 0)
                {
                    pnlOrderOptions.Visible = true;
                }
                else
                {
                    pnlOrderOptions.Visible = false;
                }


                string upsellproductlist = GetUpsellProducts(cart);
                if (upsellproductlist.Length > 0)
                {
                    UpsellProducts.Text = upsellproductlist;
                    btnUpdateCart5.Visible = true;
                    pnlUpsellProducts.Visible = true;
                }
                else
                {
                    pnlUpsellProducts.Visible = false;
                }

                if (cart.CouponsAllowed)
                {
                    ShoppingCartCoupon_gif.ImageUrl = AppLogic.SkinImage("ShoppingCartCoupon.gif");
                    if (CouponCode.Text.Length == 0)
                        CouponCode.Text = cart.Coupon.CouponCode;

                    btnRemoveCoupon.Visible = CouponCode.Text.Length != 0;
                    pnlCoupon.Visible = pnlPromotion.Visible = true;
                }
                else
                {
                    pnlCoupon.Visible = pnlPromotion.Visible = false;
                }

                ShoppingCartNotes_gif.ImageUrl = AppLogic.SkinImage("ShoppingCartNotes.gif");
                if (!AppLogic.AppConfigBool("DisallowOrderNotes"))
                {
                    OrderNotes.Text = cart.OrderNotes;
                    pnlOrderNotes.Visible = true;
                }
                else
                {
                    pnlOrderNotes.Visible = false;
                }

            }
            else
            {
                pnlOrderOptions.Visible = false;
                pnlUpsellProducts.Visible = false;
                pnlCoupon.Visible = pnlPromotion.Visible = false;
                pnlOrderNotes.Visible = false;
            }

            if (AppLogic.AppConfigBool("SkipShippingOnCheckout") || cart.IsAllFreeShippingComponents() || cart.IsAllSystemComponents())
            {
                ctrlCartSummary.ShowShipping = false;
            }


            if (!cart.HasTaxableComponents() || AppLogic.CustomerLevelHasNoTax(ThisCustomer.CustomerLevelID))
            {
                ctrlCartSummary.ShowTax = false;
            }

            String XmlPackageName2 = AppLogic.AppConfig("XmlPackage.ShoppingCartPageFooter");
            if (XmlPackageName2.Length != 0)
            {
                XmlPackage_ShoppingCartPageFooter.Text = AppLogic.RunXmlPackage(XmlPackageName2, base.GetParser, ThisCustomer, SkinID, String.Empty, String.Empty, true, true);
            }

            // handle international checkout buttons now (see internationalcheckout.com).
            if (btnCheckOutNowTop.Visible)
            {
				btnInternationalCheckOutNowTop.Visible = CheckOutPageControllerFactory.CreateCheckOutPageController(ThisCustomer, cart)
					.CanUseInternationalCheckout();
				btnInternationalCheckOutNowBottom.Visible = btnInternationalCheckOutNowTop.Visible;
            }

            if (cart.ShippingThresHoldIsDefinedButFreeShippingMethodIDIsNot)
            {
                pnlErrorMsg.Visible = true;
                ErrorMsgLabel.Text += Server.HtmlEncode(AppLogic.GetString("shoppingcart.aspx.21", SkinID, ThisCustomer.LocaleSetting));
            }

            btnRemoveEstimator.Visible = false;

            ToggleShowHideEstimate();

        }

        private void InitializeShippingAndEstimateControl()
        {
            bool showEstimates = AppLogic.AppConfigBool("ShowShippingAndTaxEstimate") && !AppLogic.ProductIsMLExpress();

            if (ThisCustomer.ThisCustomerSession.SessionBool("ShowEstimateSelected") && showEstimates)
            {
                btnRequestEstimates_Click(this, EventArgs.Empty);
            }
            else
            {

                ToggleShowHideEstimate();

                //Set it to false, in case ShowShippingAndTaxEstimate appconfig was turn off in Admin
                pnlShippingAndTaxEstimator.Visible = false;
            }

            if (!ThisCustomer.IsRegistered || ThisCustomer.PrimaryShippingAddressID <=0)
            {
                ctrlEstimateAddress.CaptionWidth = Unit.Percentage(0);
                ctrlEstimateAddress.ValueWidth = Unit.Percentage(70);
                ctrlEstimateAddress.Header = AppLogic.GetString("checkoutshipping.AddressControl.Header", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
                ctrlEstimateAddress.CountryCaption = AppLogic.GetString("checkoutshipping.AddressControl.Country", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
                ctrlEstimateAddress.StateCaption = AppLogic.GetString("checkoutshipping.AddressControl.State", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
                ctrlEstimateAddress.ZipCaption = AppLogic.GetString("checkoutshipping.AddressControl.PostalCode", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
                ctrlEstimateAddress.CityCaption = AppLogic.GetString("checkoutshipping.AddressControl.City", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
                ctrlEstimateAddress.GetEstimateCaption = AppLogic.GetString("checkoutshipping.AddressControl.GetEstimateCaption", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
                ctrlEstimateAddress.RequireZipCodeErrorMessage = AppLogic.GetString("checkoutshipping.AddressControl.ErrorMessage", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
                ctrlEstimateAddress.HideZipCodeValidation();
            }

            if (btnRequestEstimates.Visible)
            {
                pnlShippingAndTaxEstimator.Visible = false;
            }

            btnRequestEstimates.Text = AppLogic.GetString("checkoutshipping.AddressControl.GetEstimateCaption", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
        }

        private void ToggleShowHideEstimate()
        {
            if (AppLogic.AppConfigBool("ShowShippingAndTaxEstimate") && !AppLogic.ProductIsMLExpress())
            {
                bool estimateShown = ThisCustomer.ThisCustomerSession.SessionBool("ShowEstimateSelected");

                btnRequestEstimates.Visible = !estimateShown;
                btnRemoveEstimator.Visible = estimateShown;

                ctrlCartSummary.ShowShipping = !estimateShown;
                ctrlCartSummary.ShowTax = !estimateShown;
            }
            else
            {
                btnRequestEstimates.Visible = false;
                btnRemoveEstimator.Visible = false;
            }
        }

        protected void btnRemoveEstimator_Click(object sender, EventArgs e)
        {
            if (ThisCustomer.ThisCustomerSession.SessionBool("ShowEstimateSelected"))
            {
                ThisCustomer.ThisCustomerSession.ClearVal("ShowEstimateSelected");
            }

            btnRemoveEstimator.Text = AppLogic.GetString("checkoutshipping.estimator.control.remove", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
            pnlShippingAndTaxEstimator.Visible = false;

            ToggleShowHideEstimate();

			InitializePageContent(CheckOutPageControllerFactory.CreateCheckOutPageController().GetCheckoutType());
        }

        protected void btnRequestEstimates_Click(object sender, EventArgs e)
        {
            if (ThisCustomer.IsRegistered && ThisCustomer.PrimaryShippingAddressID > 0)
            {
                SetupShippingAndEstimateControl(ctrlEstimate, ThisCustomer);
                ctrlEstimate.Visible = true;
            }
            else
            {
                IShippingCalculation activeShippingCalculation = cart.GetActiveShippingCalculation();

                // check whether the current shipping calculation logic requires zip code
                ctrlEstimateAddress.RequirePostalCode = activeShippingCalculation.RequirePostalCode;
                ctrlEstimateAddress.GetEstimateCaption = AppLogic.GetString("checkoutshipping.AddressControl.GetEstimateCaption", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
                ctrlEstimateAddress.Visible = true;
				if(!ThisCustomer.IsRegistered && AppLogic.AppConfigBool("AvalaraTax.Enabled"))
                {
                    EstimateAddressControl_RequestEstimateButtonClicked(sender, e);
                }
            }

            ThisCustomer.ThisCustomerSession.SetVal("ShowEstimateSelected", true.ToString(), DateTime.MaxValue);

            pnlShippingAndTaxEstimator.Visible = true;
            ToggleShowHideEstimate();

        }

        protected void EstimateAddressControl_RequestEstimateButtonClicked(object sender, EventArgs e)
        {
            ShippingAndTaxEstimatorAddressControl addressControl = sender as ShippingAndTaxEstimatorAddressControl;

            if (addressControl != null)
            {
                // anonymous customer, extract address info from the post args
                Address NewAddress = new Address();
                NewAddress.Country = addressControl.Country;
                NewAddress.City = addressControl.City;
                NewAddress.State = addressControl.State;
                NewAddress.Zip = addressControl.Zip;
                NewAddress.InsertDB(ThisCustomer.CustomerID);
                ThisCustomer.PrimaryShippingAddressID = NewAddress.AddressID;              

                IShippingCalculation activeShippingCalculation = cart.GetActiveShippingCalculation();

                if ((activeShippingCalculation.RequirePostalCode == false) ||
                    activeShippingCalculation.RequirePostalCode && addressControl.ValidateZipCode())
                {
                    ShippingAndTaxEstimateTableControl ctrlEstimate = new ShippingAndTaxEstimateTableControl();
                    SetupShippingAndEstimateControl(ctrlEstimate, ThisCustomer);

                    pnlShippingAndTaxEstimator.Controls.Add(ctrlEstimate);
                }
                // hide the estimate button
                ToggleShowHideEstimate();
            }
        }

        public string GetUpsellProducts(ShoppingCart cart)
        {
            StringBuilder UpsellProductList = new StringBuilder(1024);
            StringBuilder results = new StringBuilder("");

            // ----------------------------------------------------------------------------------------
            // WRITE OUT UPSELL PRODUCTS:
            // ----------------------------------------------------------------------------------------
            if (AppLogic.AppConfigBool("ShowUpsellProductsOnCartPage"))
            {
                foreach (CartItem c in cart.CartItems)
                {
                    if (UpsellProductList.Length != 0)
                    {
                        UpsellProductList.Append(",");
                    }
                    UpsellProductList.Append(c.ProductID.ToString());
                }
                if (UpsellProductList.Length != 0)
                {
                    // get list of all upsell products for those products now in the cart:
                    String sql = "select UpsellProducts from Product  with (NOLOCK)  where ProductID in (" + UpsellProductList.ToString() + ")";

                    using (SqlConnection dbconn = new SqlConnection(DB.GetDBConn()))
                    {
                        dbconn.Open();
                        using (IDataReader rs = DB.GetRS(sql, dbconn))
                        {
                            UpsellProductList.Remove(0, UpsellProductList.Length);
                            while (rs.Read())
                            {
                                if (DB.RSField(rs, "UpsellProducts").Length != 0)
                                {
                                    if (UpsellProductList.Length != 0)
                                    {
                                        UpsellProductList.Append(",");
                                    }
                                    UpsellProductList.Append(DB.RSField(rs, "UpsellProducts"));
                                }
                            }
                        }

                    }

                    if (UpsellProductList.Length != 0)
                    {
                        int ShowN = AppLogic.AppConfigUSInt("UpsellProductsLimitNumberOnCart");
                        if (ShowN == 0)
                        {
                            ShowN = 10;
                        }
                        String S = String.Empty;
                        try
                        {
                            S = AppLogic.GetUpsellProductsBoxExpandedForCart(UpsellProductList.ToString(), ShowN, true, String.Empty, AppLogic.AppConfig("RelatedProductsFormat").Equals("GRID", StringComparison.InvariantCultureIgnoreCase), SkinID, ThisCustomer);
                        }
                        catch { }
                        if (S.Length != 0)
                        {
                            results.Append(S);
                        }
                    }
                }
            }
            return results.ToString();
        }

        private void UpdateCartQuantity()
        {
            int quantity = 0;
            int sRecID = 0;
            string itemNotes;

            for (int i = 0; i < ctrlShoppingCart.Items.Count; i++)
            {
                quantity = ctrlShoppingCart.Items[i].Quantity;
                sRecID = ctrlShoppingCart.Items[i].ShoppingCartRecId;
                itemNotes = ctrlShoppingCart.Items[i].ItemNotes;

                cart.SetItemQuantity(sRecID, quantity);
                cart.SetItemNotes(sRecID, CommonLogic.CleanLevelOne(itemNotes));
            }
        }

        public void ProcessCart(bool DoingFullCheckout, bool ForceOnePageCheckout, bool InternationalCheckout)
        {
            Response.CacheControl = "private";
            Response.Expires = 0;
            Response.AddHeader("pragma", "no-cache");

            ThisCustomer.RequireCustomerRecord();
            CartTypeEnum cte = CartTypeEnum.ShoppingCart;
            if (CommonLogic.QueryStringCanBeDangerousContent("CartType").Length != 0)
            {
                cte = (CartTypeEnum)CommonLogic.QueryStringUSInt("CartType");
            }
            cart = new ShoppingCart(1, ThisCustomer, cte, 0, false);

            if (cart.IsEmpty())
            {
                cart.ClearCoupon();
                // can't have this at this point:
                switch (cte)
                {
                    case CartTypeEnum.ShoppingCart:
                        Response.Redirect("shoppingcart.aspx");
                        break;
                    case CartTypeEnum.WishCart:
                        Response.Redirect("wishlist.aspx");
                        break;
                    case CartTypeEnum.GiftRegistryCart:
                        Response.Redirect("giftregistry.aspx");
                        break;
                    default:
                        Response.Redirect("shoppingcart.aspx");
                        break;
                }
            }

            // update cart quantities:
            UpdateCartQuantity();

            // save coupon code, no need to reload cart object
            // will update customer record also:
            if (cte == CartTypeEnum.ShoppingCart)
            {
                cart.SetCoupon(CouponCode.Text, true);

                // kind of backwards, but if DisallowOrderNotes is false, then
 	 	 	    // allow order notes
 	 	 	    if (!AppLogic.AppConfigBool("DisallowOrderNotes"))
 	 	 	    {
 	 	 	        if (OrderNotes.Text.Trim().Length > 0)
 	 	 	        {
 	 	 	            SqlParameter sp = new SqlParameter("@OrderNotes", SqlDbType.NText);
 	 	 	            sp.Value = OrderNotes.Text.Trim();
 	 	 	            SqlParameter[] spa = { sp };
 	 	 	            ThisCustomer.UpdateCustomer(spa);
 	 	 	        }
 	 	 	    }

                // rebind the cart summary control to handle coupon
                ctrlCartSummary.DataSource = cart;
 
                // check for upsell products
                if (CommonLogic.FormCanBeDangerousContent("Upsell").Length != 0)
                {
                    foreach (String s in CommonLogic.FormCanBeDangerousContent("Upsell").Split(','))
                    {
                        int ProductID = Localization.ParseUSInt(s);
                        if (ProductID != 0)
                        {
                            int VariantID = AppLogic.GetProductsDefaultVariantID(ProductID);
                            if (VariantID != 0)
                            {
                                int NewRecID = cart.AddItem(ThisCustomer, ThisCustomer.PrimaryShippingAddressID, ProductID, VariantID, 1, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, CartTypeEnum.ShoppingCart, true, false, 0, System.Decimal.Zero);
                                Decimal PR = AppLogic.GetUpsellProductPrice(0, ProductID, ThisCustomer.CustomerLevelID);
                                SqlParameter[] spa = { DB.CreateSQLParameter("@Price", SqlDbType.Decimal, 10, PR, ParameterDirection.Input), DB.CreateSQLParameter("@CartRecID", SqlDbType.Int, 4, NewRecID, ParameterDirection.Input) };
                                DB.ExecuteSQL("update shoppingcart set IsUpsell=1, ProductPrice=@Price where ShoppingCartRecID=@CartRecID", spa);

                            }
                        }
                    }
                    InitializeShoppingCartControl();
                }

                if (cart.CheckInventory(ThisCustomer.CustomerID))
                {
                    ErrorMsgLabel.Text += Server.HtmlEncode(AppLogic.GetString("shoppingcart_process.aspx.1", SkinID, ThisCustomer.LocaleSetting));
                    // inventory got adjusted, send them back to the cart page to confirm the new values!
                }
            }

            if (cte == CartTypeEnum.WishCart)
            {
                Response.Redirect("wishlist.aspx");
            }
            if (cte == CartTypeEnum.GiftRegistryCart)
            {
                Response.Redirect("giftregistry.aspx");
            }

            cart.ClearShippingOptions();
            if (DoingFullCheckout)
            {
                bool validated = true;
                if (!cart.MeetsMinimumOrderAmount(AppLogic.AppConfigUSDecimal("CartMinOrderAmount")))
                {
                    validated = false;
                }

                if (!cart.MeetsMinimumOrderQuantity(AppLogic.AppConfigUSInt("MinCartItemsBeforeCheckout")))
                {
                    validated = false;
                }

                if (cart.HasCoupon() && !cart.CouponIsValid)
                {
                    validated = false;
                }

                if (validated)
                {
                    Response.Redirect(cart.PageToBeginCheckout(ForceOnePageCheckout, InternationalCheckout));
                }
				InitializePageContent(CheckOutPageControllerFactory.CreateCheckOutPageController().GetCheckoutType());
            }

            //Make sure promotions is updated when the cart changes
            BindPromotions();
        }

        private void ClearErrors()
        {
            CouponError.Text = "";
            ErrorMsgLabel.Text = "";
            InventoryTrimmedError.Text = "";
            RecurringScheduleConflictError.Text = "";
            MinimumQuantitiesUpdatedError.Text = "";
            MeetsMinimumOrderAmountError.Text = "";
            MeetsMinimumOrderQuantityError.Text = "";
            Micropay_EnabledError.Text = "";
        }

        private void ContinueShopping()
        {
            if (AppLogic.AppConfig("ContinueShoppingURL") == "")
            {
                if (ViewState["ReturnURL"].ToString() == "")
                {
                    Response.Redirect("default.aspx");
                }
                else
                {
                    Response.Redirect(ViewState["ReturnURL"].ToString());
                }
            }
            else
            {
                Response.Redirect(AppLogic.AppConfig("ContinueShoppingURL"));
            }
        }
        
        private bool DeleteButtonExists(string s)
        {
            return s == "bt_Delete";
        }

        protected void btnGoogleCheckout_Click(object sender, System.Web.UI.ImageClickEventArgs e)
        {
            ProcessCart(false, false, false);
            if (!ThisCustomer.IsRegistered && !AppLogic.AppConfigBool("PasswordIsOptionalDuringCheckout") && !AppLogic.AppConfigBool("GoogleCheckout.AllowAnonCheckout"))
            {
                if (AppLogic.ProductIsMLExpress())
                {
                    Response.Redirect("signin.aspx?ReturnUrl='shoppingcart.aspx'");
                }
                else
                {
                    Response.Redirect("checkoutanon.aspx?checkout=true&checkouttype=gc");
                }
            }
            else
            {
                Response.Redirect(GoogleCheckout.CreateGoogleCheckout(cart));
            }
        }

        protected void btnPayPalExpressCheckout_Click(object sender, System.Web.UI.ImageClickEventArgs e)
        {
            ProcessCart(false, false, false);

            if (CommonLogic.CookieCanBeDangerousContent("PayPalExpressToken", false) == "")
            {
                if (!ThisCustomer.IsRegistered && !AppLogic.AppConfigBool("PasswordIsOptionalDuringCheckout")
                        && !AppLogic.AppConfigBool("PayPal.Express.AllowAnonCheckout"))
                {
                    if (AppLogic.ProductIsMLExpress())
                    {
                        Response.Redirect("signin.aspx?ReturnUrl='shoppingcart.aspx'");
                    }
                    else
                    {
                        Response.Redirect("checkoutanon.aspx?checkout=true&checkouttype=ppec");
                    }
                }
                if (cart == null)
                {
                    cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
                }

                string url = String.Empty;
                if (ThisCustomer.IsRegistered && ThisCustomer.PrimaryShippingAddressID != 0)
                {
                    Address shippingAddress = new Address();
                    shippingAddress.LoadByCustomer(ThisCustomer.CustomerID, ThisCustomer.PrimaryShippingAddressID, AddressTypes.Shipping);
                    url = Gateway.StartExpressCheckout(cart, shippingAddress);
                }
                else
                {
                    url = Gateway.StartExpressCheckout(cart, null);
                }
                Response.Redirect(url);
            }
            else
            {
                Response.Redirect("checkoutshipping.aspx");
            }
        }

        private void SetupShippingAndEstimateControl(ShippingAndTaxEstimateTableControl ctrlEstimate, Customer thisCustomer)
        {
            try
            {
                //Appconfig that need to look for
                bool skipShippingOnCheckout = AppLogic.AppConfigBool("SkipShippingOnCheckout");
                bool freeShippingAllowsRateSelection = AppLogic.AppConfigBool("FreeShippingAllowsRateSelection");
                bool vatEnable = AppLogic.ProductIsMLExpress() == false && AppLogic.AppConfigBool("VAT.Enabled");

                ShoppingCart cart = new ShoppingCart(1, thisCustomer, CartTypeEnum.ShoppingCart, 0, false);
                //Collect the available shipping method
                ShippingMethodCollection availableShippingMethods = cart.GetShippingMethods(thisCustomer.PrimaryShippingAddress);

                //Initialize the caption of the control
                string shippingEstimateCaption = AppLogic.GetString("checkoutshipping.ShippingEstimateCaption", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                ctrlEstimate.HeaderCaption = AppLogic.GetString("checkoutshipping.estimator.control.header", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                ctrlEstimate.ShippingEstimateCaption = shippingEstimateCaption;
                ctrlEstimate.TaxEstimateCaption = AppLogic.GetString("checkoutshipping.TaxEstimateCaption", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                ctrlEstimate.TotalEstimateCaption = AppLogic.GetString("checkoutshipping.TotalEstimateCaption", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                ctrlEstimate.CaptionWidth = Unit.Percentage(50);
                ctrlEstimate.ValueWidth = Unit.Percentage(50);

                string inc = string.Empty;
                string lowestfreightName = string.Empty;
                decimal shippingTaxAmount = decimal.Zero;
                decimal SubTotal = cart.SubTotal(true, false, true, true, true, false);

                // Promotions: Line Item and Shipping discounts happen in the individual calculations so all that's left is to apply order level discounts.
                var orderDiscount = 0.00M;
                if (cart.CartItems.HasDiscountResults)
                    orderDiscount = cart.CartItems.DiscountResults.Sum(dr => dr.OrderTotal);

                SubTotal = SubTotal + orderDiscount;

                // Promotions: Because multiple promotions can be applied, it's possible to get a negative value, which should be caught and zeroed out.
                if (SubTotal < 0)
                    SubTotal = 0;

                decimal estimatedTax = decimal.Zero;
                decimal estimatedTotal = decimal.Zero;
                decimal estimatedShippingtotalWithTax = decimal.Zero;
                decimal lowestFreight = decimal.Zero;
                decimal result = decimal.Zero;
                bool lowestFreightMethodShippingIsFree = false;
                //If the vat is inclusive or exclusive
                bool vatInclusive = AppLogic.VATIsEnabled() && thisCustomer.VATSettingReconciled == VATSettingEnum.ShowPricesInclusiveOfVAT;

                //The lowest shipping method cost
                ShippingMethod lowestFreightMethod = availableShippingMethods.LowestFreight;

                if (vatEnable)
                {
                    //if VAT.Enabled is true remove the ':' at the end
                    //to have format like this 'Shipping (ex vat):' instead of 'Shipping: (ex vat)'
                    int count = shippingEstimateCaption.Length - 1;
                    ctrlEstimate.ShippingEstimateCaption = shippingEstimateCaption.Remove(count);
                }

                bool isAllFreeShippingComponents = cart.IsAllFreeShippingComponents();
                bool isAllDownloadComponents = cart.IsAllDownloadComponents();
                bool isAllEmailGiftCards = cart.IsAllEmailGiftCards();
                decimal freeShippingThreshold = AppLogic.AppConfigNativeDecimal("FreeShippingThreshold");
                bool isQualifiedForFreeShippingThreshold = freeShippingThreshold > 0 && freeShippingThreshold <= SubTotal;

                //Set the value for lowest freight and name
                if (!isAllFreeShippingComponents && !cart.ShippingIsFree && !skipShippingOnCheckout
                    || freeShippingAllowsRateSelection)
                {
                    if (availableShippingMethods.Count == 0)
                    { lowestFreight = 0; }
                    else
                    {
                        lowestFreight = lowestFreightMethod.Freight;
                        lowestFreightMethodShippingIsFree = lowestFreightMethod.ShippingIsFree;
                        lowestfreightName = lowestFreightMethod.Name;
                    }

                    if (lowestFreight < 0)
                    { lowestFreight = 0; }

                    if (isQualifiedForFreeShippingThreshold && lowestFreightMethodShippingIsFree)
                    {
                        lowestFreight = 0;
                    }
                }


                //Computation of tax and shipping cost for non register customer
                if (!thisCustomer.IsRegistered || ThisCustomer.PrimaryShippingAddressID <= 0)
                {
                    decimal totalProduct = decimal.Zero;
                    decimal TaxShippingTotal = decimal.Zero;

                    //taxes for shipping
                    Decimal CountryShippingTaxRate = AppLogic.GetCountryTaxRate(thisCustomer.PrimaryShippingAddress.Country, AppLogic.AppConfigUSInt("ShippingTaxClassID"));
                    Decimal ZipShippingTaxRate = AppLogic.ZipTaxRatesTable.GetTaxRate(thisCustomer.PrimaryShippingAddress.Zip, AppLogic.AppConfigUSInt("ShippingTaxClassID"));
                    Decimal StateShippingTaxRate = AppLogic.GetStateTaxRate(thisCustomer.PrimaryShippingAddress.State, AppLogic.AppConfigUSInt("ShippingTaxClassID"));

                    foreach (CartItem ci in cart.CartItems)
                    {
                        Decimal StateTaxRate = AppLogic.GetStateTaxRate(thisCustomer.PrimaryShippingAddress.State, ci.TaxClassID);
                        Decimal CountryTaxRate = AppLogic.GetCountryTaxRate(thisCustomer.PrimaryShippingAddress.Country, ci.TaxClassID);
                        Decimal ZipTaxRate = AppLogic.ZipTaxRatesTable.GetTaxRate(thisCustomer.PrimaryShippingAddress.Zip, ci.TaxClassID);
                        Decimal DIDPercent = 0.0M;
                        Decimal DiscountedItemPrice = ci.Price * ci.Quantity;
                        QuantityDiscount.QuantityDiscountType fixedPriceDID = QuantityDiscount.QuantityDiscountType.Percentage;

                        //Handle the quantity discount
                        DIDPercent = QuantityDiscount.GetQuantityDiscountTablePercentageForLineItem(ci, out fixedPriceDID);
                        if (DIDPercent != 0.0M)
                        {
                            if (fixedPriceDID == QuantityDiscount.QuantityDiscountType.FixedAmount)
                            {
                                if (Currency.GetDefaultCurrency() == thisCustomer.CurrencySetting)
                                {
                                    DiscountedItemPrice = (ci.Price - DIDPercent) * ci.Quantity;

                                }
                                else
                                {
                                    DIDPercent = Decimal.Round(Currency.Convert(DIDPercent, Localization.StoreCurrency(), thisCustomer.CurrencySetting), 2, MidpointRounding.AwayFromZero);
                                    DiscountedItemPrice = (ci.Price - DIDPercent) * ci.Quantity;

                                }
                            }
                            else
                            {
                                DiscountedItemPrice = ((100.0M - DIDPercent) / 100.0M) * (ci.Price * ci.Quantity);
                            }
                        }

                        //Handle the coupon
                        if ((cart.GetCoupon().CouponType == CouponTypeEnum.OrderCoupon)
                            || (cart.GetCoupon().CouponType == CouponTypeEnum.ProductCoupon))
                        {
                            decimal discountPercent = cart.GetCoupon().DiscountPercent;
                            decimal discountAmount = cart.GetCoupon().DiscountAmount;

                            discountPercent = DiscountedItemPrice * (discountPercent / 100);
                            DiscountedItemPrice = DiscountedItemPrice - discountPercent;
                            DiscountedItemPrice = DiscountedItemPrice - (discountAmount / cart.CartItems.Count);

                        }

                        //Handle Gift Card
                        if (cart.Coupon.CouponType == CouponTypeEnum.GiftCard)
                        {
                            decimal giftCardAmount = cart.Coupon.DiscountAmount;
                            if (estimatedTotal > giftCardAmount)
                            {
                                estimatedTotal -= giftCardAmount;
                            }
                            else
                            {
                                giftCardAmount = estimatedTotal;
                                estimatedTotal = decimal.Zero;
                            }
                            ctrlEstimate.ShowGiftCardApplied = true;
                            ctrlEstimate.GiftCardAppliedCaption = AppLogic.GetString("checkoutshipping.estimator.control.GiftCardApplied", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                            ctrlEstimate.GiftCardAppliedEstimate = Localization.CurrencyStringForDisplayWithExchangeRate(giftCardAmount, thisCustomer.CurrencySetting);
                        }

                        //Making sure to set it zero if DiscountedItemPrice becomes less than zero
                        if (DiscountedItemPrice < 0)
                        {
                            DiscountedItemPrice = 0;
                        }
                        if (ci.IsTaxable)
                        {
                            totalProduct = ZipTaxRate + CountryTaxRate + StateTaxRate;
                            totalProduct = (totalProduct / 100) * DiscountedItemPrice;
                            result += totalProduct;
                            estimatedTax = result;
                        }
                        //This will handle the order option
                        if(cart.OrderOptions.Count > 0)
                        {
                            //Then add it to the estimated tax
                            //JH - removed the following addtion as it doesn't make sense. Estimate tax should not have order option totals included
                            //estimatedTax += Prices.OrderOptionTotal(ThisCustomer, cart.OrderOptions);

                            int orderOptionTaxClassID = 0;
                            decimal estimatedTaxOnOrderOption = decimal.Zero;
                            decimal StateTaxRateForOrderOption = decimal.Zero;
                            decimal CountryTaxRateForOrderOption = decimal.Zero;
                            decimal ZipTaxRateForOrderOption = decimal.Zero;
                            decimal orderOptioncost = decimal.Zero;

                            foreach (int s_optionId in cart.OrderOptions.Select( o => o.ID ))
                            {
                                //Check if it selected then apply the tax
                                if (cart.OptionIsSelected(s_optionId, cart.OrderOptions))
                                {
                                    //We need to get the cost per order option so we can compute
                                    //the tax base on taxclass id
                                    using (SqlConnection conn = new SqlConnection(DB.GetDBConn()))
                                    {
                                        conn.Open();
                                        string query = string.Format("Select TaxClassID,Cost from OrderOption WHERE OrderOptionID = {0}", s_optionId.ToString());

                                        using (IDataReader orderOptionreader = DB.GetRS(query, conn))
                                        {
                                            if (orderOptionreader.Read())
                                            {
                                                orderOptionTaxClassID = DB.RSFieldInt(orderOptionreader, "TaxClassID");
                                                orderOptioncost = DB.RSFieldDecimal(orderOptionreader, "Cost");
                                            }
                                        }

                                    }
                                    //Base on the taxclass id and address
                                    //JH - updated state tax rate to newer function calls
                                    //StateTaxRateForOrderOption = AppLogic.GetStateTaxRate(orderOptionTaxClassID, thisCustomer.PrimaryShippingAddress.State);
                                    StateTaxRateForOrderOption = AppLogic.GetStateTaxRate(thisCustomer.PrimaryShippingAddress.State, orderOptionTaxClassID);
                                    CountryTaxRateForOrderOption = AppLogic.GetCountryTaxRate(thisCustomer.PrimaryShippingAddress.Country, orderOptionTaxClassID);
                                    ZipTaxRateForOrderOption = AppLogic.ZipTaxRatesTable.GetTaxRate(thisCustomer.PrimaryShippingAddress.Zip, orderOptionTaxClassID);

                                    //Total first the tax base on the address 
                                    estimatedTaxOnOrderOption = StateTaxRateForOrderOption + CountryTaxRateForOrderOption + ZipTaxRateForOrderOption;
                                    //Then apply it to orderoption cost
                                    estimatedTaxOnOrderOption = (estimatedTaxOnOrderOption / 100) * orderOptioncost;
                                    //Then add it to the estimated tax
                                    estimatedTax += estimatedTaxOnOrderOption;
                                }
                            }
                        }

                        //Set it to zero if customerlevel has no tax
                        if (AppLogic.CustomerLevelHasNoTax(thisCustomer.CustomerLevelID))
                        {
                            estimatedTax = decimal.Zero;
                        }
                    }

					if(!thisCustomer.IsRegistered && AppLogic.AppConfigBool("AvalaraTax.Enabled"))
                    {
                        estimatedTax = Prices.TaxTotal(ThisCustomer, cart.CartItems, estimatedShippingtotalWithTax, cart.OrderOptions);                        
                    }

                    TaxShippingTotal = lowestFreight;
                    if (StateShippingTaxRate != System.Decimal.Zero
                        || CountryShippingTaxRate != System.Decimal.Zero
                        || ZipShippingTaxRate != System.Decimal.Zero)
                    {
                        if (thisCustomer.CurrencySetting != Localization.GetPrimaryCurrency())
                        {
                            TaxShippingTotal = decimal.Round(Currency.Convert(TaxShippingTotal, Localization.StoreCurrency(), thisCustomer.CurrencySetting), 2, MidpointRounding.AwayFromZero);
                        }
                        estimatedTax += ((StateShippingTaxRate + CountryShippingTaxRate + ZipShippingTaxRate) / 100.0M) * TaxShippingTotal;//st;

                    }

                    estimatedTotal = estimatedTax + lowestFreight + SubTotal;

                }
                //Register Customer
                else
                {
                    if (isAllFreeShippingComponents &&
                        !freeShippingAllowsRateSelection &&
                        !skipShippingOnCheckout ||
                        isAllDownloadComponents ||
                        skipShippingOnCheckout ||
                        isAllEmailGiftCards)
                    {
                        estimatedTax = cart.TaxTotal();

                        if (vatInclusive)
                        {
                            estimatedTotal = SubTotal;
                        }
                        else
                        {
                            estimatedTotal = SubTotal + estimatedTax;
                        }

                        // apply gift card if any
                        if (cart.Coupon.CouponType == CouponTypeEnum.GiftCard)
                        {
                            decimal giftCardAmount = cart.Coupon.DiscountAmount;
                            if (estimatedTotal > giftCardAmount)
                            {
                                estimatedTotal -= giftCardAmount;
                            }
                            else
                            {
                                giftCardAmount = estimatedTotal;
                                estimatedTotal = decimal.Zero;
                            }
                            ctrlEstimate.ShowGiftCardApplied = true;
                            ctrlEstimate.GiftCardAppliedCaption = AppLogic.GetString("checkoutshipping.estimator.control.GiftCardApplied", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                            ctrlEstimate.GiftCardAppliedEstimate = Localization.CurrencyStringForDisplayWithExchangeRate(giftCardAmount, thisCustomer.CurrencySetting);
                        }
                        else
                        {
                            //always set it false, in case user update it
                            ctrlEstimate.ShowGiftCardApplied = false;
                        }
                    }
                    else
                    {
						decimal taxTotal;

						if(AppLogic.CustomerLevelHasNoTax(thisCustomer.CustomerLevelID))
						{
							estimatedTax = decimal.Zero;
							taxTotal = decimal.Zero;
							shippingTaxAmount = decimal.Zero;
							estimatedTotal = SubTotal + lowestFreight;
						}
						else if(vatInclusive)
						{
							// zero out the shipping total for now so that we can get the breakdown
							decimal subTotalTaxAmount = Prices.TaxTotal(cart.ThisCustomer, cart.CartItems, System.Decimal.Zero, cart.OrderOptions);

							int shippingTaxID = AppLogic.AppConfigUSInt("ShippingTaxClassID");
							decimal shippingTaxRate = thisCustomer.TaxRate(shippingTaxID);
							shippingTaxAmount = decimal.Round(lowestFreight * (shippingTaxRate / 100.0M), 2, MidpointRounding.AwayFromZero);
		
							taxTotal = subTotalTaxAmount + shippingTaxAmount;
							estimatedTax = taxTotal;
							estimatedTotal = SubTotal + lowestFreight + shippingTaxAmount;
						}
						else
						{
							taxTotal = Prices.TaxTotal(cart.ThisCustomer, cart.CartItems, lowestFreight, cart.OrderOptions);
							estimatedTax = taxTotal;
							estimatedTotal = SubTotal + lowestFreight + taxTotal;
						}

						// apply gift card if any
                        if (cart.Coupon.CouponType == CouponTypeEnum.GiftCard)
                        {
                            decimal giftCardAmount = cart.Coupon.DiscountAmount;
                            if (estimatedTotal > giftCardAmount)
                            {
                                estimatedTotal -= giftCardAmount;
                            }
                            else
                            {
                                giftCardAmount = estimatedTotal;
                                estimatedTotal = decimal.Zero;
                            }
                            ctrlEstimate.ShowGiftCardApplied = true;
                            ctrlEstimate.GiftCardAppliedCaption = AppLogic.GetString("checkoutshipping.estimator.control.GiftCardApplied", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                            ctrlEstimate.GiftCardAppliedEstimate = Localization.CurrencyStringForDisplayWithExchangeRate(giftCardAmount, thisCustomer.CurrencySetting);
                        }
                        else
                        {
                            //always set it false, in case user update it
                            ctrlEstimate.ShowGiftCardApplied = false;
                        }
                    }

                }
                

                //Assigning of value to the control
                if (isAllDownloadComponents
                  || availableShippingMethods.Count == 0
                  || skipShippingOnCheckout
                  || lowestFreightMethodShippingIsFree)
                {
                    string NoShippingRequire = string.Empty;
                    string shippingName = string.Empty;
                    if (cart.ShippingIsFree && !isAllDownloadComponents && !isAllEmailGiftCards && !cart.NoShippingRequiredComponents())
                    {
                        NoShippingRequire = AppLogic.GetString("checkoutshipping.estimator.control.FreeShipping", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                    }
                    else if (string.IsNullOrEmpty(lowestfreightName))
                    {
                        NoShippingRequire = "N/A";
                    }
                    else
                    {
                        NoShippingRequire = AppLogic.GetString("checkoutshipping.estimator.control.Shipping", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                    }

                    if (lowestFreightMethodShippingIsFree)
                    {
                        shippingName = " (" + lowestfreightName + ")";
                    }

                    if (!vatInclusive || !thisCustomer.IsRegistered)
                    {
                        if (vatEnable && !vatInclusive)
                        {
                            inc = " (" + AppLogic.GetString("setvatsetting.aspx.7", thisCustomer.SkinID, thisCustomer.LocaleSetting) + "):";

                        }

                        ctrlEstimate.ShippingEstimateCaption += inc + shippingName;

                        if (ctrlEstimate.ShippingEstimateCaption.LastIndexOf(":").Equals(-1))
                        {
                            ctrlEstimate.ShippingEstimateCaption += ":";
                        }

                        ctrlEstimate.ShippingEstimate = NoShippingRequire;
                        ctrlEstimate.TaxEstimate = Localization.CurrencyStringForDisplayWithExchangeRate(estimatedTax, thisCustomer.CurrencySetting);

                    }
                    else
                    {
                        if (vatEnable)
                        {
                            inc = " (" + AppLogic.GetString("setvatsetting.aspx.6", thisCustomer.SkinID, thisCustomer.LocaleSetting) + "):";
                        }
                        ctrlEstimate.ShowTax = false;
                        ctrlEstimate.ShippingEstimateCaption += inc + shippingName;
                        ctrlEstimate.ShippingEstimate = NoShippingRequire;
                    }
                }
                else if (lowestfreightName == "FREE SHIPPING (All Orders Have Free Shipping)"
                     || isAllFreeShippingComponents && !freeShippingAllowsRateSelection
                     || cart.ShippingIsFree && !freeShippingAllowsRateSelection
                     || isQualifiedForFreeShippingThreshold && lowestFreightMethodShippingIsFree)
                {
                    string Free = AppLogic.GetString("checkoutshipping.estimator.control.FreeShipping", thisCustomer.SkinID, thisCustomer.LocaleSetting);

                    if (thisCustomer.IsRegistered && vatInclusive)
                    {
                        if (vatEnable)
                        {
                            inc = " (" + AppLogic.GetString("setvatsetting.aspx.6", thisCustomer.SkinID, thisCustomer.LocaleSetting) + "):";
                        }
                        ctrlEstimate.ShippingEstimate = Free;
                        ctrlEstimate.ShippingEstimateCaption += inc;
                        ctrlEstimate.ShowTax = false;
                        ctrlEstimate.TaxEstimate = Localization.CurrencyStringForDisplayWithExchangeRate(estimatedTax, thisCustomer.CurrencySetting);
                    }
                    else
                    {
                        //Seperate tax and shipping cost even it is inclusive mode
                        //if non register so user will not be confused on the total computation
                        if (vatEnable && !vatInclusive)
                        {
                            inc = " (" + AppLogic.GetString("setvatsetting.aspx.7", thisCustomer.SkinID, thisCustomer.LocaleSetting) + "):";

                        }

                        ctrlEstimate.ShippingEstimateCaption += inc;

                        if (ctrlEstimate.ShippingEstimateCaption.LastIndexOf(":").Equals(-1))
                        {
                            ctrlEstimate.ShippingEstimateCaption += ":";
                        }

                        ctrlEstimate.ShippingEstimate = Free;
                        ctrlEstimate.ShowTax = true;
                        ctrlEstimate.TaxEstimate = Localization.CurrencyStringForDisplayWithExchangeRate(estimatedTax, thisCustomer.CurrencySetting);
                    }
                }
                else
                {
                    if (!vatInclusive)
                    {
                        if (vatEnable)
                        {
                            inc = "(" + AppLogic.GetString("setvatsetting.aspx.7", thisCustomer.SkinID, thisCustomer.LocaleSetting) + "):";

                        }
                        string shippingText = string.Format(" {0} ({1})", inc, lowestFreightMethod.Name);
                        ctrlEstimate.ShippingEstimateCaption += shippingText;
                        ctrlEstimate.ShippingEstimate = Localization.CurrencyStringForDisplayWithExchangeRate(lowestFreight, thisCustomer.CurrencySetting);
                        ctrlEstimate.TaxEstimate = Localization.CurrencyStringForDisplayWithExchangeRate(estimatedTax, thisCustomer.CurrencySetting);
                        ctrlEstimate.TotalEstimate = Localization.CurrencyStringForDisplayWithExchangeRate(estimatedTotal, thisCustomer.CurrencySetting);
                    }
                    else
                    {
                        if (vatEnable)
                        {
                            inc = "(" + AppLogic.GetString("setvatsetting.aspx.6", thisCustomer.SkinID, thisCustomer.LocaleSetting) + "):";
                        }

                        if (thisCustomer.IsRegistered)
                        {
                            estimatedShippingtotalWithTax = (lowestFreight + shippingTaxAmount);
                        }
                        else
                        {
                            estimatedShippingtotalWithTax = (lowestFreight + shippingTaxAmount + estimatedTax);
                        }

                        string shippingText = string.Format(" {0} ({1})", inc, lowestFreightMethod.Name);
                        ctrlEstimate.ShippingEstimateCaption += shippingText;
                        ctrlEstimate.ShippingEstimate = Localization.CurrencyStringForDisplayWithExchangeRate(estimatedShippingtotalWithTax, thisCustomer.CurrencySetting);
                        ctrlEstimate.ShowTax = false;
                    }
                }
                ctrlEstimate.TotalEstimate = Localization.CurrencyStringForDisplayWithExchangeRate(estimatedTotal, thisCustomer.CurrencySetting);
            }
            catch
            {
                ctrlEstimate.ShippingEstimate = "--";
                ctrlEstimate.TaxEstimate = "--";
                ctrlEstimate.TotalEstimate = "--";
                ctrlEstimate.HeaderCaption = AppLogic.GetString("checkoutshipping.estimator.control.header", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                ctrlEstimate.ShippingEstimateCaption = AppLogic.GetString("checkoutshipping.ShippingEstimateCaption", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                ctrlEstimate.TaxEstimateCaption = AppLogic.GetString("checkoutshipping.TaxEstimateCaption", thisCustomer.SkinID, thisCustomer.LocaleSetting);
                ctrlEstimate.TotalEstimateCaption = AppLogic.GetString("checkoutshipping.TotalEstimateCaption", thisCustomer.SkinID, thisCustomer.LocaleSetting);
            }
        }

        protected void ctrlShoppingCart_ItemDeleting(object sender, ItemEventArgs e)
        {
            cart.SetItemQuantity(e.ID, 0);
            Response.Redirect("shoppingcart.aspx");
        }

        protected void btnRemoveCoupon_Click(object sender, EventArgs e)
        {
            cart.SetCoupon("", true);
            CouponCode.Text = "";
            btnRemoveCoupon.Visible = false;
            UpdateCart();
        }

        private void BindPromotions()
        {
            repeatPromotions.DataSource = cart.DiscountResults.Select(dr => dr.Promotion);
            repeatPromotions.DataBind();            
        }

        protected void repeatPromotions_ItemDataBound(Object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
                return;

            Promotions.IPromotion promotion = e.Item.DataItem as Promotions.IPromotion;
            if (promotion == null)
                return;                                   
        }

        protected void repeatPromotions_ItemCommand(Object sender, RepeaterCommandEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
                return;

            PromotionManager.ClearPromotionUsages(ThisCustomer.CustomerID, e.CommandArgument.ToString(), true);
            UpdateCart();
            BindPromotions();
        }

        protected void btnAddPromotion_Click (Object sender, EventArgs e)
        {
            String promotionCode = txtPromotionCode.Text.ToLower().Trim();
            txtPromotionCode.Text = String.Empty;
            lblPromotionError.Text = String.Empty;

            IEnumerable<IPromotionValidationResult> validationResults = PromotionManager.ValidatePromotion(promotionCode, PromotionManager.CreateRuleContext(cart));
            if (validationResults.Count() > 0 && validationResults.Any(vr => !vr.IsValid))
            {
				InitializeShippingAndEstimateControl();

                foreach (var reason in validationResults.Where(vr => !vr.IsValid).SelectMany(vr => vr.Reasons))
                {
                    String message = reason.MessageKey.StringResource();
                    if (reason.ContextItems != null && reason.ContextItems.Any())
                        foreach (var item in reason.ContextItems)
                            message = message.Replace(String.Format("{{{0}}}", item.Key), item.Value.ToString());

                    lblPromotionError.Text += String.Format("<div class='promotionreason'>{0}</div>", message);
                }
                return;
            }
            else
            {
                PromotionManager.AssignPromotion(ThisCustomer.CustomerID, promotionCode);
            }

            UpdateCart();
            BindPromotions();
        }
    }
}
