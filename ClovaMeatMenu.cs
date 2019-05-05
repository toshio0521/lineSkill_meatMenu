using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CEK.CSharp;
using CEK.CSharp.Models;

namespace MangaClova
{
    public static class ClovaMeatMenu
    {
        private const String LaunchMessege = "何を食べるか決められないあなたに、オススメのメニューを提案します。朝、昼、夜を指定して、朝に何を食べたらいい？などと聞いてください";
        private const String SessionEndedMessege = "いつでも呼んでくださいね。";
        private const String NoSlotMessege = "うまく聞き取れませんでした。朝、昼、夜を指定してください。";
        private const String  MenuIntent= "MenuIntent";
        private const String  SlotTimeZone= "meatTime";
        public const int SpeedOfLight = 300000; // km per sec.
        [FunctionName("ClovaMeatMenu")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            //リクエストボディのJSONを検証してC#のクラスに変換
            var clovaRequest = await new ClovaClient().GetRequest(req.Headers["SignatureCEK"],req.Body);
            //返事を作成
            var clovaResponse = new CEKResponse();
            switch(clovaRequest.Request.Type)
            {
                case RequestType.LaunchRequest:
                //起動時の処理
                clovaResponse.AddText(LaunchMessege);
                clovaResponse.ShouldEndSession = false;
                break;
                case RequestType.SessionEndedRequest:
                //終了時の処理
                clovaResponse.AddText (SessionEndedMessege);
                break;
                case RequestType.IntentRequest:
                    // インテントの処理
                    switch (clovaRequest.Request.Intent.Name)
                    {
                        case MenuIntent:
                        // インテント
                        var timeZone = "";
                        if (clovaRequest.Request.Intent.Slots != null && clovaRequest.Request.Intent.Slots.TryGetValue(SlotTimeZone, out var timeZoneSlot))
                        {
                            // 対象がある場合は、スロットから取得する
                             timeZone = timeZoneSlot.Value;
                        }
                        var menu ="ごはん";
                        // メニューを朝、昼、夜、で分けてランダムで取得しています。
                        if(timeZone.Equals("朝")){
                            menu = new[]{ "卵焼き", "目玉焼き", "おにぎり", "ホットドッグ", "ヨーグルト"}[new Random().Next(5)];
                        }
                        else if(timeZone.Equals("昼")){
                            menu = new[]{ "ラーメン", "ざるそば", "たこ焼き", "豚ヒレカツでふわふわカツ丼", "焼きそばナポリタン"}[new Random().Next(5)];
                        }
                        else if(timeZone.Equals("夜")){
                            menu = new[]{ "オムライス", "山椒たっぷりの讃岐うどん", "ドライカレー", "じゃがいもステーキのタルタルソースがけ", "チキン南蛮弁当"}[new Random().Next(5)];
                        }
                        else {
                            clovaResponse.AddText(NoSlotMessege);
                            clovaResponse.ShouldEndSession = false;
                            break;      
                        }
                        clovaResponse.AddText($"今日あなたが食べたい{timeZone}ご飯は {menu} です。");
                        break;
                        default:
                        // 認識できなかったインテント
                        clovaResponse.AddText(NoSlotMessege);
                        clovaResponse.ShouldEndSession = false; // スキルを終わらせないように設定する
                        break;
                    }
                break;
            }
            // レスポンスとして作成した返事の内容を返す
            return new OkObjectResult(clovaResponse);
        }
    }
}
