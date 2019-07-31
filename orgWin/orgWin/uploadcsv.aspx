<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="uploadcsv.aspx.cs" Inherits="orgWin.uploadcsv" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
        body
        {
            font-family: Arial;
            font-size: 10pt;
        }
        table
        {
            border: 1px solid #ccc;
            border-collapse: collapse;
            background-color: #fff;
        }
        table th
        {
            background-color: #B8DBFD;
            color: #333;
            font-weight: bold;
        }
        table th, table td
        {
            padding: 5px;
            border: 1px solid #ccc;
        }
        table, table table td
        {
            border: 0px solid #ccc;
        }
#btnTempl {
    float: right;
}
#delim {
    width: 5px;
}
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <asp:FileUpload ID="FileUpload1" runat="server" />
    <asp:Button ID="btnImport" runat="server" Text="Загрузить CSV" OnClick="ImportCSV" />
    Разделитель CSV
    <asp:TextBox id="delim" Text=";" runat="server" />
    <asp:Button ID="btnSave"  runat="server" Text="Save" OnClick="Save" />
    <asp:Button ID="btnTempl" runat="server" Text="Получить шаблон CSV" OnClick="Tpl" />
    <hr />
    <asp:GridView ID="GridView1" runat="server">
    </asp:GridView>
    </form>
</body>
</html>
