using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerPromptType]
public class OrderHubPrompts
{
    [McpServerPrompt(Name = "debug-complaint"),
     Description("用標準格式描述一個客訴問題,方便 agent 定位根因")]
    public static string DebugComplaint(
        [Description("客訴症狀描述")] string symptom,
        [Description("實測到的具體現象,例如訂單編號、金額、頁碼")] string observation)
        => $"""
            請依照標準流程排查以下問題:

            症狀:{symptom}
            實測現象:{observation}

            請先推測涉及的頁面與流程並跟我確認理解,
            再往下追蹤到 Controller/Service/Repository 定位根因,
            說明根因後等我確認才動手修。
            """;
}
