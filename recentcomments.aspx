<%@ Page language="c#" Inherits="AspDotNetStorefront.recentcomments" CodeFile="recentcomments.aspx.cs" MasterPageFile="~/App_Templates/Skin_1/template.master" %>
<%@ Register TagPrefix="aspdnsf" TagName="XmlPackage" Src="~/Controls/XmlPackageControl.ascx" %>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="PageContent">
    <asp:Panel ID="pnlContent" runat="server" >
    
        <aspdnsf:XmlPackage ID="XmlPackage1" PackageName="page.recentcomments.xml.config" runat="server"
            EnforceDisclaimer="true" EnforcePassword="True" EnforceSubscription="true" AllowSEPropogation="True" />
            
    </asp:Panel>
</asp:Content>
