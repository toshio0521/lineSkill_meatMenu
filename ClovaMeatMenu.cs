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
using Line.Messaging;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace MangaClova
{
    public static class ClovaMeatMenu
    {
        private static string IntroductionMessage { get; } = "こんにちは。何を食べるか決められないあなたに、オススメのメニューを提案します。朝、昼、夜を指定して、朝に何を食べたらいい？などと聞いてください";
        private static string SessionEndedMessege { get; } = "いつでも呼んでくださいね。";
        private static string NoSlotMessege { get; } = "うまく聞き取れませんでした。朝、昼、夜を指定してください。";
        private const string  MenuIntent= "MenuIntent";
        private static string  SlotTimeZone= "meatTime";
        public const int SpeedOfLight = 300000; // km per sec.
        [FunctionName("ClovaMeatMenu")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ExecutionContext context,ILogger log)
        {
            //リクエストボディのJSONを検証してC#のクラスに変換
            var clovaRequest = await new ClovaClient().GetRequest(req.Headers["SignatureCEK"],req.Body);
            //返事を作成
            var clovaResponse = new CEKResponse();
            switch(clovaRequest.Request.Type)
            {
                case RequestType.LaunchRequest:
                //起動時の処理
                clovaResponse.AddText(IntroductionMessage);
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
                        // LINE にプッシュ通知する
                            var config = new ConfigurationBuilder()
                                .SetBasePath(context.FunctionAppDirectory)
                                .AddJsonFile("local.settings.json", true)
                                .AddEnvironmentVariables()
                                .Build();

                            var secret = config.GetValue<string>("LineMessagingApiSecret");
                            var messagingClient = new LineMessagingClient(secret);
  
                             await messagingClient.PushMessageAsync(
                                 to: clovaRequest.Session.User.UserId,
                                 messages: new List<ISendMessage>
                                 {
                                     new TextMessage($"今日あなたが食べたい{timeZone}ご飯は {menu} です。"),
                                 });
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
