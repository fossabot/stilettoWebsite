// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways.Processors;
using Vortx.OnePageCheckout.Models;
using Vortx.OnePageCheckout.Views;

public partial class OPCControls_PaymentForms_FirstPayEmbeddedCheckoutPaymentForm : System.Web.UI.UserControl,
    IPaymentMethodView
{
    public IStringResourceProvider StringResourceProvider { get; set; }
    public PaymentMethodBaseModel PaymentMethodModel { get; private set; }
    public GatewayProcessor gateway { get; set; }
    public Customer thisCustomer { get; set; }

    public void SetModel(PaymentMethodBaseModel model)
    {
        this.PaymentMethodModel = model;
    }


    public void BindView()
    {
    }

    public void BindView(object identifier)
    {
    }

    public void SaveViewToModel()
    {
    }

    public void Initialize()
    {
        if (gateway == null || thisCustomer == null)
            return;

        string ccPane = gateway.CreditCardPaneInfo(thisCustomer.SkinID, thisCustomer);

        litFirstPayEmbeddedCheckoutFrame.Text = ccPane;

        if (CommonLogic.QueryStringNativeInt("ErrorMsg") > 0)
        {
            ErrorMessage e = new ErrorMessage(CommonLogic.QueryStringNativeInt("ErrorMsg"));
            ShowError(e.Message);
        }
    }

    public void Disable()
    {
        this.Visible = false;
    }

    public void Enable()
    {
        this.Visible = true;
    }

    public void Show()
    {
        this.Visible = true;
    }

    public void Hide()
    {
        this.Visible = false;
    }

    public void ShowError(string message)
    {
        PanelError.Visible = true;
        ErrorMessage.Text = message;
    }

}
