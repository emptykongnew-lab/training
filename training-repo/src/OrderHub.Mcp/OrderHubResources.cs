using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerResourceType]
public class OrderHubResources
{
    [McpServerResource(UriTemplate = "orderhub://business-rules", Name = "business-rules"),
     Description("OrderHub 的核心商業規則:會員折扣、訂單驗證、取消規則")]
    public string GetBusinessRules() => """
        # OrderHub 商業規則

        ## 會員折扣
        - Gold: 9折
        - Silver: 95折
        - Standard: 無折扣

        ## 訂單建立驗證
        - 客戶必須存在
        - 明細不可為空
        - 數量必須大於0
        - 同一訂單不可重複商品
        - 商品必須存在且上架中
        - 庫存必須足夠

        ## 取消訂單規則
        - 僅 Pending 或 Confirmed 狀態可取消
        - 取消後自動回補庫存
        """;
}
