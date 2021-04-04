using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Line.Messaging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DotnetTestFunction
{
    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request,
            ILambdaContext context)
        {
            Console.WriteLine("====== start lambda =======");
            var message = JsonSerializer.Deserialize<LineMessage>(request.Body);
            Console.WriteLine(message.Events[0].Message.Text);

            var client = new AmazonDynamoDBClient();
            // TODO: こっちの書き方でやりたい
            // var ctx = new DynamoDBContext(client);
            // var item = await ctx.QueryAsync<PokemonTableItem>(message.Events[0].Message.Text).GetRemainingAsync();

            var response = await client.QueryAsync(new QueryRequest
            {
                TableName = Environment.GetEnvironmentVariable("TABLE_NAME"),
                KeyConditionExpression = "id = :id",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":id"] = new AttributeValue {S = message.Events[0].Message.Text}
                }
            });

            try
            {
                var types = response.Items.SelectMany(item => item["types"].L.Select(type => type.S)).ToList();
                await SendLinedMessage(message.Events[0].ReplyToken, string.Join(" ", types));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await SendLinedMessage(message.Events[0].ReplyToken, "検索に失敗しました。\nガラル地方のポケモンの名前か確認してください\uDBC0\uDC86");
            }

            return new APIGatewayProxyResponse
            {
                StatusCode = (int) HttpStatusCode.OK
            };
        }

        private async Task SendLinedMessage(string replyToken, string message)
        {
            var replyMessage = new TextMessage(message);
            var messagingClient = new LineMessagingClient(Environment.GetEnvironmentVariable("CHANNEL_ACCESS_TOKEN"));

            await messagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage> {replyMessage});
        }
    }

    [DynamoDBTable("PokemonTable")]
    class PokemonTableItem
    {
        [DynamoDBHashKey("id")] public string Id { get; set; }

        [DynamoDBProperty("types")] public string[] Types { get; set; }
    }

    class LineMessage
    {
        [JsonPropertyName("events")] public Event[] Events { get; set; }
    }

    class Event
    {
        [JsonPropertyName("replyToken")] public string ReplyToken { get; set; }
        [JsonPropertyName("message")] public Message Message { get; set; }
    }

    class Message
    {
        [JsonPropertyName("text")] public string Text { get; set; }
    }
}